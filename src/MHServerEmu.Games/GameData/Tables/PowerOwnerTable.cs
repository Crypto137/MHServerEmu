using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class PowerOwnerTable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PrototypeId, PrototypeId> _powerOwnerDict = new();
        private readonly Dictionary<(PrototypeId, PrototypeId), PrototypeId> _powerProgressionTabDict = new();
        private readonly Dictionary<(PrototypeId, PrototypeId), PowerProgressionEntryPrototype> _powerProgressionEntryDict = new();
        private readonly Dictionary<(PrototypeId, PrototypeId), TeamUpPowerProgressionEntryPrototype> _teamUpPowerProgressionEntryDict = new();

        public PowerOwnerTable()
        {
            // Get data from avatar prototypes
            foreach (PrototypeId avatarRef in DataDirectory.Instance.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                AvatarPrototype avatarProto = avatarRef.As<AvatarPrototype>();

                // Get data from power progression tables
                foreach (PowerProgressionTablePrototype powerProgTableProto in avatarProto.PowerProgressionTables)
                {
                    foreach (PowerProgressionEntryPrototype powerProgEntry in powerProgTableProto.PowerProgressionEntries)
                    {
                        AbilityAssignmentPrototype abilityAssignmentProto = powerProgEntry.PowerAssignment;
                            
                        _powerOwnerDict[abilityAssignmentProto.Ability] = avatarProto.DataRef;
                        _powerProgressionTabDict[(avatarProto.DataRef, abilityAssignmentProto.Ability)] = powerProgTableProto.PowerProgTableTabRef;
                        _powerProgressionEntryDict[(avatarProto.DataRef, abilityAssignmentProto.Ability)] = powerProgEntry;
                    }
                }
            }

            // Get data from team-up prototypes
            foreach (PrototypeId teamUpRef in DataDirectory.Instance.IteratePrototypesInHierarchy<AgentTeamUpPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                AgentTeamUpPrototype teamUpProto = teamUpRef.As<AgentTeamUpPrototype>();

                foreach (TeamUpPowerProgressionEntryPrototype powerProgEntryProto in teamUpProto.PowerProgression)
                {
                    _powerOwnerDict[powerProgEntryProto.Power] = teamUpProto.DataRef;
                    _teamUpPowerProgressionEntryDict[(teamUpProto.DataRef, powerProgEntryProto.Power)] = powerProgEntryProto;
                }
            }
        }

        public PrototypeId GetPowerProgressionOwnerForPower(PrototypeId powerRef)
        {
            if (powerRef == PrototypeId.Invalid)
                return Logger.WarnReturn(PrototypeId.Invalid, "GetPowerProgressionOwnerForPower(): powerRef == PrototypeId.Invalid");

            if (_powerOwnerDict.TryGetValue(powerRef, out PrototypeId powerOwner) == false)
                return PrototypeId.Invalid;

            return powerOwner;
        }

        public PrototypeId GetPowerProgressionTab(PrototypeId ownerRef, PrototypeId powerRef)
        {
            if (_powerProgressionTabDict.TryGetValue((ownerRef, powerRef), out PrototypeId tabRef) == false)
                return PrototypeId.Invalid;

            return tabRef;
        }

        public PowerProgressionEntryPrototype GetPowerProgressionEntry(PrototypeId ownerRef, PrototypeId powerRef)
        {
            if (_powerProgressionEntryDict.TryGetValue((ownerRef, powerRef), out var entry) == false)
                return null;

            return entry;
        }

        public TeamUpPowerProgressionEntryPrototype GetTeamUpPowerProgressionEntry(PrototypeId ownerRef, PrototypeId powerRef)
        {
            if (_teamUpPowerProgressionEntryDict.TryGetValue((ownerRef, powerRef), out var entry) == false)
                return null;

            return entry;
        }
    }
}
