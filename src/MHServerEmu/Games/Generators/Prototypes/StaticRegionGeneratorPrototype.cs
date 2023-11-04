using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class StaticRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public StaticAreaPrototype[] StaticAreas;
        public AreaConnectionPrototype[] Connections;
        public StaticRegionGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StaticRegionGeneratorPrototype), proto); }
    }

    public class AreaConnectionPrototype : Prototype
    {
        public ulong AreaA;
        public ulong AreaB;
        public bool ConnectAllShared;
        public AreaConnectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AreaConnectionPrototype), proto); }
    }

    public class StaticAreaPrototype : Prototype
    {
        public ulong Area;
        public int X;
        public int Y;
        public int Z;
        public StaticAreaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StaticAreaPrototype), proto); }
    }
}
