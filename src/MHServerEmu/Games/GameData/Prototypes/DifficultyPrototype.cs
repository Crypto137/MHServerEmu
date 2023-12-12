using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class RegionDifficultySettingsPrototype : Prototype
    {
        public ulong TuningTable;
        public RegionDifficultySettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionDifficultySettingsPrototype), proto); }
    }

    public class NumNearbyPlayersDmgByRankPrototype : Prototype
    {
        public Rank Rank;
        public ulong MobToPlayerCurve;
        public ulong PlayerToMobCurve;
        public NumNearbyPlayersDmgByRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(NumNearbyPlayersDmgByRankPrototype), proto); }
    }

    public class DifficultyIndexDamageByRankPrototype : Prototype
    {
        public Rank Rank;
        public ulong MobToPlayerCurve;
        public ulong PlayerToMobCurve;
        public DifficultyIndexDamageByRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DifficultyIndexDamageByRankPrototype), proto); }
    }

    public class TuningDamageByRankPrototype : Prototype
    {
        public Rank Rank;
        public float TuningMobToPlayer;
        public float TuningPlayerToMob;
        public TuningDamageByRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TuningDamageByRankPrototype), proto); }
    }

    public class NegStatusRankCurveEntryPrototype : Prototype
    {
        public Rank Rank;
        public ulong TenacityModifierCurve;
        public NegStatusRankCurveEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(NegStatusRankCurveEntryPrototype), proto); }
    }

    public class NegStatusPropCurveEntryPrototype : Prototype
    {
        public ulong NegStatusProp;
        public NegStatusRankCurveEntryPrototype[] RankEntries;
        public NegStatusPropCurveEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(NegStatusPropCurveEntryPrototype), proto); }
    }
    public class RankAffixTableByDifficultyEntryPrototype : Prototype
    {
        public ulong DifficultyMin;
        public ulong DifficultyMax;
        public RankAffixEntryPrototype[] RankAffixTable;
        public RankAffixTableByDifficultyEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RankAffixTableByDifficultyEntryPrototype), proto); }
    }

    public class TuningPrototype : Prototype
    {
        public ulong Name;
        public float PlayerInflictedDamageTimerSec;
        public float PlayerNearbyRange;
        public NegStatusPropCurveEntryPrototype[] NegativeStatusCurves;
        public ulong LootFindByLevelDeltaCurve;
        public ulong SpecialItemFindByLevelDeltaCurve;
        public ulong LootFindByDifficultyIndexCurve;
        public ulong PlayerXPByDifficultyIndexCurve;
        public ulong DeathPenaltyCondition;
        public ulong PctXPFromParty;
        public ulong PctXPFromRaid;
        public ulong Tier;
        public bool UseTierMinimapColor;
        public RankAffixEntryPrototype[] RankAffixTable;
        public float PctXPMultiplier;
        public bool NumNearbyPlayersScalingEnabled;
        public float TuningDamageMobToPlayer;
        public float TuningDamageMobToPlayerDCL;
        public float TuningDamagePlayerToMob;
        public float TuningDamagePlayerToMobDCL;
        public TuningDamageByRankPrototype[] TuningDamageByRank;
        public TuningDamageByRankPrototype[] TuningDamageByRankDCL;
        public RankAffixTableByDifficultyEntryPrototype[] RankAffixTableByDifficulty;
        public TuningPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TuningPrototype), proto); }
    }

    public class DifficultyModePrototype : Prototype
    {
        public ulong IconPath;
        public ulong Name;
        public DEPRECATEDDifficultyMode DifficultyModeEnum;
        public int MigrationUnlocksAtLevel;
        public ulong UnlockNotification;
        public ulong TextStyle;
        public DifficultyModePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DifficultyModePrototype), proto); }
    }

    public enum DEPRECATEDDifficultyMode
    {
        Normal = 0,
        Heroic = 1,
        SuperHeroic = 2,
    }

    public class DifficultyTierPrototype : Prototype
    {
        public int DEPTier;
        public DifficultyTier Tier;
        public float BonusExperiencePct;
        public float DamageMobToPlayerPct;
        public float DamagePlayerToMobPct;
        public float ItemFindCreditsPct;
        public float ItemFindRarePct;
        public float ItemFindSpecialPct;
        public int UnlockLevel;
        public ulong UIColor;
        public ulong UIDisplayName;
        public int BonusItemFindBonusDifficultyMult;
        public DifficultyTierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DifficultyTierPrototype), proto); }
    }

    public class DifficultyGlobalsPrototype : Prototype
    {
        public ulong MobConLevelCurve;
        public RegionDifficultySettingsPrototype RegionSettingsDefault;
        public RegionDifficultySettingsPrototype RegionSettingsDefaultPCZ;
        public ulong NumNearbyPlayersDmgDefaultMtoP;
        public ulong NumNearbyPlayersDmgDefaultPtoM;
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRank;
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRankPCZ;
        public ulong DifficultyIndexDamageDefaultMtoP;
        public ulong DifficultyIndexDamageDefaultPtoM;
        public DifficultyIndexDamageByRankPrototype[] DifficultyIndexDamageByRank;
        public float PvPDamageMultiplier;
        public float PvPCritDamageMultiplier;
        public ulong PvPDamageScalarFromLevelCurve;
        public ulong TeamUpDamageScalarFromLevelCurve;
        public EvalPrototype EvalDamageLevelDeltaMtoP;
        public EvalPrototype EvalDamageLevelDeltaPtoM;
        public DifficultyGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DifficultyGlobalsPrototype), proto); }
    }
}
