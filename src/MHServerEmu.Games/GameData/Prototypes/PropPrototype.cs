using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

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

        //---

        public int GetPropDensity(PrototypeId marker)
        {
            if (marker == PrototypeId.Invalid)
                return 0;

            if (MarkerDensityOverrides.HasValue())
            {
                foreach (PropDensityEntryPrototype densityEntry in MarkerDensityOverrides)
                {
                    if (!Verify.IsNotNull(densityEntry))
                        continue;

                    if (densityEntry.Marker == marker)
                        return densityEntry.OverrideDensity;
                }
            }

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
