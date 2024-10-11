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

        public Game Game { get; }

        /// <summary>
        /// Constructs a new <see cref="LootManager"/> for the provided <see cref="Games.Game"/>.
        /// </summary>
        public LootManager(Game game)
        {
            Game = game;
            _resolver = new(game.Random);
        }

        /// <summary>
        /// Drops random loot from the provided source <see cref="WorldEntity"/>.
        /// </summary>
        public void DropRandomLoot(Player player, WorldEntity sourceEntity)
        {
            LootDropEventType lootDropEventType = LootDropEventType.OnKilled;

            RankPrototype rankProto = sourceEntity.GetRankPrototype();
            if (rankProto.LootTableParam != LootDropEventType.None)
                lootDropEventType = rankProto.LootTableParam;

            PrototypeId lootTableProtoRef = sourceEntity.Properties[PropertyEnum.LootTablePrototype, (PropertyParam)lootDropEventType, 0, (PropertyParam)LootActionType.Spawn];
            if (lootTableProtoRef == PrototypeId.Invalid) return;

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            RollLootTable(lootTableProtoRef, player, lootResultSummary);

            if (lootResultSummary.HasAnyResult == false) return;

            SpawnLootFromSummary(lootResultSummary, player, sourceEntity);
        }

        /// <summary>
        /// Does a test roll of the specified loot table for the provided <see cref="Player"/>.
        /// </summary>
        public void TestLootTable(PrototypeId lootTableProtoRef, Player player)
        {
            Logger.Info($"--- Loot Table Test - {lootTableProtoRef.GetName()} ---");

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            if (RollLootTable(lootTableProtoRef, player, lootResultSummary) == false)
                Logger.Warn($"TestLootTable(): Failed to roll loot table {lootTableProtoRef.GetName()}");

            Logger.Info($"Types: {lootResultSummary.Types}");

            foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                Logger.Info($"itemProtoRef={itemSpec.ItemProtoRef.GetName()}, rarity={GameDatabase.GetFormattedPrototypeName(itemSpec.RarityProtoRef)}");

            Logger.Info("--- Loot Table Test Over ---");
        }
        
        /// <summary>
        /// Spawns loot contained in the provided <see cref="LootResultSummary"/> in the game world.
        /// </summary>
        public void SpawnLootFromSummary(LootResultSummary lootResultSummary, Player player, WorldEntity sourceEntity)
        {
            // Calculate drop radius
            int numDrops = lootResultSummary.ItemSpecs.Count + lootResultSummary.AgentSpecs.Count;
            float maxDropRadius = MathF.Min(300f, 75f + 25f * numDrops);

            // Instance the loot if we have a player provided and instanced loot is not disabled by server config
            ulong restrictedToPlayerGuid = player != null && Game.CustomGameOptions.DisableInstancedLoot == false ? player.DatabaseUniqueId : 0;

            if (lootResultSummary.Types != LootType.None && lootResultSummary.Types != LootType.Item)
                Logger.Debug($"SpawnLootFromSummary(): Types={lootResultSummary.Types}");

            // Spawn items
            if (lootResultSummary.Types.HasFlag(LootType.Item))
            {
                foreach (ItemSpec itemSpec in lootResultSummary.ItemSpecs)
                    SpawnItem(itemSpec, sourceEntity, maxDropRadius, restrictedToPlayerGuid);
            }

            // Spawn agents (orbs)
            if (lootResultSummary.Types.HasFlag(LootType.Agent))
            {
                foreach (AgentSpec agentSpec in lootResultSummary.AgentSpecs)
                    SpawnAgent(agentSpec, sourceEntity, maxDropRadius, restrictedToPlayerGuid);
            }
        }

        public bool SpawnItem(PrototypeId itemProtoRef, Player player, WorldEntity sourceEntity)
        {
            ItemSpec itemSpec = CreateItemSpec(itemProtoRef);
            if (itemSpec == null)
                return Logger.WarnReturn(false, $"SpawnItem(): Failed to create an ItemSpec for {itemProtoRef.GetName()}");

            using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            LootResult lootResult = new(itemSpec);
            lootResultSummary.Add(lootResult);

            SpawnLootFromSummary(lootResultSummary, player, sourceEntity);
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
        private bool RollLootTable(PrototypeId lootTableProtoRef, Player player, LootResultSummary lootResultSummary)
        {
            LootTablePrototype lootTableProto = lootTableProtoRef.As<LootTablePrototype>();
            if (lootTableProto == null) return Logger.WarnReturn(false, "RollLootTable(): lootTableProto == null");

            using LootRollSettings settings = ObjectPoolManager.Instance.Get<LootRollSettings>();
            settings.UsableAvatar = player.CurrentAvatar.AvatarPrototype;
            settings.UsablePercent = GameDatabase.LootGlobalsPrototype.LootUsableByRecipientPercent;
            settings.Level = player.CurrentAvatar.CharacterLevel;
            settings.LevelForRequirementCheck = player.CurrentAvatar.CharacterLevel;
            settings.DifficultyTier = player.GetRegion().DifficultyTierRef;

            _resolver.SetContext(LootContext.Drop, player);

            LootRollResult result = lootTableProto.RollLootTable(settings, _resolver);
            if (result.HasFlag(LootRollResult.Success))
                _resolver.FillLootResultSummary(lootResultSummary);

            return true;
        }

        /// <summary>
        /// Spawns an <see cref="Item"/> in the game world.
        /// </summary>
        private bool SpawnItem(ItemSpec itemSpec, WorldEntity sourceEntity, float dropRadius, ulong restrictedToPlayerGuid = 0)
        {
            // Pick a random point near source entity
            if (ChooseDropPosition(sourceEntity, dropRadius, out Vector3 dropPosition) == false)
                return Logger.WarnReturn(false, $"SpawnItem(): Failed to find position to spawn item {itemSpec.ItemProtoRef.GetName()}");

            // Get item prototype to calculate lifespan
            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();

            // Create entity
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = itemSpec.ItemProtoRef;
            settings.RegionId = sourceEntity.RegionLocation.RegionId;
            settings.Position = dropPosition;
            settings.SourceEntityId = sourceEntity.Id;
            settings.SourcePosition = sourceEntity.RegionLocation.Position;
            settings.ItemSpec = itemSpec;
            settings.Lifespan = itemProto.GetExpirationTime(itemSpec.RarityProtoRef);

            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settings.Properties = properties;
            settings.Properties[PropertyEnum.RestrictedToPlayerGuid] = restrictedToPlayerGuid;

            Item item = Game.EntityManager.CreateEntity(settings) as Item;
            if (item == null) return Logger.WarnReturn(false, "SpawnItem(): item == null");

            return true;
        }

        private bool SpawnAgent(in AgentSpec agentSpec, WorldEntity sourceEntity, float maxDropRadius, ulong restrictedToPlayerGuid = 0)
        {
            // this looks very similar to SpawnItem, TODO: move common functionality to a separate method

            // Pick a random point near source entity
            if (ChooseDropPosition(sourceEntity, maxDropRadius, out Vector3 dropPosition) == false)
                return Logger.WarnReturn(false, $"SpawnAgent(): Failed to find position to spawn agent {agentSpec}");

            // NOTE: Orbs shrink over time using their behavior profile, see CAgent::onEnterWorldScheduleOrbShrink for details.
            // Until we have their AI implemented, calculated lifespan here.
            TimeSpan lifespan = TimeSpan.Zero;

            AgentPrototype agentProto = agentSpec.AgentProtoRef.As<AgentPrototype>();
            if (agentProto.BehaviorProfile != null)
            {
                ProceduralProfileOrbPrototype orbProto = agentProto.BehaviorProfile.Brain.As<ProceduralProfileOrbPrototype>();
                if (orbProto != null)
                    lifespan = TimeSpan.FromMilliseconds(orbProto.ShrinkageDelayMS + orbProto.ShrinkageDurationMS);
            }

            // Create entity
            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = agentSpec.AgentProtoRef;
            settings.RegionId = sourceEntity.RegionLocation.RegionId;
            settings.Position = dropPosition;
            settings.SourceEntityId = sourceEntity.Id;
            settings.SourcePosition = sourceEntity.RegionLocation.Position;

            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settings.Properties = properties;
            settings.Properties[PropertyEnum.RestrictedToPlayerGuid] = restrictedToPlayerGuid;
            settings.Properties[PropertyEnum.CharacterLevel] = agentSpec.AgentLevel;
            settings.Properties[PropertyEnum.CombatLevel] = agentSpec.AgentLevel;

            settings.Lifespan = lifespan;

            Agent agent = Game.EntityManager.CreateEntity(settings) as Agent;
            if (agent == null) return Logger.WarnReturn(false, "SpawnAgent(): item == null");

            return true;
        }

        private static bool ChooseDropPosition(WorldEntity sourceEntity, float maxDropRadius, out Vector3 dropPosition)
        {
            if (sourceEntity == null)
            {
                dropPosition = Vector3.Zero;
                return Logger.WarnReturn(false, "ChooseDropPosition(): sourceEntity == null");
            }

            const float MinDropRadius = 50f;
            maxDropRadius = MathF.Max(MinDropRadius, maxDropRadius);

            return sourceEntity.Region.ChooseRandomPositionNearPoint(sourceEntity.Bounds, PathFlags.Walk, PositionCheckFlags.PreferNoEntity,
                BlockingCheckFlags.CheckSpawns, MinDropRadius, maxDropRadius, out dropPosition);
        }
    }
}
