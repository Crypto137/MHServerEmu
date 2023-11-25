namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropPrototype : WorldEntityPrototype
    {
        public bool PreventsSpawnCleanup { get; set; }
    }

    public class PropDensityEntryPrototype : Prototype
    {
        public ulong Marker { get; set; }
        public int OverrideDensity { get; set; }
    }

    public class PropDensityPrototype : Prototype
    {
        public PropDensityEntryPrototype[] MarkerDensityOverrides { get; set; }
        public int DefaultDensity { get; set; }
    }

    public class SmartPropPrototype : AgentPrototype
    {
    }

    public class DestructiblePropPrototype : PropPrototype
    {
    }

    public class DestructibleSmartPropPrototype : SmartPropPrototype
    {
    }
}
