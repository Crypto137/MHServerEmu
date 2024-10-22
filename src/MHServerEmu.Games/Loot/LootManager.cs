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
        private readonly PrototypeId _creditsItemProtoRef; 

        public Game Game { get; }

        /// <summary>
        /// Constructs a new <see cref="LootManager"/> for the provided <see cref="Games.Game"/>.
        /// </summary>
        public LootManager(Game game)
        {
            Game = game;

            _resolver = new(game.Random);
            _creditsItemProtoRef = GameDatabase.GlobalsPrototype.CreditsItemPrototype;
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
        public void SpawnLootFromSummary(LootResultSummary lootResultSummary, LootInputSettings inputSettings)
        {
            if (lootResultSummary.Types == LootType.None)
                return;

            Player player = inputSettings.Player;
            WorldEntity sourceEntity = inputSettings.SourceEntity;

            // Calculate drop radius
            int numDrops = lootResultSummary.ItemSpecs.Count + lootResultSummary.AgentSpecs.Count;
            float maxDropRadius = MathF.Min(300f, 75f + 25f * numDrops);

            // Instance the loot if we have a player provided and instanced loot is not disabled by server config
            ulong restrictedToPlayerGuid = player != null && Game.CustomGameOptions.DisableInstancedLoot == false ? player.DatabaseUniqueId : 0;

            // Temp property collection for transfering properties
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            properties[PropertyEnum.RestrictedToPlayerGuid] = restrictedToPlayerGuid;

            // Trigger callbacks
            if (lootResultSummary.Types.HasFlag(LootType.CallbackNode))
            {
                foreach (LootNodePrototype callbackNode in lootResultSummary.CallbackNodes)
                    callbackNode.OnResultsEvaluation(player, sourceEntity);
            }

            // Spawn items
            if (lootResultSummary.Types.HasFlag(LootType.Item))
            {
                foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                    SpawnItem(itemSpec, sourceEntity, maxDropRadius, properties);
            }

            // Spawn agents (orbs)
            if (lootResultSummary.Types.HasFlag(LootType.Agent))
            {
                foreach (AgentSpec agentSpec in lootResultSummary.AgentSpecs)
                    SpawnAgent(agentSpec, sourceEntity, maxDropRadius, properties);
            }

            // Spawn credits
            if (lootResultSummary.Types.HasFlag(LootType.Credits))
            {
                foreach (int creditsAmount in lootResultSummary.Credits)
                {
                    AgentSpec agentSpec = new(_creditsItemProtoRef, 1, creditsAmount);
                    SpawnAgent(agentSpec, sourceEntity, maxDropRadius, properties);
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
                        SpawnItem(itemSpec, sourceEntity, maxDropRadius, properties);
                    }
                    else if (currencySpec.IsAgent)
                    {
                        AgentSpec agentSpec = new(currencySpec.AgentOrItemProtoRef, 1, 0);
                        SpawnAgent(agentSpec, sourceEntity, maxDropRadius, properties);
                    }
                    else
                    {
                        Logger.Warn($"SpawnLootFromSummary(): Unsupported currency type for {currencySpec.CurrencyRef.GetName()}");
                    }

                    properties.RemovePropertyRange(PropertyEnum.ItemCurrency);
                }
            }
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
        private bool SpawnItem(ItemSpec itemSpec, WorldEntity sourceEntity, float dropRadius, PropertyCollection properties)
        {
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (itemProto == null)
                return Logger.WarnReturn(false, "SpawnItem(): itemProto == null");

            // Find a position for this item
            if (FindDropPosition(itemProto, sourceEntity, dropRadius, out Vector3 dropPosition) == false)
                return Logger.WarnReturn(false, $"SpawnItem(): Failed to find position to spawn item {itemSpec.ItemProtoRef.GetName()}");

            // Create entity
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = itemSpec.ItemProtoRef;
            settings.RegionId = sourceEntity.RegionLocation.RegionId;
            settings.Position = dropPosition;
            settings.SourceEntityId = sourceEntity.Id;
            settings.SourcePosition = sourceEntity.RegionLocation.Position;
            settings.ItemSpec = itemSpec;
            settings.Lifespan = itemProto.GetExpirationTime(itemSpec.RarityProtoRef);
            settings.Properties = properties;

            Item item = Game.EntityManager.CreateEntity(settings) as Item;
            if (item == null) return Logger.WarnReturn(false, "SpawnItem(): item == null");

            return true;
        }

        private bool SpawnAgent(in AgentSpec agentSpec, WorldEntity sourceEntity, float dropRadius, PropertyCollection properties)
        {
            // this looks very similar to SpawnItem, TODO: move common functionality to a separate method
            AgentPrototype agentProto = agentSpec.AgentProtoRef.As<AgentPrototype>();
            if (agentProto == null) return Logger.WarnReturn(false, "SpawnAgent(): agentProto == null");

            // Pick a position for this agent
            if (FindDropPosition(agentProto, sourceEntity, dropRadius, out Vector3 dropPosition) == false)
                return Logger.WarnReturn(false, $"SpawnAgent(): Failed to find position to spawn agent {agentSpec}");

            // Create entity
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = agentSpec.AgentProtoRef;
            settings.RegionId = sourceEntity.RegionLocation.RegionId;
            settings.Position = dropPosition;
            settings.SourceEntityId = sourceEntity.Id;
            settings.SourcePosition = sourceEntity.RegionLocation.Position;

            settings.Properties = properties;
            settings.Properties[PropertyEnum.CharacterLevel] = agentSpec.AgentLevel;
            settings.Properties[PropertyEnum.CombatLevel] = agentSpec.AgentLevel;

            if (agentSpec.CreditsAmount > 0)
                settings.Properties[PropertyEnum.ItemCurrency, GameDatabase.CurrencyGlobalsPrototype.Credits] = agentSpec.CreditsAmount;

            Agent agent = Game.EntityManager.CreateEntity(settings) as Agent;
            if (agent == null) return Logger.WarnReturn(false, "SpawnAgent(): item == null");

            // Clean up properties
            settings.Properties.RemoveProperty(PropertyEnum.CharacterLevel);
            settings.Properties.RemoveProperty(PropertyEnum.CombatLevel);
            settings.Properties.RemovePropertyRange(PropertyEnum.ItemCurrency);

            return true;
        }

        private bool FindDropPosition(WorldEntityPrototype dropEntityProto, WorldEntity sourceEntity, float maxRadius, out Vector3 dropPosition)
        {
            dropPosition = Vector3.Zero;

            // TODO: Dropping without a source entity? It seems to be optional for LootLocationTable
            if (sourceEntity == null) return Logger.WarnReturn(false, "FindDropPosition(): sourceEntity == null");
            Bounds bounds = sourceEntity.Bounds;

            // Get the loot location table for this drop
            // NOTE: Loot location tables don't work properly with random locations, we need to implement some kind
            // of distribution system that gradually fills space from min radius to max to fully make use of this data.
            PrototypeId lootLocationTableProtoRef = dropEntityProto.Properties[PropertyEnum.LootSpawnPrototype];
            if (lootLocationTableProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "FindDropPosition(): lootLocationTableProtoRef == PrototypeId.Invalid");

            var lootLocationTableProto = lootLocationTableProtoRef.As<LootLocationTablePrototype>();
            if (lootLocationTableProto == null) return Logger.WarnReturn(false, "FindDropPosition(): lootLocationTable == null");

            // Roll it
            using LootLocationData lootLocationData = ObjectPoolManager.Instance.Get<LootLocationData>();
            lootLocationData.Initialize(Game, bounds.Center, sourceEntity);
            lootLocationTableProto.Roll(lootLocationData);

            if (lootLocationData.DropInPlace)
            {
                dropPosition = bounds.Center;
                return true;
            }

            float minRadius = MathF.Max(bounds.Radius, lootLocationData.MinRadius);

            // If minRadius is equal to maxRadius, ChooseRandomPositionNearPoint() sometimes fails, so we need to add some padding
            if (minRadius >= maxRadius)
                maxRadius = minRadius + 10f;

            return sourceEntity.Region.ChooseRandomPositionNearPoint(bounds, PathFlags.Walk, PositionCheckFlags.PreferNoEntity,
                BlockingCheckFlags.CheckSpawns, minRadius, maxRadius, out dropPosition);
        }
    }
}
