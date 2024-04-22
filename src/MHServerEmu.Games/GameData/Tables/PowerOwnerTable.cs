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
        private readonly Dictionary<(PrototypeId, PrototypeId), (TalentEntryPrototype, uint)> _talentEntryDict = new();
        private readonly Dictionary<(PrototypeId, PrototypeId), (TalentGroupPrototype, uint)> _talentGroupDict = new();
        private readonly Dictionary<(PrototypeId, PrototypeId), TeamUpPowerProgressionEntryPrototype> _teamUpPowerProgressionEntryDict = new();

        public PowerOwnerTable()
        {
            // Get data from avatar prototypes
            foreach (PrototypeId avatarRef in DataDirectory.Instance.IteratePrototypesInHierarchy(typeof(AvatarPrototype),
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
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

                // Get data from talent groups
                uint talentGroupIndex = 1;

                foreach (TalentGroupPrototype talentGroupProto in avatarProto.TalentGroups)
                {
                    uint talentIndex = 0;

                    foreach (TalentEntryPrototype talentEntry in talentGroupProto.Talents)
                    {
                        _powerOwnerDict[talentEntry.Talent] = avatarProto.DataRef;

                        (PrototypeId, PrototypeId) avatarTalent = (avatarProto.DataRef, talentEntry.Talent);
                        
                        _talentEntryDict[avatarTalent] = (talentEntry, talentIndex++);
                        _talentGroupDict[avatarTalent] = (talentGroupProto, talentGroupIndex);
                    }

                    talentGroupIndex++;
                }
            }

            // Get data from team-up prototypes
            foreach (PrototypeId teamUpRef in DataDirectory.Instance.IteratePrototypesInHierarchy(typeof(AgentTeamUpPrototype),
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
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

        public TalentEntryPrototype GetTalentEntry(PrototypeId ownerRef, PrototypeId powerRef)
        {
            if (_talentEntryDict.TryGetValue((ownerRef, powerRef), out var entry) == false)
                return null;

            return entry.Item1;
        }

        public uint GetTalentGroupIndex(PrototypeId ownerRef, PrototypeId powerRef)
        {
            if (_talentGroupDict.TryGetValue((ownerRef, powerRef), out var group) == false)
                return 0;

            return group.Item2;
        }

        public (TalentEntryPrototype, uint) GetTalentEntryPair(PrototypeId ownerRef, PrototypeId powerRef)
        {
            if (_talentEntryDict.TryGetValue((ownerRef, powerRef), out var entry) == false)
                return default;

            return entry;
        }

        public (TalentGroupPrototype, uint) GetTalentGroupPair(PrototypeId ownerRef, PrototypeId powerRef)
        {
            if (_talentGroupDict.TryGetValue((ownerRef, powerRef), out var entry) == false)
                return default;

            return entry;
        }
    }
}
