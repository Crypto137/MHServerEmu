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

        private readonly Picker<Picker<PrototypeId>> _commonMetaPicker;
        private readonly Picker<Picker<PrototypeId>> _uncommonMetaPicker;
        private readonly Picker<Picker<PrototypeId>> _rareMetaPicker;

        public Game Game { get; }

        public LootGenerator(Game game)
        {
            Game = game;

            // NOTE: This is a first, highly inaccurate iteration of loot just to have something dropping

            // Initialize pickers

            // Picker of pickers (meta pickers)
            _commonMetaPicker = new(Game.Random);
            _uncommonMetaPicker = new(Game.Random);
            _rareMetaPicker = new(Game.Random);

            // Populate pickers by type
            DataDirectory dataDirectory = DataDirectory.Instance;

            // Common drops
            // Crafting elements
            Picker<PrototypeId> craftingElementPicker = new(Game.Random);
            _commonMetaPicker.Add(craftingElementPicker);

            foreach (PrototypeId craftingElementRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.CraftingElement, PrototypeIterateFlags.NoAbstractApprovedOnly))
                craftingElementPicker.Add(craftingElementRef);

            // Relics
            Picker<PrototypeId> relicPicker = new(Game.Random);
            _commonMetaPicker.Add(relicPicker);

            foreach (PrototypeId relicProtoRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.Relic, PrototypeIterateFlags.NoAbstractApprovedOnly))
                relicPicker.Add(relicProtoRef);

            // Uncommon drops
            // Artifacts
            Picker<PrototypeId> artifactPicker = new(Game.Random);
            _uncommonMetaPicker.Add(artifactPicker);

            foreach (PrototypeId artifactProtoRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.Artifact, PrototypeIterateFlags.NoAbstractApprovedOnly))
                artifactPicker.Add(artifactProtoRef);

            // Rings
            Picker<PrototypeId> ringPicker = new(Game.Random);
            _uncommonMetaPicker.Add(ringPicker);

            foreach (PrototypeId ringProtoRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.RingBlueprint, PrototypeIterateFlags.NoAbstractApprovedOnly))
                ringPicker.Add(ringProtoRef);

            // Runeword glyphs
            Picker<PrototypeId> runewordGlyphPicker = new(Game.Random);
            _uncommonMetaPicker.Add(runewordGlyphPicker);

            foreach (PrototypeId runewordGlyphRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.RunewordGlyphParent, PrototypeIterateFlags.NoAbstractApprovedOnly))
                runewordGlyphPicker.Add(runewordGlyphRef);


            // Rare drops

            // Costumes
            Picker<PrototypeId> costumePicker = new(Game.Random);
            _rareMetaPicker.Add(costumePicker);

            foreach (PrototypeId petItemProtoRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.Costume, PrototypeIterateFlags.NoAbstractApprovedOnly))
                costumePicker.Add(petItemProtoRef);

            // Pets
            Picker<PrototypeId> petItemPicker = new(Game.Random);
            _rareMetaPicker.Add(petItemPicker);

            foreach (PrototypeId petItemProtoRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.PetItem, PrototypeIterateFlags.NoAbstractApprovedOnly))
                petItemPicker.Add(petItemProtoRef);
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
            int lootRating = source.GetRankPrototype().Rank switch
            {
                Rank.Popcorn    => 1,
                Rank.Champion   => 2,
                Rank.Elite      => 3,
                Rank.MiniBoss   => 4,
                Rank.Boss       => 5,
                _ => 0,
            };

            float maxDistanceFromSource = 25f + 25f * lootRating;

            // Drop a bunch of common items
            for (int i = 0; i < lootRating; i++)
                DropItem(source, _commonMetaPicker.Pick().Pick(), maxDistanceFromSource);

            // Occasionally drop an uncommon item
            for (int i = 0; i < lootRating; i++)
            {
                if (Game.Random.NextFloat() <= 0.33f)
                    DropItem(source, _uncommonMetaPicker.Pick().Pick(), maxDistanceFromSource);
            }

            // Eternity splinter
            if (Game.Random.NextFloat() <= 0.10f * lootRating)
                DropItem(source, (PrototypeId)11087194553833821680, maxDistanceFromSource);

            // Rare 1% drops
            if (Game.Random.NextFloat() <= 0.01f)
                DropItem(source, _rareMetaPicker.Pick().Pick(), maxDistanceFromSource);
        }
    }
}
