using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Powers
{
    public struct PowerProgressionInfo
    {
        // TODO?: Potentially make this struct readonly, constructors private and Init() functions static

        public const int RankLocked = -1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private ProgressionEntryPrototype _progressionEntryPrototype;

        public PowerPrototype PowerPrototype { get; private set; }
        public PrototypeId MappedPowerRef { get; private set; }
        public PrototypeId PowerTabRef { get; private set; }
        public uint TalentIndex { get; private set; }
        public uint TalentGroupIndex { get; private set; }

        public readonly PowerProgressionEntryPrototype PowerProgressionEntryPrototype { get => _progressionEntryPrototype as PowerProgressionEntryPrototype; }
        public readonly PrototypeId PowerRef { get => PowerPrototype != null ? PowerPrototype.DataRef : PrototypeId.Invalid; }
        public readonly bool IsValid { get => PowerPrototype != null; }

        public readonly bool IsForAvatar { get => _progressionEntryPrototype is PowerProgressionEntryPrototype; }
        public readonly bool IsForTeamUp { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype; }
        public readonly bool IsInPowerProgression { get => IsForAvatar || IsForTeamUp; }

        public readonly PrototypeId[] PrerequisitePowerRefs { get => _progressionEntryPrototype?.GetPrerequisites(); }
        public readonly PrototypeId[] AntirequisitePowerRefs { get => _progressionEntryPrototype?.GetAntirequisites(); }
        public readonly bool IsUltimatePower { get => PowerPrototype != null && Power.IsUltimatePower(PowerPrototype); }
        public readonly bool IsPassivePowerOnAvatarWhileAway { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype entry && entry.IsPassiveOnAvatarWhileAway; }
        public readonly bool IsPassivePowerOnAvatarWhileSummoned { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype entry && entry.IsPassiveOnAvatarWhileSummoned; }

        public PowerProgressionInfo() { }

        public bool InitNonProgressionPower(PrototypeId powerRef)
        {
            if (PowerPrototype != null) return Logger.WarnReturn(false, "InitNonProgressionPower(): PowerPrototype != null");

            PowerPrototype = powerRef.As<PowerPrototype>();

            return true;
        }

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

        public bool InitForTeamUp(TeamUpPowerProgressionEntryPrototype teamUpPowerProgressionEntryPrototype)
        {
            if (PowerPrototype != null) return Logger.WarnReturn(false, "InitForTeamUp(): PowerPrototype != null");
            
            _progressionEntryPrototype = teamUpPowerProgressionEntryPrototype;
            PowerPrototype = teamUpPowerProgressionEntryPrototype.Power.As<PowerPrototype>();

            return true;
        }

        public readonly int GetRequiredLevel()
        {
            if (_progressionEntryPrototype != null)
                return _progressionEntryPrototype.GetRequiredLevel();
            
            return 0;
        }

        public readonly int GetStartingRank()
        {
            if (_progressionEntryPrototype != null)
                return _progressionEntryPrototype.GetStartingRank();

            return 0;
        }

        public readonly Curve GetMaxRankCurve()
        {
            return _progressionEntryPrototype?.GetMaxRankForPowerAtCharacterLevel().AsCurve();
        }

        public readonly bool CanBeRankedUp()
        {
            Curve maxRankCurve = GetMaxRankCurve();
            if (maxRankCurve == null)
                return false;
            
            return maxRankCurve.GetIntAt(maxRankCurve.MaxPosition) > GetStartingRank();
        }
    }
}
