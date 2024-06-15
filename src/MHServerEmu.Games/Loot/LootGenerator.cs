using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    public class LootGenerator
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Picker<PrototypeId> _artifactPicker;

        public Game Game { get; }

        public LootGenerator(Game game)
        {
            Game = game;
            _artifactPicker = new(Game.Random);

            // Add all artifacts to the pool
            foreach (PrototypeId artifactProtoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.Artifact, PrototypeIterateFlags.NoAbstractApprovedOnly))
                _artifactPicker.Add(artifactProtoRef);
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
        public Item DropItem(WorldEntity source, PrototypeId itemProtoRef, float maxDistanceFromSource)
        {
            if (GameDatabase.DataDirectory.PrototypeIsChildOfBlueprint(itemProtoRef, HardcodedBlueprints.Item) == false)
                return Logger.WarnReturn<Item>(null, $"DropItem(): Provided itemProtoRef {GameDatabase.GetPrototypeName(itemProtoRef)} is not an item");

            // Pick a random point near source entity
            source.Region.ChooseRandomPositionNearPoint(source.Bounds, PathFlags.Walk, PositionCheckFlags.CheckClearOfEntity,
                BlockingCheckFlags.None, 10f, maxDistanceFromSource, out Vector3 dropPosition);

            EntitySettings settings = new();
            settings.EntityRef = itemProtoRef;
            settings.RegionId = source.RegionLocation.RegionId;
            settings.Position = dropPosition;
            settings.SourceEntityId = source.Id;
            // TODO: settings.SourcePosition
            settings.OptionFlags |= EntitySettingsOptionFlags.IsNewOnServer;    // needed for drop animation
            settings.ItemSpec = CreateItemSpec(itemProtoRef);

            return Game.EntityManager.CreateEntity(settings) as Item;
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
        public void DropRandomLoot(WorldEntity source)
        {
            int numDrops = source.GetRankPrototype().Rank switch
            {
                Rank.Popcorn    => 1,
                Rank.Champion   => 2,
                Rank.Elite      => 3,
                Rank.MiniBoss   => 4,
                Rank.Boss       => 5,
                _ => 0,
            };

            float maxDistanceFromSource = 25f * numDrops + 25f;

            for (int i = 0; i < numDrops; i++)
                DropItem(source, _artifactPicker.Pick(), maxDistanceFromSource);
        }
    }
}
