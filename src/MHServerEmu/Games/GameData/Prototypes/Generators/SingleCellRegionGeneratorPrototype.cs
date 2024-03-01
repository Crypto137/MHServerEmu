namespace MHServerEmu.Games.GameData.Prototypes
{

    public class SingleCellRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public PrototypeId AreaInterface { get; protected set; }
        public AssetId Cell { get; protected set; }

        public PrototypeId CellProto;
    }
}
