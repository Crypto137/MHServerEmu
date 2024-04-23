using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class AllianceTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly bool[][] _friendlyLookup;
        private readonly bool[][] _hostileLookup;

        public AllianceTable()
        {
            DataDirectory dataDirectory = GameDatabase.DataDirectory;

            // Get the alliance blueprint and figure out the total number of alliances
            GlobalsPrototype globals = GameDatabase.GlobalsPrototype;
            BlueprintId allianceBlueprintRef = dataDirectory.GetPrototypeBlueprintDataRef(globals.AnyAlliancePrototype);
            int numAlliances = dataDirectory.GetPrototypeMaxEnumValue(allianceBlueprintRef) + 1;

            // Allocate our table for every alliance combination using jagged arrays, the default value for both friendliness and hostility is false.
            // NOTE: The client uses nested vectors here, which we don't have to do, since we don't use any hotloading.
            _friendlyLookup = new bool[numAlliances][];
            _hostileLookup = new bool[numAlliances][];

            for (int i = 0; i < numAlliances; i++)
            {
                _friendlyLookup[i] = new bool[numAlliances];
                _hostileLookup[i] = new bool[numAlliances];
            }


            // Fill our table with data from alliance prototypes
            foreach (PrototypeId alliancePrototypeRef in dataDirectory.IteratePrototypesInHierarchy<AlliancePrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var alliancePrototype = alliancePrototypeRef.As<AlliancePrototype>();
                if (alliancePrototype == null)
                {
                    Logger.Warn("AllianceTable(): alliancePrototype == null");
                    continue;
                }

                // Alliances are automatically friendly to themselves
                _friendlyLookup[alliancePrototype.EnumValue][alliancePrototype.EnumValue] = true;

                // Add all friendliness flags from the prototype
                if (alliancePrototype.FriendlyTo != null)
                {
                    foreach (PrototypeId friendlyAllianceRef in alliancePrototype.FriendlyTo)
                    {
                        int friendlyEnumValue = dataDirectory.GetPrototypeEnumValue(friendlyAllianceRef, allianceBlueprintRef);
                        _friendlyLookup[alliancePrototype.EnumValue][friendlyEnumValue] = true;
                    }
                }

                // Add all hostility flags from the prototype
                if (alliancePrototype.HostileTo != null)
                {
                    foreach (PrototypeId hostileAllianceRef in alliancePrototype.HostileTo)
                    {
                        int hostileEnumValue = dataDirectory.GetPrototypeEnumValue(hostileAllianceRef, allianceBlueprintRef);
                        _hostileLookup[alliancePrototype.EnumValue][hostileEnumValue] = true;
                    }
                }
            }
        }

        public bool IsFriendlyTo(AlliancePrototype lhsAllianceProto, AlliancePrototype rhsAllianceProto)
        {
            if (lhsAllianceProto.EnumValue >= _friendlyLookup.Length || rhsAllianceProto.EnumValue >= _friendlyLookup.Length)
                return Logger.WarnReturn(false, "IsFriendlyTo(): Alliance prototype enum value out of range");

            return _friendlyLookup[lhsAllianceProto.EnumValue][rhsAllianceProto.EnumValue];
        }

        public bool IsHostileTo(AlliancePrototype lhsAllianceProto, AlliancePrototype rhsAllianceProto)
        {
            if (lhsAllianceProto.EnumValue >= _hostileLookup.Length || rhsAllianceProto.EnumValue >= _hostileLookup.Length)
                return Logger.WarnReturn(false, "IsHostileTo(): Alliance prototype enum value out of range");

            return _hostileLookup[lhsAllianceProto.EnumValue][rhsAllianceProto.EnumValue];
        }
    }
}
