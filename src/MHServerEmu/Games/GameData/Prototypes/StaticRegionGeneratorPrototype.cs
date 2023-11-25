namespace MHServerEmu.Games.GameData.Prototypes
{
    public class StaticRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public StaticAreaPrototype[] StaticAreas { get; set; }
        public AreaConnectionPrototype[] Connections { get; set; }
    }

    public class AreaConnectionPrototype : Prototype
    {
        public ulong AreaA { get; set; }
        public ulong AreaB { get; set; }
        public bool ConnectAllShared { get; set; }
    }

    public class StaticAreaPrototype : Prototype
    {
        public ulong Area { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}
