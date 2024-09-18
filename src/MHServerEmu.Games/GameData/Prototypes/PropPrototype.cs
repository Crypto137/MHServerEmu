using MHServerEmu.Core.Extensions;

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
        public int GetPropDensity(PrototypeId marker)
        {
            if (marker == PrototypeId.Invalid) return 0;
            if (MarkerDensityOverrides.HasValue())
                foreach (var densityEntry in MarkerDensityOverrides)
                    if (densityEntry != null && densityEntry.Marker == marker)
                        return densityEntry.OverrideDensity;

            return DefaultDensity;
        }
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
