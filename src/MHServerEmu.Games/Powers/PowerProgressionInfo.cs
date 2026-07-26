using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Powers
{
    public struct PowerProgressionInfo
    {
        public const int RankLocked = -1;

        private ProgressionEntryPrototype _progressionEntryPrototype;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private TalentEntryPrototype _talentEntryPrototype;
        private TalentGroupPrototype _talentGroupPrototype;
#endif

        public PowerPrototype PowerPrototype { get; private set; }
        public PrototypeId MappedPowerRef { get; private set; }
        public PrototypeId PowerTabRef { get; private set; }
        public uint TalentIndex { get; private set; }
        public uint TalentGroupIndex { get; private set; }

        public readonly PowerProgressionEntryPrototype PowerProgressionEntryPrototype { get => _progressionEntryPrototype as PowerProgressionEntryPrototype; }
        public readonly PrototypeId PowerRef { get => PowerPrototype != null ? PowerPrototype.DataRef : PrototypeId.Invalid; }
        public readonly bool IsValid { get => PowerPrototype != null; }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public readonly bool IsForAvatar { get => _progressionEntryPrototype is PowerProgressionEntryPrototype || _talentEntryPrototype != null; }
#else
        public readonly bool IsForAvatar { get => _progressionEntryPrototype is PowerProgressionEntryPrototype; }
#endif
        public readonly bool IsForTeamUp { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype; }
        public readonly bool IsInPowerProgression { get => IsForAvatar || IsForTeamUp; }

        public readonly PrototypeId[] PrerequisitePowerRefs { get => _progressionEntryPrototype?.GetPrerequisites(); }
        public readonly PrototypeId[] AntirequisitePowerRefs { get => _progressionEntryPrototype?.GetAntirequisites(); }
        public readonly bool IsUltimatePower { get => PowerPrototype != null && Power.IsUltimatePower(PowerPrototype); }
#if GAME_VERSION_1_53
        public readonly bool IsTrait { get => _progressionEntryPrototype is PowerProgressionEntryPrototype entry && entry.TraitCategory != TraitCategory.None; }
#elif GAME_VERSION_1_52
        public readonly bool IsTrait { get => _progressionEntryPrototype is PowerProgressionEntryPrototype entry && entry.IsTrait; }
#endif
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public readonly bool IsTalent { get => _talentEntryPrototype != null && _talentGroupPrototype != null; }
#endif
        public readonly bool IsPassivePowerOnAvatarWhileAway { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype entry && entry.IsPassiveOnAvatarWhileAway; }
        public readonly bool IsPassivePowerOnAvatarWhileSummoned { get => _progressionEntryPrototype is TeamUpPowerProgressionEntryPrototype entry && entry.IsPassiveOnAvatarWhileSummoned; }

        public PowerProgressionInfo() { }

        public void InitNonProgressionPower(PrototypeId powerRef)
        {
            if (!Verify.IsTrue(PowerPrototype == null)) return;

            PowerPrototype = powerRef.As<PowerPrototype>();
        }

        public void InitForAvatar(PowerProgressionEntryPrototype avatarPowerProgEntry, PrototypeId mappedPowerRef, PrototypeId powerTabRef)
        {
            if (!Verify.IsTrue(PowerPrototype == null)) return;

            _progressionEntryPrototype = avatarPowerProgEntry;
            MappedPowerRef = mappedPowerRef;
            PowerTabRef = powerTabRef;

            if (!Verify.IsNotNull(avatarPowerProgEntry.PowerAssignment)) return;
            PowerPrototype = avatarPowerProgEntry.PowerAssignment.Ability.As<PowerPrototype>();
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public void InitForAvatar(TalentEntryPrototype talentEntryPrototype, TalentGroupPrototype talentGroupPrototype, uint talentIndex, uint talentGroupIndex)
        {
            if (!Verify.IsTrue(PowerPrototype == null)) return;

            _talentEntryPrototype = talentEntryPrototype;
            _talentGroupPrototype = talentGroupPrototype;
            TalentIndex = talentIndex;
            TalentGroupIndex = talentGroupIndex;
            PowerPrototype = talentEntryPrototype.Talent.As<PowerPrototype>();
        }
#endif

        public void InitForTeamUp(TeamUpPowerProgressionEntryPrototype teamUpPowerProgressionEntryPrototype)
        {
            if (!Verify.IsTrue(PowerPrototype == null)) return;

            _progressionEntryPrototype = teamUpPowerProgressionEntryPrototype;
            PowerPrototype = teamUpPowerProgressionEntryPrototype.Power.As<PowerPrototype>();
        }

        public readonly int GetRequiredLevel()
        {
            if (_progressionEntryPrototype != null)
                return _progressionEntryPrototype.GetRequiredLevel();

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            if (_talentEntryPrototype != null)
                return _talentEntryPrototype.UnlockLevel;
#endif

            return 0;
        }

        public readonly int GetStartingRank()
        {
            if (_progressionEntryPrototype != null)
                return _progressionEntryPrototype.GetStartingRank();

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            if (_talentEntryPrototype != null)
                return 1;
#endif

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
