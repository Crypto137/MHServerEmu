namespace MHServerEmu.Games.GameData.Prototypes
{
    public class StaticRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public StaticAreaPrototype[] StaticAreas { get; private set; }
        public AreaConnectionPrototype[] Connections { get; private set; }
    }

    public class AreaConnectionPrototype : Prototype
    {
        public ulong AreaA { get; private set; }
        public ulong AreaB { get; private set; }
        public bool ConnectAllShared { get; private set; }
    }

    public class StaticAreaPrototype : Prototype
    {
        public ulong Area { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
    }
}
