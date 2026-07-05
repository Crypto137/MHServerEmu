using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{

    public class SingleCellRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public PrototypeId AreaInterface { get; protected set; }
        public AssetId Cell { get; protected set; }

        //---

        [DoNotCopy]
        public PrototypeId CellProto { get; set; }  // Overrides the Cell asset specified in the field above
    }
}
