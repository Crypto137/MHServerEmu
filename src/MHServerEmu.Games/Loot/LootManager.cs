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
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    public class LootManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Picker<Picker<PrototypeId>> _commonMetaPicker;
        private readonly Picker<Picker<PrototypeId>> _uncommonMetaPicker;
        private readonly Picker<Picker<PrototypeId>> _rareMetaPicker;

        public Game Game { get; }

        public LootManager(Game game)
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

            // Runeword glyphs
            Picker<PrototypeId> runewordGlyphPicker = new(Game.Random);
            _commonMetaPicker.Add(runewordGlyphPicker);

            foreach (PrototypeId runewordGlyphRef in dataDirectory.IteratePrototypesInHierarchy(HardcodedBlueprints.RunewordGlyphParent, PrototypeIterateFlags.NoAbstractApprovedOnly))
                runewordGlyphPicker.Add(runewordGlyphRef);

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
            Rank rank = source.GetRankPrototype().Rank;
            int lootRating = (int)rank + 1;

            float maxDistanceFromSource = 75f + 25f * lootRating;

            // Nodrop chance for popcorn mobs
            if (rank <= Rank.Popcorn && Game.Random.NextFloat() < 0.66f)
                return;

            // Instance the loot if we have a player provided and instanced loot is not disabled by server config
            ulong restrictedToPlayerGuid = player != null && Game.CustomGameOptions.DisableInstancedLoot == false ? player.DatabaseUniqueId : 0;

            // Drop some common items
            DropItem(source, _commonMetaPicker.Pick().Pick(), maxDistanceFromSource, restrictedToPlayerGuid);
            for (int i = 0; i < lootRating; i++)
            {
                if (Game.Random.NextFloat() < 0.20f)
                    DropItem(source, _commonMetaPicker.Pick().Pick(), maxDistanceFromSource, restrictedToPlayerGuid);
            }

            // Occasionally drop an uncommon item
            for (int i = 0; i < lootRating; i++)
            {
                if (Game.Random.NextFloat() < 0.20f)
                    DropItem(source, _uncommonMetaPicker.Pick().Pick(), maxDistanceFromSource, restrictedToPlayerGuid);
            }

            // Eternity splinter
            if (Game.Random.NextFloat() < 0.10f * lootRating)
                DropItem(source, (PrototypeId)11087194553833821680, maxDistanceFromSource, restrictedToPlayerGuid);

            if (rank == Rank.Boss || rank == Rank.GroupBoss)
            {
                // lootsplosion for bosses
                for (int i = 0; i < 10; i++)
                {
                    DropItem(source, _commonMetaPicker.Pick().Pick(), maxDistanceFromSource, restrictedToPlayerGuid);
                    DropItem(source, _uncommonMetaPicker.Pick().Pick(), maxDistanceFromSource, restrictedToPlayerGuid);
                }
            }

            // Rare 0.1% drops
            if (Game.Random.NextFloat() < 0.001f * lootRating)
                DropItem(source, _rareMetaPicker.Pick().Pick(), maxDistanceFromSource, restrictedToPlayerGuid);
        }
    }
}
