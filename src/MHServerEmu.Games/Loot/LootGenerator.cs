using MHServerEmu.Core.Collections;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;

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
        private readonly Picker<PrototypeId> _itemPicker;

        public Game Game { get; }

        public LootGenerator(Game game)
        {
            Game = game;
            _itemPicker = new(Game.Random);

            // Add all artifacts to the pool
            foreach (PrototypeId artifactProtoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy((BlueprintId)1626168533479592044, PrototypeIterateFlags.NoAbstractApprovedOnly))
                _itemPicker.Add(artifactProtoRef);
        }

        public Item CreateItem(WorldEntity source, PrototypeId itemProtoRef)
        {
            EntitySettings settings = new();
            settings.EntityRef = itemProtoRef;
            settings.RegionId = source.RegionLocation.RegionId;
            settings.Position = new(source.RegionLocation.Position);
            settings.Orientation = new(source.RegionLocation.Orientation);
            settings.SourceEntityId = source.Id;
            // TODO: settings.SourcePosition
            settings.OptionFlags |= EntitySettingsOptionFlags.IsNewOnServer;    // needed for drop animation
            settings.ItemSpec = new(itemProtoRef, (PrototypeId)10195041726035595077, 1, 0, Array.Empty<AffixSpec>(), 1, PrototypeId.Invalid);

            return Game.EntityManager.CreateEntity(settings) as Item;
        }

        public Item CreateItem(WorldEntity source, ItemPrototypeId itemProtoRef) => CreateItem(source, (PrototypeId)itemProtoRef);

        public Item CreateRandomItem(WorldEntity source) => CreateItem(source, _itemPicker.Pick());
    }
}
