using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public enum ItemPrototypeId : ulong
    {
        Art067SuperHeroic = 1934310603366604595,    // doom1
        Art153 = 16667966083210025606,              // doom2
        Art209 = 2348277069438327432,               // kurse
        Art208 = 16219747376418921095,              // kingpin
        Art210 = 13870721159688493696,              // modok
        Art224 = 3596602080314202505,               // jugg2
        Art225 = 17322934653406550410,              // jugg1
        Art236 = 17447023190666713484,              // mandarin
        Art237 = 3724082061191682445,               // taskmaster
        Art321 = 12391768528730856067,              // sinister
        Art354 = 11017918513942632845,              // shocker
        Art356 = 17751953736382093963,              // hood
        Art357 = 73716217325623696,                 // doc oc
    }

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
        public Item DropItem(WorldEntity source, PrototypeId itemProtoRef)
        {
            if (GameDatabase.DataDirectory.PrototypeIsChildOfBlueprint(itemProtoRef, HardcodedBlueprints.Item) == false)
                return Logger.WarnReturn<Item>(null, $"DropItem(): Provided itemProtoRef {GameDatabase.GetPrototypeName(itemProtoRef)} is not an item");

            EntitySettings settings = new();
            settings.EntityRef = itemProtoRef;
            settings.RegionId = source.RegionLocation.RegionId;
            settings.Position = new(source.RegionLocation.Position);
            settings.Orientation = new(source.RegionLocation.Orientation);
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

        public void DropRandomLoot(WorldEntity source)
        {
            // Drop a random artifact
            DropItem(source, _artifactPicker.Pick());
        }
    }
}
