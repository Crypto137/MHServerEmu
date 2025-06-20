using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot.Specs;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// Create loot by rolling loot tables and from other sources.
    /// </summary>
    public class LootManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ItemResolver _resolver;
        private readonly LootSpawnGrid _lootSpawnGrid;
        private readonly WorldEntityPrototype _creditsItemProto; 

        public Game Game { get; }

        /// <summary>
        /// Constructs a new <see cref="LootManager"/> for the provided <see cref="Games.Game"/>.
        /// </summary>
        public LootManager(Game game)
        {
            Game = game;

            _resolver = new();
            _resolver.Initialize(game.Random);

            _lootSpawnGrid = new(game);

            _creditsItemProto = GameDatabase.GlobalsPrototype.CreditsItemPrototype.As<WorldEntityPrototype>();
        }

        /// <summary>
        /// Rolls the specified loot table and drops loot from the provided source <see cref="WorldEntity"/>.
        /// </summary>
        public void SpawnLootFromTable(PrototypeId lootTableProtoRef, LootInputSettings inputSettings, int recipientId)
        {
            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            RollLootTable(lootTableProtoRef, inputSettings, lootResultSummary);

            if (lootResultSummary.HasAnyResult == false) return;

            SpawnLootFromSummary(lootResultSummary, inputSettings, recipientId);
        }

        public void GiveLootFromTable(PrototypeId lootTableProtoRef, LootInputSettings inputSettings)
        {
            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            RollLootTable(lootTableProtoRef, inputSettings, lootResultSummary);

            if (lootResultSummary.HasAnyResult == false) return;

            GiveLootFromSummary(lootResultSummary, inputSettings.Player);
        }

        public void AwardLootFromTables(Span<(PrototypeId, LootActionType)> tables, LootInputSettings inputSettings, int recipientId)
        {
            // TODO: Combine loot summaries from multiple spawn / drop events

            foreach ((PrototypeId, LootActionType) tableEntry in tables)
            {
                (PrototypeId lootTableProtoRef, LootActionType actionType) = tableEntry;

                if (actionType == LootActionType.Spawn)
                {
                    SpawnLootFromTable(lootTableProtoRef, inputSettings, recipientId);
                }
                else if (actionType == LootActionType.Give)
                {
                    GiveLootFromTable(lootTableProtoRef, inputSettings);
                }
            }
            
            // Spawn mission-specific loot (e.g. brood biomass in chapter 7)
            if (inputSettings.LootContext == LootContext.Drop &&
                inputSettings.EventType >= LootDropEventType.OnKilled &&
                inputSettings.EventType <= LootDropEventType.OnKilledMiniBoss)
            {
                List<MissionLootTable> missionLootTableList = ListPool<MissionLootTable>.Instance.Get();

                if (MissionManager.GetMissionLootTablesForEnemy(inputSettings.SourceEntity, inputSettings.Player, missionLootTableList))
                {
                    foreach (MissionLootTable missionLootTable in missionLootTableList)
                    {
                        inputSettings.MissionProtoRef = missionLootTable.MissionRef;
                        SpawnLootFromTable(missionLootTable.LootTableRef, inputSettings, recipientId);
                    }

                    // We are not using these settings anymore, but let's clear the mission prototype ref just in case something changes
                    inputSettings.MissionProtoRef = PrototypeId.Invalid;
                }

                ListPool<MissionLootTable>.Instance.Return(missionLootTableList);
            }
        }

        /// <summary>
        /// Does a test roll of the specified loot table for the provided <see cref="Player"/>.
        /// </summary>
        public void TestLootTable(PrototypeId lootTableProtoRef, Player player)
        {
            Logger.Info($"--- Loot Table Test - {lootTableProtoRef.GetName()} ---");

            using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            inputSettings.Initialize(LootContext.Drop, player, null);
            inputSettings.LootRollSettings.DropChanceModifiers = LootDropChanceModifiers.PreviewOnly | LootDropChanceModifiers.IgnoreCooldown;

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            if (RollLootTable(lootTableProtoRef, inputSettings, lootResultSummary) == false)
                Logger.Warn($"TestLootTable(): Failed to roll loot table {lootTableProtoRef.GetName()}");

            if (lootResultSummary.Types != LootType.None)
                Logger.Info($"Summary: {lootResultSummary}\n{lootResultSummary.ToStringVerbose()}");

            Logger.Info("--- Loot Table Test Over ---");
        }
        
        /// <summary>
        /// Spawns loot contained in the provided <see cref="LootResultSummary"/> in the game world.
        /// </summary>
        public bool SpawnLootFromSummary(LootResultSummary lootResultSummary, LootInputSettings inputSettings, int recipientId = 1)
        {
            LootType lootTypes = lootResultSummary.Types;

            if (lootTypes == LootType.None)
                return true;

            Player player = inputSettings.Player;
            WorldEntity sourceEntity = inputSettings.SourceEntity;

            WorldEntity recipient = player?.CurrentAvatar;
            if (recipient == null) return Logger.WarnReturn(false, "SpawnLootFromSummary(): recipient == null");

            Region region = recipient.Region;
            if (region == null) return Logger.WarnReturn(false, "SpawnLootFromSummary(): region == null");

            // Trigger callbacks
            if (lootTypes.HasFlag(LootType.CallbackNode))
            {
                foreach (LootNodePrototype callbackNode in lootResultSummary.CallbackNodes)
                    callbackNode.OnResultsEvaluation(player, inputSettings.SourceEntity);
            }

            // Vanity titles
            if (lootTypes.HasFlag(LootType.VanityTitle))
            {
                foreach (PrototypeId vanityTitleProtoRef in lootResultSummary.VanityTitles)
                    player.UnlockVanityTitle(vanityTitleProtoRef);
            }

            // Vendor XP
            if (lootTypes.HasFlag(LootType.VendorXP))
            {
                foreach (VendorXPSummary vendorXPSummary in lootResultSummary.VendorXP)
                    player.AwardVendorXP(vendorXPSummary.XPAmount, vendorXPSummary.VendorProtoRef);
            }

            // Check if there is any spawnable loot
            if ((lootTypes & (LootType.Item | LootType.Agent | LootType.Credits | LootType.Currency)) == 0)
                return true;

            // Finalize vaporization (early exit if everything was vaporized)
            ulong sourceEntityId = sourceEntity != null ? sourceEntity.Id : Entity.InvalidId;

            if (LootVaporizer.VaporizeLootResultSummary(player, lootResultSummary, sourceEntityId) == false)
                return true;

            // Spawn what's left
            // Instance the loot if instanced loot is not disabled by server config (TODO: fix non-instanced loot for orbs)
            ulong restrictedToPlayerGuid = Game.CustomGameOptions.DisableInstancedLoot == false ? player.DatabaseUniqueId : 0;

            // Temp property collection for transfering properties
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            properties[PropertyEnum.RestrictedToPlayerGuid] = restrictedToPlayerGuid;

            if (inputSettings.MissionProtoRef != PrototypeId.Invalid)
                properties[PropertyEnum.MissionPrototype] = inputSettings.MissionProtoRef;

            // Determine drop source bounds
            Bounds bounds = sourceEntity != null ? sourceEntity.Bounds : recipient.Bounds;

            // Override source bounds if needed
            if (inputSettings.PositionOverride != null)
            {
                bounds = new(bounds);
                bounds.Center = inputSettings.PositionOverride.Value;
                sourceEntity = null;
            }

            Vector3 sourcePosition = bounds.Center;

            // Find positions for all drops in the summary
            _lootSpawnGrid.SetContext(region, sourcePosition, sourceEntity);

            Span<Vector3> dropPositions = stackalloc Vector3[lootResultSummary.NumDrops];
            FindDropPositions(lootResultSummary, recipient, bounds, ref dropPositions, recipientId);
            int i = 0;

            // Spawn items
            ulong regionId = region.Id;

            if (lootTypes.HasFlag(LootType.Item))
            {
                foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                    SpawnItemInternal(itemSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
            }

            // Spawn agents (orbs)
            if (lootTypes.HasFlag(LootType.Agent))
            {
                foreach (AgentSpec agentSpec in lootResultSummary.AgentSpecs)
                    SpawnAgentInternal(agentSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
            }

            // Spawn credits
            if (lootTypes.HasFlag(LootType.Credits))
            {
                foreach (int creditsAmount in lootResultSummary.Credits)
                {
                    AgentSpec agentSpec = new(_creditsItemProto.DataRef, 1, creditsAmount);
                    SpawnAgentInternal(agentSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
                }
            }

            // Spawn other currencies (items or orbs)
            if (lootTypes.HasFlag(LootType.Currency))
            {
                foreach (CurrencySpec currencySpec in lootResultSummary.Currencies)
                {
                    currencySpec.ApplyCurrency(properties);

                    if (currencySpec.IsItem)
                    {
                        // LootUtilities::FillItemSpecFromCurrencySpec()
                        ItemSpec itemSpec = new(currencySpec.AgentOrItemProtoRef, GameDatabase.LootGlobalsPrototype.RarityDefault, 1);
                        SpawnItemInternal(itemSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
                    }
                    else if (currencySpec.IsAgent)
                    {
                        AgentSpec agentSpec = new(currencySpec.AgentOrItemProtoRef, 1, 0);
                        SpawnAgentInternal(agentSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
                    }
                    else
                    {
                        Logger.Warn($"SpawnLootFromSummary(): Unsupported currency entity type for {currencySpec.AgentOrItemProtoRef.GetName()}");
                    }

                    properties.RemovePropertyRange(PropertyEnum.ItemCurrency);
                }
            }

            return true;
        }

        public bool GiveLootFromSummary(LootResultSummary lootResultSummary, Player player, PrototypeId inventoryProtoRef = PrototypeId.Invalid, bool isMissionLoot = false)
        {
            LootType lootTypes = lootResultSummary.Types;

            if (lootTypes == LootType.None)
                return true;

            bool success = true;

            // Use a list to process ItemSpec + item CurrencySpec loot together
            List<Item> itemList = ListPool<Item>.Instance.Get();

            // Reusable property collection for applying extra properties
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            EntityManager entityManager = Game.EntityManager;

            // Trigger callbacks
            if (lootTypes.HasFlag(LootType.CallbackNode))
            {
                foreach (LootNodePrototype callbackNode in lootResultSummary.CallbackNodes)
                    callbackNode.OnResultsEvaluation(player, null);
            }

            // Create items
            if (lootTypes.HasFlag(LootType.Item))
            {
                foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                {
                    using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                    settings.EntityRef = itemSpec.ItemProtoRef;
                    settings.ItemSpec = itemSpec;

                    Item item = entityManager.CreateEntity(settings) as Item;
                    if (item == null)
                    {
                        // Something went terribly terribly wrong, abandon ship
                        Logger.Warn($"GiveLootFromSummary(): Failed to create item, aborting\nItemSpec: {itemSpec}");
                        
                        foreach (Item itemToDestroy in itemList)
                            itemToDestroy.Destroy();

                        success = false;
                        goto end;
                    }

                    item.Properties[PropertyEnum.InventoryStackCount] = itemSpec.StackCount;
                    itemList.Add(item);
                }
            }

            // Create currency
            if (lootTypes.HasFlag(LootType.Currency))
            {
                foreach (CurrencySpec currencySpec in lootResultSummary.Currencies)
                {
                    currencySpec.ApplyCurrency(properties);

                    if (currencySpec.IsItem)
                    {
                        // Create currency item
                        ItemSpec itemSpec = new(currencySpec.AgentOrItemProtoRef, GameDatabase.LootGlobalsPrototype.RarityDefault, 1);

                        using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                        settings.EntityRef = currencySpec.AgentOrItemProtoRef;
                        settings.ItemSpec = itemSpec;
                        settings.Properties = properties;

                        if (player.IsInGame == false)
                            settings.OptionFlags &= ~EntitySettingsOptionFlags.EnterGame;

                        Item item = entityManager.CreateEntity(settings) as Item;
                        if (item == null)
                        {
                            // Something went terribly terribly wrong, abandon ship
                            Logger.Warn($"GiveLootFromSummary(): Failed to create currency item, aborting\nItemSpec: {itemSpec}\nCurrencySpec: {currencySpec}");

                            foreach (Item itemToDestroy in itemList)
                                itemToDestroy.Destroy();

                            success = false;
                            goto end;
                        }

                        item.Properties[PropertyEnum.InventoryStackCount] = itemSpec.StackCount;
                        itemList.Add(item);
                    }
                    else if (currencySpec.IsAgent)
                    {
                        // Agents are always spawned and not given (this whole system is such a disaster)
                        SpawnAgentForPlayer(currencySpec, player, properties);
                    }

                    properties.RemovePropertyRange(PropertyEnum.ItemCurrency);
                }
            }

            // Give regular and items to the player
            foreach (Item item in itemList)
            {
                InventoryResult result = player.AcquireItem(item, inventoryProtoRef);
                if (result != InventoryResult.Success)
                {
                    // Something went terribly terribly wrong, abandon ship
                    Logger.Warn($"GiveLootFromSummary(): Failed to give item, aborting\nItem: {item}");

                    foreach (Item itemToDestroy in itemList)
                        itemToDestroy.Destroy();

                    success = false;
                    goto end;
                }
            }

            // Now spawn regular agents (i.e. orbs)
            if (lootTypes.HasFlag(LootType.Agent))
            {
                foreach (AgentSpec agentSpec in lootResultSummary.AgentSpecs)
                    SpawnAgentForPlayer(agentSpec, player, properties);
            }

            // Credits
            if (lootTypes.HasFlag(LootType.Credits))
            {
                foreach (int creditsAmount in lootResultSummary.Credits)
                {
                    AgentSpec agentSpec = new(_creditsItemProto.DataRef, 1, creditsAmount);
                    SpawnAgentForPlayer(agentSpec, player, properties);
                }
            }

            // Vanity titles
            if (lootTypes.HasFlag(LootType.VanityTitle))
            {
                foreach (PrototypeId vanityTitleProtoRef in lootResultSummary.VanityTitles)
                    player.UnlockVanityTitle(vanityTitleProtoRef);
            }

            // Vendor XP
            if (lootTypes.HasFlag(LootType.VendorXP))
            {
                foreach (VendorXPSummary vendorXPSummary in lootResultSummary.VendorXP)
                    player.AwardVendorXP(vendorXPSummary.XPAmount, vendorXPSummary.VendorProtoRef);
            }

            // Mission-exclusive rewards: experience, endurance / health bonuses, power points
            if (isMissionLoot)
            {
                if (lootTypes.HasFlag(LootType.Experience))
                {
                    Avatar avatar = player.CurrentAvatar;
                    avatar?.AwardXP(lootResultSummary.Experience, 0, false);
                }

                if (lootTypes.HasFlag(LootType.HealthBonus))
                {
                    // TODO for 1.48
                    Logger.Warn("GiveLootFromSummary(): HealthBonus rewards are not yet implemented");
                }

                if (lootTypes.HasFlag(LootType.EnduranceBonus))
                {
                    // TODO for 1.48
                    Logger.Warn("GiveLootFromSummary(): EnduranceBonus rewards are not yet implemented");
                }

                if (lootTypes.HasFlag(LootType.PowerPoints))
                {
                    // TODO for 1.48
                    Logger.Warn("GiveLootFromSummary(): PowerPoints rewards are not yet implemented");
                }
            }
            else
            {
                if ((lootTypes & (LootType.Experience | LootType.HealthBonus | LootType.EnduranceBonus | LootType.PowerPoints)) != 0)
                {
                    Logger.Warn($"GiveLootFromSummary(): Mission-only loot types found in a non-mission summary, Types=[{lootResultSummary.Types}]");
                }
            }

            // NOTE: We use goto here because returning a list to the pool while it's
            // being iterated will clear it and cause it to be modified during iteration.
            end:
            ListPool<Item>.Instance.Return(itemList);
            return success;
        }

        public bool SpawnItem(PrototypeId itemProtoRef, LootContext lootContext, Player player, WorldEntity sourceEntity)
        {
            ItemSpec itemSpec = CreateItemSpec(itemProtoRef, lootContext, player);
            if (itemSpec == null)
                return Logger.WarnReturn(false, $"SpawnItem(): Failed to create an ItemSpec for {itemProtoRef.GetName()}");

            using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            inputSettings.Initialize(LootContext.Drop, player, sourceEntity);

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            LootResult lootResult = new(itemSpec);
            lootResultSummary.Add(lootResult);

            return SpawnLootFromSummary(lootResultSummary, inputSettings);
        }

        /// <summary>
        /// Creates and gives a new item to the provided <see cref="Player"/>.
        /// </summary>
        public bool GiveItem(PrototypeId itemProtoRef, LootContext lootContext, Player player)
        {
            ItemSpec itemSpec = CreateItemSpec(itemProtoRef, lootContext, player);
            if (itemSpec == null)
                return Logger.WarnReturn(false, $"GiveItem(): Failed to create an ItemSpec for {itemProtoRef.GetName()}");

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            LootResult lootResult = new(itemSpec);
            lootResultSummary.Add(lootResult);

            return GiveLootFromSummary(lootResultSummary, player, PrototypeId.Invalid);
        }

        /// <summary>
        /// Creates an <see cref="ItemSpec"/> for the provided <see cref="PrototypeId"/>.
        /// </summary>
        public ItemSpec CreateItemSpec(PrototypeId itemProtoRef, LootContext lootContext, Player player, int level = 1)
        {
            ItemPrototype itemProto = itemProtoRef.As<ItemPrototype>();
            if (itemProto == null)
                return Logger.WarnReturn<ItemSpec>(null, "CreateItemSpec(): itemProto == null");

            if (DataDirectory.Instance.PrototypeIsAbstract(itemProtoRef))
                return Logger.WarnReturn<ItemSpec>(null, $"CreateItemSpec(): {itemProtoRef.GetName()} is abstract, which is currently not supported for this");

            _resolver.SetContext(lootContext, player);

            AvatarPrototype avatarProto = player?.CurrentAvatar?.AvatarPrototype;

            using DropFilterArguments filterArgs = ObjectPoolManager.Instance.Get<DropFilterArguments>();
            filterArgs.ItemProto = itemProto;
            filterArgs.Level = level;
            filterArgs.RollFor = _resolver.ResolveAvatarPrototype(avatarProto, true, 1f).DataRef;
            filterArgs.Rarity = _resolver.ResolveRarity(null, level, itemProto);
            filterArgs.Slot = itemProto.GetInventorySlotForAgent(avatarProto);

            if (itemProto.MakeRestrictionsDroppable(filterArgs, RestrictionTestFlags.All, out _) == false)
                return Logger.WarnReturn<ItemSpec>(null, $"CreateItemSpec(): Failed to make item {itemProto} droppable");

            // Finalize spec
            ItemSpec itemSpec = new(filterArgs.ItemProto.DataRef, filterArgs.Rarity, filterArgs.Level, 0,
                Array.Empty<AffixSpec>(), _resolver.Random.Next());

            if (LootUtilities.UpdateAffixes(_resolver, filterArgs, AffixCountBehavior.Roll, itemSpec, null).HasFlag(MutationResults.Error))
                return Logger.WarnReturn<ItemSpec>(null, $"CreateItemSpec(): Failed to update affixes for {itemProto}");

            return itemSpec;
        }

        /// <summary>
        /// Rolls the specified loot table and fills the provided <see cref="LootResultSummary"/> with results.
        /// </summary>
        private bool RollLootTable(PrototypeId lootTableProtoRef, LootInputSettings inputSettings, LootResultSummary lootResultSummary)
        {
            LootTablePrototype lootTableProto = lootTableProtoRef.As<LootTablePrototype>();
            if (lootTableProto == null) return Logger.WarnReturn(false, "RollLootTable(): lootTableProto == null");

            _resolver.SetContext(inputSettings.LootContext, inputSettings.Player, inputSettings.SourceEntity);

            LootRollResult result = lootTableProto.RollLootTable(inputSettings.LootRollSettings, _resolver);
            if (result.HasFlag(LootRollResult.Success))
                _resolver.FillLootResultSummary(lootResultSummary);

            return true;
        }

        /// <summary>
        /// Spawns an <see cref="Item"/> in the game world.
        /// </summary>
        private bool SpawnItemInternal(ItemSpec itemSpec, ulong regionId, Vector3 position, ulong sourceEntityId, Vector3 sourcePosition, PropertyCollection properties)
        {
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (itemProto == null) return Logger.WarnReturn(false, "SpawnItemInternal(): itemProto == null");

            if (itemProto.IsLiveTuningEnabled() == false)
                return false;

            // Create entity
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = itemSpec.ItemProtoRef;
            settings.RegionId = regionId;
            settings.Position = position;
            settings.SourceEntityId = sourceEntityId;
            settings.SourcePosition = sourcePosition;
            settings.Properties = properties;
            settings.Properties[PropertyEnum.InventoryStackCount] = itemSpec.StackCount;

            settings.ItemSpec = itemSpec;
            settings.Lifespan = itemProto.GetExpirationTime(itemSpec.RarityProtoRef);

            Item item = Game.EntityManager.CreateEntity(settings) as Item;

            // Clean up properties (even if we failed to create the item for some reason)
            settings.Properties.RemoveProperty(PropertyEnum.InventoryStackCount);

            if (item == null) return Logger.WarnReturn(false, "SpawnItemInternal(): item == null");

            return true;
        }

        private bool SpawnAgentForPlayer(in CurrencySpec currencySpec, Player player, PropertyCollection agentProperties)
        {
            // Used when "giving" rewards
            AgentSpec agentSpec = new(currencySpec.AgentOrItemProtoRef, 1, 0);
            return SpawnAgentForPlayer(agentSpec, player, agentProperties);
        }

        private bool SpawnAgentForPlayer(in AgentSpec agentSpec, Player player, PropertyCollection agentProperties)
        {
            // Used when "giving" rewards
            AgentPrototype agentProto = agentSpec.AgentProtoRef.As<AgentPrototype>();
            if (agentProto == null) return Logger.WarnReturn(false, "SpawnAgentForPlayer(): agentProto == null");

            // We need a valid avatar that is in the world to spawn something for a player
            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "SpawnAgentForPlayer(): avatar == null");

            Region region = avatar.Region;
            if (region == null) return Logger.WarnReturn(false, "SpawnAgentForPlayer(): region == null");

            if (agentProto.Properties != null && agentProto.Properties[PropertyEnum.RestrictedToPlayer])
                agentProperties[PropertyEnum.RestrictedToPlayerGuid] = player.DatabaseUniqueId;

            _lootSpawnGrid.SetContext(region, avatar.RegionLocation.Position, null);
            Vector3 dropPosition = FindDropPosition(agentProto, avatar, avatar.Bounds, 1);

            bool success = SpawnAgentInternal(agentSpec, region.Id, dropPosition, avatar.Id, avatar.RegionLocation.Position, agentProperties);

            // Clean up instancing
            agentProperties.RemoveProperty(PropertyEnum.RestrictedToPlayerGuid);

            return success;
        }

        private bool SpawnAgentInternal(in AgentSpec agentSpec, ulong regionId, Vector3 position, ulong sourceEntityId, Vector3 sourcePosition, PropertyCollection properties)
        {
            // TODO: figure out a way to move functionality shared with SpawnItemInternal to a separate method?

            WorldEntityPrototype agentProto = agentSpec.AgentProtoRef.As<WorldEntityPrototype>();
            if (agentProto == null) return Logger.WarnReturn(false, "SpawnAgentInternal(): agentProto == null");

            if (agentProto.IsLiveTuningEnabled() == false)
                return false;

            // Create entity
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = agentSpec.AgentProtoRef;
            settings.RegionId = regionId;
            settings.Position = position;
            settings.SourceEntityId = sourceEntityId;
            settings.SourcePosition = sourcePosition;

            settings.Properties = properties;
            settings.Properties[PropertyEnum.CharacterLevel] = agentSpec.AgentLevel;
            settings.Properties[PropertyEnum.CombatLevel] = agentSpec.AgentLevel;

            if (agentSpec.CreditsAmount > 0)
                settings.Properties[PropertyEnum.ItemCurrency, GameDatabase.CurrencyGlobalsPrototype.Credits] = agentSpec.CreditsAmount;

            // NOTE: Some loot tables (e.g. InanimateObjectsCh03GarbageBags) spawn destructible props. They are not agents,
            // but they still go through here, which means we have to use WorldEntity instead of Agent.
            WorldEntity agent = Game.EntityManager.CreateEntity(settings) as WorldEntity;

            // Clean up properties (even if we failed to create the agent for some reason)
            settings.Properties.RemoveProperty(PropertyEnum.CharacterLevel);
            settings.Properties.RemoveProperty(PropertyEnum.CombatLevel);
            settings.Properties.RemovePropertyRange(PropertyEnum.ItemCurrency);

            if (agent == null) return Logger.WarnReturn(false, "SpawnAgentInternal(): agent == null");

            return true;
        }

        #region Drop Positioning

        private void FindDropPositions(LootResultSummary lootResultSummary, WorldEntity recipient, Bounds bounds, ref Span<Vector3> dropPositions, int recipientId)
        {
            // Find drop positions for each item
            int i = 0;

            // NOTE: The order here has to be the same as SpawnLootFromSummary()
            foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                dropPositions[i++] = FindDropPosition(itemSpec, recipient, bounds, recipientId);

            foreach (AgentSpec agentSpec in lootResultSummary.AgentSpecs)
                dropPositions[i++] = FindDropPosition(agentSpec, recipient, bounds, recipientId);

            foreach (int credits in lootResultSummary.Credits)
                dropPositions[i++] = FindDropPosition(_creditsItemProto, recipient, bounds, recipientId);

            foreach (CurrencySpec currencySpec in lootResultSummary.Currencies)
                dropPositions[i++] = FindDropPosition(currencySpec, recipient, bounds, recipientId);
        }

        private Vector3 FindDropPosition(ItemSpec itemSpec, WorldEntity recipient, Bounds bounds, int recipientId)
        {
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (itemProto == null)
                return Logger.WarnReturn(bounds.Center, "FindDropPosition(): itemProto == null");

            return FindDropPosition(itemProto, recipient, bounds, recipientId);
        }

        private Vector3 FindDropPosition(in AgentSpec agentSpec, WorldEntity recipient, Bounds bounds, int recipientId)
        {
            // NOTE: Some loot tables (e.g. InanimateObjectsCh03GarbageBags) spawn destructible props. They are not agents,
            // but they still go through here, which means we have to use WorldEntityPrototype instead of AgentPrototype.
            WorldEntityPrototype agentProto = agentSpec.AgentProtoRef.As<WorldEntityPrototype>();
            if (agentProto == null)
                return Logger.WarnReturn(bounds.Center, $"FindDropPosition(): Failed to retrieve prototype for AgentSpec [{agentSpec}]");

            return FindDropPosition(agentProto, recipient, bounds, recipientId);
        }

        private Vector3 FindDropPosition(in CurrencySpec currencySpec, WorldEntity recipient, Bounds bounds, int recipientId)
        {
            WorldEntityPrototype worldEntityProto = currencySpec.AgentOrItemProtoRef.As<WorldEntityPrototype>();
            if (worldEntityProto == null)
                return Logger.WarnReturn(bounds.Center, "FindDropPosition(): worldEntityProto == null");

            return FindDropPosition(worldEntityProto, recipient, bounds, recipientId);
        }

        private Vector3 FindDropPosition(WorldEntityPrototype dropEntityProto, WorldEntity recipient, Bounds bounds, int recipientId)
        {
            // Fall back to the center of provided bounds if something goes wrong
            Vector3 sourcePosition = bounds.Center;

            // TODO: Dropping without a recipient? It seems to be optional for LootLocationTable
            if (recipient == null) return Logger.WarnReturn(sourcePosition, "FindDropPosition(): recipient == null");

            Region region = recipient.Region;
            if (region == null) return Logger.WarnReturn(sourcePosition, "FindDropPosition(): region == null");

            // Get the loot location table for this drop
            PrototypeId lootLocationTableProtoRef = dropEntityProto.Properties[PropertyEnum.LootSpawnPrototype];
            if (lootLocationTableProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(sourcePosition, "FindDropPosition(): lootLocationTableProtoRef == PrototypeId.Invalid");

            var lootLocationTableProto = lootLocationTableProtoRef.As<LootLocationTablePrototype>();
            if (lootLocationTableProto == null) return Logger.WarnReturn(sourcePosition, "FindDropPosition(): lootLocationTable == null");

            // Roll it
            using LootLocationData lootLocationData = ObjectPoolManager.Instance.Get<LootLocationData>();
            lootLocationData.Initialize(Game, bounds.Center, recipient);
            lootLocationTableProto.Roll(lootLocationData);

            // Drop in place if required by location settings
            if (lootLocationData.DropInPlace)
                return sourcePosition;

            // Use the loot grid to put the item in a spiral with the rolled location settings
            GRandom rng = Game.Random;

            float boundsHeight = bounds.HalfHeight * 2f;

            float startOrientation = rng.NextFloat(MathHelper.TwoPi);
            float orientation = startOrientation;

            float cellRadius = _lootSpawnGrid.CellRadius;
            float cellDiameter = _lootSpawnGrid.CellDiameter;

            float radius = MathF.Max(bounds.Radius, lootLocationData.MinRadius) + cellRadius;

            while (radius < LootSpawnGrid.MaxSpiralRadius)
            {
                // Calculate current position within our spiral and try to get a matching grid position
                Vector3 spiralDirection = new(MathF.Cos(orientation), MathF.Sin(orientation), 0f);
                Vector3 spiralOffset = lootLocationData.Offset * (1f - radius / LootSpawnGrid.MaxSpiralRadius);
                Vector3 dropPositionWithinSpiral = spiralDirection * radius + spiralOffset;

                if (_lootSpawnGrid.TryGetDropPosition(dropPositionWithinSpiral, dropEntityProto, recipientId, boundsHeight, out Vector3 dropPosition))
                    return dropPosition;

                // Move further along the circumference of the current radius randomly
                float orientationStep = MathF.Asin(cellRadius / (cellRadius + radius)) * 2f;
                int numSteps = rng.Next(1, (int)(MathHelper.TwoPi / orientationStep));
                orientation += orientationStep * numSteps;

                // Increase the radius once we move an entire circle
                if (orientation - startOrientation > MathHelper.TwoPi)
                {
                    orientation = startOrientation;
                    radius += cellDiameter;
                }
            }

            // Default to the source position if no more space on the spiral
            return sourcePosition;
        }

        #endregion
    }
}
