using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropPrototype : WorldEntityPrototype
    {
        public bool PreventsSpawnCleanup;
        public PropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropPrototype), proto); }
    }

    public class PropDensityEntryPrototype : Prototype
    {
        public ulong Marker;
        public int OverrideDensity;
        public PropDensityEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropDensityEntryPrototype), proto); }
    }

    public class PropDensityPrototype : Prototype
    {
        public PropDensityEntryPrototype[] MarkerDensityOverrides;
        public int DefaultDensity;
        public PropDensityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropDensityPrototype), proto); }

        public int GetPropDensity(ulong marker)
        {
            if (marker == 0) return 0;
            if (MarkerDensityOverrides.IsNullOrEmpty() == false)
            {
                foreach (var densityEntry in MarkerDensityOverrides)
                    if (densityEntry != null && densityEntry.Marker == marker)
                        return densityEntry.OverrideDensity;
            }
            return DefaultDensity;
        }

    }

    public class SmartPropPrototype : AgentPrototype
    {
        public SmartPropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SmartPropPrototype), proto); }
    }

    public class DestructiblePropPrototype : PropPrototype
    {
        public DestructiblePropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DestructiblePropPrototype), proto); }
    }

    public class DestructibleSmartPropPrototype : SmartPropPrototype
    {
        public DestructibleSmartPropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DestructibleSmartPropPrototype), proto); }
    }
}
