using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Powers
{
    public class PowerProgressionInfo
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ProgressionEntryPrototype _progressionEntryPrototype;
        private TalentEntryPrototype _talentEntryPrototype;
        private TalentGroupPrototype _talentGroupPrototype;

        // Auto properties
        public PowerPrototype PowerPrototype { get; private set; }
        public PrototypeId MappedPowerRef { get; private set; }
        public PrototypeId PowerTabRef { get; private set; }
        public uint TalentIndex { get; private set; }
        public uint TalentGroupIndex { get; private set; }

        // Accessor properties
        public PowerProgressionEntryPrototype PowerProgressionEntryPrototype { get => _progressionEntryPrototype as PowerProgressionEntryPrototype; }
        public PrototypeId PowerRef { get => PowerPrototype != null ? PowerPrototype.DataRef : PrototypeId.Invalid; }
        public bool IsValid { get => PowerPrototype != null; }

        public bool IsForAvatar { get => _progressionEntryPrototype is PowerProgressionEntryPrototype || _talentEntryPrototype != null; }
        public bool IsForTeamUp { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype; }
        public bool IsInPowerProgression { get => IsForAvatar || IsForTeamUp; }
        
        public int RequiredLevel
        {
            get
            {
                if (_progressionEntryPrototype != null) return _progressionEntryPrototype.GetRequiredLevel();
                if (_talentEntryPrototype != null) return _talentEntryPrototype.UnlockLevel;
                return 0;
            }
        }
        
        public int StartingRank
        {
            get
            {
                if (_progressionEntryPrototype != null) return _progressionEntryPrototype.GetStartingRank();
                if (_talentEntryPrototype != null) return 1;
                return 0;
            }
        }

        public Curve MaxRankCurve { get => _progressionEntryPrototype?.GetMaxRankForPowerAtCharacterLevel().AsCurve(); }

        public bool CanBeRankedUp
        {
            get
            {
                Curve maxRankCurve = MaxRankCurve;
                if (maxRankCurve == null) return false;
                return maxRankCurve.GetIntAt(maxRankCurve.MaxPosition) > StartingRank;
            }
        }

        public IEnumerable<PrototypeId> PrerequisitePowerRefs { get => _progressionEntryPrototype?.GetPrerequisites(); }
        public IEnumerable<PrototypeId> AntirequisitePowerRefs { get => _progressionEntryPrototype?.GetAntirequisites(); }
        public bool IsUltimatePower { get => PowerPrototype != null && Power.IsUltimatePower(PowerPrototype); }
        public bool IsTrait { get => _progressionEntryPrototype is PowerProgressionEntryPrototype entry && entry.IsTrait; }
        public bool IsTalent { get => _talentEntryPrototype != null && _talentGroupPrototype != null; }
        public bool IsPassivePowerOnAvatarWhileAway { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype entry && entry.IsPassiveOnAvatarWhileAway; }

        public bool InitForAvatar(PowerProgressionEntryPrototype powerProgressionEntryPrototype, PrototypeId mappedPowerRef, PrototypeId powerTabRef)
        {
            if (PowerPrototype != null) return Logger.WarnReturn(false, "InitForAvatar(): PowerPrototype != null");

            _progressionEntryPrototype = powerProgressionEntryPrototype;
            MappedPowerRef = mappedPowerRef;
            PowerTabRef = powerTabRef;

            if (powerProgressionEntryPrototype.PowerAssignment == null)
                return Logger.WarnReturn(false, "InitForAvatar(): powerProgressionEntryPrototype.PowerAssignment == null");

            PowerPrototype = powerProgressionEntryPrototype.PowerAssignment.Ability.As<PowerPrototype>();

            return true;
        }

        public bool InitForAvatar(TalentEntryPrototype talentEntryPrototype, TalentGroupPrototype talentGroupPrototype, uint talentIndex, uint talentGroupIndex)
        {
            if (PowerPrototype != null) return Logger.WarnReturn(false, "InitForAvatar(): PowerPrototype != null");

            _talentEntryPrototype = talentEntryPrototype;
            _talentGroupPrototype = talentGroupPrototype;
            TalentIndex = talentIndex;
            TalentGroupIndex = talentGroupIndex;
            PowerPrototype = talentEntryPrototype.Talent.As<PowerPrototype>();
            
            return true;
        }

        public bool InitForTeamUp(TeamUpPowerProgressionEntryPrototype teamUpPowerProgressionEntryPrototype)
        {
            if (PowerPrototype != null) return Logger.WarnReturn(false, "InitForTeamUp(): PowerPrototype != null");
            
            _progressionEntryPrototype = teamUpPowerProgressionEntryPrototype;
            PowerPrototype = teamUpPowerProgressionEntryPrototype.Power.As<PowerPrototype>();

            return true;
        }

        public bool InitNonProgressionPower(PrototypeId powerRef)
        {
            if (PowerPrototype != null) return Logger.WarnReturn(false, "InitNonProgressionPower(): PowerPrototype != null");
            
            PowerPrototype = powerRef.As<PowerPrototype>();
            
            return true;
        }
    }
}
