using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot.Specs;
using MHServerEmu.Games.Navi;
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
        private readonly WorldEntityPrototype _creditsItemProto; 

        public Game Game { get; }

        /// <summary>
        /// Constructs a new <see cref="LootManager"/> for the provided <see cref="Games.Game"/>.
        /// </summary>
        public LootManager(Game game)
        {
            Game = game;

            _resolver = new(game.Random);
            _creditsItemProto = GameDatabase.GlobalsPrototype.CreditsItemPrototype.As<WorldEntityPrototype>();
        }

        /// <summary>
        /// Rolls the specified loot table and drops loot from the provided source <see cref="WorldEntity"/>.
        /// </summary>
        public void SpawnLootFromTable(PrototypeId lootTableProtoRef, LootInputSettings inputSettings)
        {
            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            RollLootTable(lootTableProtoRef, inputSettings, lootResultSummary);

            if (lootResultSummary.HasAnyResult == false) return;

            SpawnLootFromSummary(lootResultSummary, inputSettings);
        }

        /// <summary>
        /// Does a test roll of the specified loot table for the provided <see cref="Player"/>.
        /// </summary>
        public void TestLootTable(PrototypeId lootTableProtoRef, Player player)
        {
            Logger.Info($"--- Loot Table Test - {lootTableProtoRef.GetName()} ---");

            using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            inputSettings.Initialize(LootContext.Drop, player, null);

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
        public bool SpawnLootFromSummary(LootResultSummary lootResultSummary, LootInputSettings inputSettings)
        {
            if (lootResultSummary.Types == LootType.None)
                return true;

            Player player = inputSettings.Player;
            WorldEntity sourceEntity = inputSettings.SourceEntity;

            WorldEntity recipient = player?.CurrentAvatar;
            if (recipient == null) return Logger.WarnReturn(false, "SpawnLootFromSummary(): recipient == null");

            Region region = recipient.Region;
            if (region == null) return Logger.WarnReturn(false, "SpawnLootFromSummary(): region == null");

            // Instance the loot if instanced loot is not disabled by server config
            ulong restrictedToPlayerGuid = Game.CustomGameOptions.DisableInstancedLoot == false ? player.DatabaseUniqueId : 0;

            // Temp property collection for transfering properties
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            properties[PropertyEnum.RestrictedToPlayerGuid] = restrictedToPlayerGuid;

            // Trigger callbacks
            if (lootResultSummary.Types.HasFlag(LootType.CallbackNode))
            {
                foreach (LootNodePrototype callbackNode in lootResultSummary.CallbackNodes)
                    callbackNode.OnResultsEvaluation(player, inputSettings.SourceEntity);
            }

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
            Span<Vector3> dropPositions = stackalloc Vector3[lootResultSummary.NumDrops];
            FindDropPositions(lootResultSummary, recipient, bounds, ref dropPositions);
            int i = 0;

            // Spawn items
            ulong regionId = region.Id;
            ulong sourceEntityId = sourceEntity != null ? sourceEntity.Id : Entity.InvalidId;

            if (lootResultSummary.Types.HasFlag(LootType.Item))
            {
                foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                    SpawnItemInternal(itemSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
            }

            // Spawn agents (orbs)
            if (lootResultSummary.Types.HasFlag(LootType.Agent))
            {
                foreach (AgentSpec agentSpec in lootResultSummary.AgentSpecs)
                    SpawnAgentInternal(agentSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
            }

            // Spawn credits
            if (lootResultSummary.Types.HasFlag(LootType.Credits))
            {
                foreach (int creditsAmount in lootResultSummary.Credits)
                {
                    AgentSpec agentSpec = new(_creditsItemProto.DataRef, 1, creditsAmount);
                    SpawnAgentInternal(agentSpec, regionId, dropPositions[i++], sourceEntityId, sourcePosition, properties);
                }
            }

            // Spawn other currencies (items or orbs)
            if (lootResultSummary.Types.HasFlag(LootType.Currency))
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

        public bool SpawnItem(PrototypeId itemProtoRef, Player player, WorldEntity sourceEntity)
        {
            ItemSpec itemSpec = CreateItemSpec(itemProtoRef);
            if (itemSpec == null)
                return Logger.WarnReturn(false, $"SpawnItem(): Failed to create an ItemSpec for {itemProtoRef.GetName()}");

            using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            inputSettings.Initialize(LootContext.Drop, player, sourceEntity);

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            LootResult lootResult = new(itemSpec);
            lootResultSummary.Add(lootResult);

            SpawnLootFromSummary(lootResultSummary, inputSettings);
            return true;
        }

        /// <summary>
        /// Creates and gives a new item to the provided <see cref="Player"/>.
        /// </summary>
        public Item GiveItem(PrototypeId itemProtoRef, Player player)
        {
            // TODO: Do the itemProtoRef -> LootResultSummary -> Give flow, similar to spawning

            ItemSpec itemSpec = CreateItemSpec(itemProtoRef);
            if (itemSpec == null)
                return Logger.WarnReturn<Item>(null, $"GiveItem(): Failed to create an ItemSpec for {itemProtoRef.GetName()}");

            Inventory inventory = player.GetInventory(InventoryConvenienceLabel.General);
            if (inventory == null) return Logger.WarnReturn<Item>(null, "GiveItem(): inventory == null");

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = itemProtoRef;
            settings.InventoryLocation = new(player.Id, inventory.PrototypeDataRef);
            settings.ItemSpec = CreateItemSpec(itemProtoRef);

            return Game.EntityManager.CreateEntity(settings) as Item;
        }

        /// <summary>
        /// Creates an <see cref="ItemSpec"/> for the provided <see cref="PrototypeId"/>.
        /// </summary>
        public static ItemSpec CreateItemSpec(PrototypeId itemProtoRef)
        {
            if (DataDirectory.Instance.PrototypeIsA<ItemPrototype>(itemProtoRef) == false)
                return Logger.WarnReturn<ItemSpec>(null, $"CreateItemSpec(): {itemProtoRef.GetName()} [{itemProtoRef}] is not an item prototype ref");

            // Create a dummy item spec for now
            PrototypeId rarityProtoRef = GameDatabase.LootGlobalsPrototype.RarityDefault;  // R1Common
            int itemLevel = 1;
            int creditsAmount = 0;
            IEnumerable<AffixSpec> affixSpecs = Array.Empty<AffixSpec>();
            int seed = 1;
            PrototypeId equippableBy = PrototypeId.Invalid;

            return new(itemProtoRef, rarityProtoRef, itemLevel, creditsAmount, affixSpecs, seed, equippableBy);
        }

        /// <summary>
        /// Rolls the specified loot table and fills the provided <see cref="LootResultSummary"/> with results.
        /// </summary>
        private bool RollLootTable(PrototypeId lootTableProtoRef, LootInputSettings inputSettings, LootResultSummary lootResultSummary)
        {
            LootTablePrototype lootTableProto = lootTableProtoRef.As<LootTablePrototype>();
            if (lootTableProto == null) return Logger.WarnReturn(false, "RollLootTable(): lootTableProto == null");

            _resolver.SetContext(inputSettings.LootContext, inputSettings.Player);

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
            if (itemProto == null)
                return Logger.WarnReturn(false, "SpawnItemInternal(): itemProto == null");

            // Create entity
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = itemSpec.ItemProtoRef;
            settings.RegionId = regionId;
            settings.Position = position;
            settings.SourceEntityId = sourceEntityId;
            settings.SourcePosition = sourcePosition;
            settings.Properties = properties;

            settings.ItemSpec = itemSpec;
            settings.Lifespan = itemProto.GetExpirationTime(itemSpec.RarityProtoRef);

            Item item = Game.EntityManager.CreateEntity(settings) as Item;
            if (item == null) return Logger.WarnReturn(false, "SpawnItemInternal(): item == null");

            return true;
        }

        private bool SpawnAgentInternal(in AgentSpec agentSpec, ulong regionId, Vector3 position, ulong sourceEntityId, Vector3 sourcePosition, PropertyCollection properties)
        {
            // TODO: figure out a way to move functionality shared with SpawnItemInternal to a separate method?

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

            Agent agent = Game.EntityManager.CreateEntity(settings) as Agent;

            // Clean up properties (even if we failed to create the agent for some reason)
            settings.Properties.RemoveProperty(PropertyEnum.CharacterLevel);
            settings.Properties.RemoveProperty(PropertyEnum.CombatLevel);
            settings.Properties.RemovePropertyRange(PropertyEnum.ItemCurrency);

            if (agent == null) return Logger.WarnReturn(false, "SpawnAgentInternal(): agent == null");

            return true;
        }

        #region Drop Positioning

        private void FindDropPositions(LootResultSummary lootResultSummary, WorldEntity recipient, Bounds bounds, ref Span<Vector3> dropPositions)
        {
            // Calculate max drop radius
            float maxRadius = MathF.Min(300f, 75f + 25f * lootResultSummary.NumDrops);

            // Find drop positions for each item
            int i = 0;

            // NOTE: The order here has to be the same as SpawnLootFromSummary()
            foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                dropPositions[i++] = FindDropPosition(itemSpec, recipient, bounds, maxRadius);

            foreach (AgentSpec agentSpec in lootResultSummary.AgentSpecs)
                dropPositions[i++] = FindDropPosition(agentSpec, recipient, bounds, maxRadius);

            foreach (int credits in lootResultSummary.Credits)
                dropPositions[i++] = FindDropPosition(_creditsItemProto, recipient, bounds, maxRadius);

            foreach (CurrencySpec currencySpec in lootResultSummary.Currencies)
                dropPositions[i++] = FindDropPosition(currencySpec, recipient, bounds, maxRadius);
        }

        private Vector3 FindDropPosition(ItemSpec itemSpec, WorldEntity recipient, Bounds bounds, float maxRadius)
        {
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (itemProto == null)
                return Logger.WarnReturn(bounds.Center, "FindDropPosition(): itemProto == null");

            return FindDropPosition(itemProto, recipient, bounds, maxRadius);
        }

        private Vector3 FindDropPosition(in AgentSpec agentSpec, WorldEntity recipient, Bounds bounds, float maxRadius)
        {
            AgentPrototype agentProto = agentSpec.AgentProtoRef.As<AgentPrototype>();
            if (agentProto == null)
                return Logger.WarnReturn(bounds.Center, "FindDropPosition(): agentProto == null");

            return FindDropPosition(agentProto, recipient, bounds, maxRadius);
        }

        private Vector3 FindDropPosition(in CurrencySpec currencySpec, WorldEntity recipient, Bounds bounds, float maxRadius)
        {
            WorldEntityPrototype worldEntityProto = currencySpec.AgentOrItemProtoRef.As<WorldEntityPrototype>();
            if (worldEntityProto == null)
                return Logger.WarnReturn(bounds.Center, "FindDropPosition(): worldEntityProto == null");

            return FindDropPosition(worldEntityProto, recipient, bounds, maxRadius);
        }

        private Vector3 FindDropPosition(WorldEntityPrototype dropEntityProto, WorldEntity recipient, Bounds bounds, float maxRadius)
        {
            // Fall back to the center of provided bounds if something goes wrong
            Vector3 boundsCenter = bounds.Center;

            // TODO: Dropping without a recipient? It seems to be optional for LootLocationTable
            if (recipient == null) return Logger.WarnReturn(boundsCenter, "FindDropPosition(): recipient == null");

            Region region = recipient.Region;
            if (region == null) return Logger.WarnReturn(boundsCenter, "FindDropPosition(): region == null");

            // Get the loot location table for this drop
            // NOTE: Loot location tables don't work properly with random locations, we need to implement some kind
            // of distribution system that gradually fills space from min radius to max to fully make use of this data.
            PrototypeId lootLocationTableProtoRef = dropEntityProto.Properties[PropertyEnum.LootSpawnPrototype];
            if (lootLocationTableProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(boundsCenter, "FindDropPosition(): lootLocationTableProtoRef == PrototypeId.Invalid");

            var lootLocationTableProto = lootLocationTableProtoRef.As<LootLocationTablePrototype>();
            if (lootLocationTableProto == null) return Logger.WarnReturn(boundsCenter, "FindDropPosition(): lootLocationTable == null");

            // Roll it
            using LootLocationData lootLocationData = ObjectPoolManager.Instance.Get<LootLocationData>();
            lootLocationData.Initialize(Game, bounds.Center, recipient);
            lootLocationTableProto.Roll(lootLocationData);

            if (lootLocationData.DropInPlace)
                return boundsCenter;

            float minRadius = MathF.Max(bounds.Radius, lootLocationData.MinRadius);

            // If minRadius is equal to maxRadius, ChooseRandomPositionNearPoint() sometimes fails, so we need to add some padding
            if (minRadius >= maxRadius)
                maxRadius = minRadius + 10f;

            if (region.ChooseRandomPositionNearPoint(bounds, PathFlags.Walk, PositionCheckFlags.PreferNoEntity,
                BlockingCheckFlags.CheckSpawns, minRadius, maxRadius, out Vector3 dropPosition) == false)
            {
                return boundsCenter;
            }

            return dropPosition;
        }

        #endregion
    }
}
