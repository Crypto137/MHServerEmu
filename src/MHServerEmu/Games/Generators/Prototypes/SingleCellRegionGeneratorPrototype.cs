using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class SingleCellRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public ulong AreaInterface;
        public ulong Cell;
        public SingleCellRegionGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SingleCellRegionGeneratorPrototype), proto); }
    }
}
