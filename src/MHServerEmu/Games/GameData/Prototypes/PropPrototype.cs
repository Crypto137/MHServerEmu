namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropPrototype : WorldEntityPrototype
    {
        public bool PreventsSpawnCleanup { get; protected set; }
    }

    public class PropDensityEntryPrototype : Prototype
    {
        public PrototypeId Marker { get; protected set; }
        public int OverrideDensity { get; protected set; }
    }

    public class PropDensityPrototype : Prototype
    {
        public PropDensityEntryPrototype[] MarkerDensityOverrides { get; protected set; }
        public int DefaultDensity { get; protected set; }
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
