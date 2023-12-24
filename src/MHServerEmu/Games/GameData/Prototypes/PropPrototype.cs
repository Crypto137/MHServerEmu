namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropPrototype : WorldEntityPrototype
    {
        public bool PreventsSpawnCleanup { get; private set; }
    }

    public class PropDensityEntryPrototype : Prototype
    {
        public ulong Marker { get; private set; }
        public int OverrideDensity { get; private set; }
    }

    public class PropDensityPrototype : Prototype
    {
        public PropDensityEntryPrototype[] MarkerDensityOverrides { get; private set; }
        public int DefaultDensity { get; private set; }
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
