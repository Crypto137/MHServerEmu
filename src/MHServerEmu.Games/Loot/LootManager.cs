using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    public class LootManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; }

        public LootManager(Game game)
        {
            Game = game;
        }

        /// <summary>
        /// Creates an <see cref="ItemSpec"/> for the provided <see cref="PrototypeId"/>.
        /// </summary>
        public ItemSpec CreateItemSpec(PrototypeId itemProtoRef)
        {
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
        /// Creates and drops a new <see cref="Item"/> near the provided source <see cref="WorldEntity"/>. 
        /// </summary>
        public Item DropItem(WorldEntity source, PrototypeId itemProtoRef, float maxDistanceFromSource, ulong restrictedToPlayerGuid = 0)
        {
            if (GameDatabase.DataDirectory.PrototypeIsChildOfBlueprint(itemProtoRef, HardcodedBlueprints.Item) == false)
                return Logger.WarnReturn<Item>(null, $"DropItem(): Provided itemProtoRef {GameDatabase.GetPrototypeName(itemProtoRef)} is not an item");

            // Pick a random point near source entity
            source.Region.ChooseRandomPositionNearPoint(source.Bounds, PathFlags.Walk, PositionCheckFlags.PreferNoEntity,
                BlockingCheckFlags.CheckSpawns, 50f, maxDistanceFromSource, out Vector3 dropPosition);

            // Create entity
            EntitySettings settings = new();
            settings.EntityRef = itemProtoRef;
            settings.RegionId = source.RegionLocation.RegionId;
            settings.Position = dropPosition;
            settings.SourceEntityId = source.Id;
            settings.SourcePosition = source.RegionLocation.Position;
            settings.OptionFlags |= EntitySettingsOptionFlags.IsNewOnServer;    // needed for drop animation
            settings.ItemSpec = CreateItemSpec(itemProtoRef);

            if (restrictedToPlayerGuid != 0)
            {
                PropertyCollection properties = new();
                properties[PropertyEnum.RestrictedToPlayerGuid] = restrictedToPlayerGuid;
                settings.Properties = properties;
            }

            Item item = Game.EntityManager.CreateEntity(settings) as Item;
            if (item == null) return Logger.WarnReturn(item, "DropItem(): item == null");

            // Set lifespan
            TimeSpan expirationTime = item.GetExpirationTime();
            item.InitLifespan(expirationTime);

            return item;
        }

        /// <summary>
        /// Creates and gives a new item to the provided <see cref="Player"/>.
        /// </summary>
        public Item GiveItem(Player player, PrototypeId itemProtoRef)
        {
            if (GameDatabase.DataDirectory.PrototypeIsChildOfBlueprint(itemProtoRef, HardcodedBlueprints.Item) == false)
                return Logger.WarnReturn<Item>(null, $"GiveItem(): Provided itemProtoRef {GameDatabase.GetPrototypeName(itemProtoRef)} is not an item");

            Inventory inventory = player.GetInventory(InventoryConvenienceLabel.General);
            if (inventory == null) return Logger.WarnReturn<Item>(null, "GiveItem(): inventory == null");

            EntitySettings settings = new();
            settings.EntityRef = itemProtoRef;
            settings.InventoryLocation = new(player.Id, inventory.PrototypeDataRef);
            settings.ItemSpec = CreateItemSpec(itemProtoRef);

            return Game.EntityManager.CreateEntity(settings) as Item;
        }

        /// <summary>
        /// Drops random loot from the provided source <see cref="WorldEntity"/>.
        /// </summary>
        public void DropRandomLoot(WorldEntity source, Player player)
        {
            LootDropEventType lootDropEventType = LootDropEventType.OnKilled;

            RankPrototype rankProto = source.GetRankPrototype();
            if (rankProto.LootTableParam != LootDropEventType.None)
                lootDropEventType = rankProto.LootTableParam;

            PrototypeId lootTableProtoRef = source.Properties[PropertyEnum.LootTablePrototype, (PropertyParam)lootDropEventType, 0, (PropertyParam)LootActionType.Spawn];
            LootTablePrototype lootTableProto = lootTableProtoRef.As<LootTablePrototype>();
            if (lootTableProto == null) return;

            // Instance the loot if we have a player provided and instanced loot is not disabled by server config
            ulong restrictedToPlayerGuid = player != null && Game.CustomGameOptions.DisableInstancedLoot == false ? player.DatabaseUniqueId : 0;

            Logger.Trace($"DropRandomLoot(): Rolling loot table {lootTableProto}");

            LootRollSettings settings = new();
            settings.UsableAvatar = player.CurrentAvatar.AvatarPrototype;
            settings.LevelForRequirementCheck = player.CurrentAvatar.CharacterLevel;

            ItemResolver resolver = new(Game.Random, LootContext.Drop, player);

            lootTableProto.RollLootTable(settings, resolver);
            
            float maxDistanceFromSource = MathF.Min(75f + 25f * resolver.ProcessedItemCount, 300f);

            foreach (PrototypeId itemProtoRef in resolver.ProcessedItems)
                DropItem(source, itemProtoRef, maxDistanceFromSource, restrictedToPlayerGuid);
        }

        public void TestLootTable(PrototypeId lootTableProtoRef, Player player)
        {
            LootTablePrototype lootTableProto = lootTableProtoRef.As<LootTablePrototype>();
            if (lootTableProto == null) return;

            Logger.Info($"--- Loot Table Test - {lootTableProto} ---");

            LootRollSettings settings = new();
            settings.UsableAvatar = player.CurrentAvatar.AvatarPrototype;
            settings.LevelForRequirementCheck = player.CurrentAvatar.CharacterLevel;

            ItemResolver resolver = new(Game.Random, LootContext.Drop, player);

            lootTableProto.RollLootTable(settings, resolver);

            Logger.Info("--- Loot Table Test Over ---");
        }
    }
}
