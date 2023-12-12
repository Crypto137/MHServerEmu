namespace MHServerEmu.Games.GameData.Prototypes
{

    public class SingleCellRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public ulong AreaInterface;
        public ulong Cell;

        public ulong CellProto;

        public SingleCellRegionGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SingleCellRegionGeneratorPrototype), proto); }
    }
}
