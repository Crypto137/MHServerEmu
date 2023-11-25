using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public enum DEPRECATEDDifficultyMode
    {
        Normal = 0,
        Heroic = 1,
        SuperHeroic = 2,
    }

    public class RegionDifficultySettingsPrototype : Prototype
    {
        public ulong TuningTable { get; set; }
    }

    public class NumNearbyPlayersDmgByRankPrototype : Prototype
    {
        public Rank Rank { get; set; }
        public ulong MobToPlayerCurve { get; set; }
        public ulong PlayerToMobCurve { get; set; }
    }

    public class DifficultyIndexDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; set; }
        public ulong MobToPlayerCurve { get; set; }
        public ulong PlayerToMobCurve { get; set; }
    }

    public class TuningDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; set; }
        public float TuningMobToPlayer { get; set; }
        public float TuningPlayerToMob { get; set; }
    }

    public class NegStatusRankCurveEntryPrototype : Prototype
    {
        public Rank Rank { get; set; }
        public ulong TenacityModifierCurve { get; set; }
    }

    public class NegStatusPropCurveEntryPrototype : Prototype
    {
        public ulong NegStatusProp { get; set; }
        public NegStatusRankCurveEntryPrototype[] RankEntries { get; set; }
    }

    public class RankAffixTableByDifficultyEntryPrototype : Prototype
    {
        public ulong DifficultyMin { get; set; }
        public ulong DifficultyMax { get; set; }
        public RankAffixEntryPrototype[] RankAffixTable { get; set; }
    }

    public class TuningPrototype : Prototype
    {
        public ulong Name { get; set; }
        public float PlayerInflictedDamageTimerSec { get; set; }
        public float PlayerNearbyRange { get; set; }
        public NegStatusPropCurveEntryPrototype[] NegativeStatusCurves { get; set; }
        public ulong LootFindByLevelDeltaCurve { get; set; }
        public ulong SpecialItemFindByLevelDeltaCurve { get; set; }
        public ulong LootFindByDifficultyIndexCurve { get; set; }
        public ulong PlayerXPByDifficultyIndexCurve { get; set; }
        public ulong DeathPenaltyCondition { get; set; }
        public ulong PctXPFromParty { get; set; }
        public ulong PctXPFromRaid { get; set; }
        public ulong Tier { get; set; }
        public bool UseTierMinimapColor { get; set; }
        public RankAffixEntryPrototype[] RankAffixTable { get; set; }
        public float PctXPMultiplier { get; set; }
        public bool NumNearbyPlayersScalingEnabled { get; set; }
        public float TuningDamageMobToPlayer { get; set; }
        public float TuningDamageMobToPlayerDCL { get; set; }
        public float TuningDamagePlayerToMob { get; set; }
        public float TuningDamagePlayerToMobDCL { get; set; }
        public TuningDamageByRankPrototype[] TuningDamageByRank { get; set; }
        public TuningDamageByRankPrototype[] TuningDamageByRankDCL { get; set; }
        public RankAffixTableByDifficultyEntryPrototype[] RankAffixTableByDifficulty { get; set; }
    }

    public class DifficultyModePrototype : Prototype
    {
        public ulong IconPath { get; set; }
        public ulong Name { get; set; }
        public DEPRECATEDDifficultyMode DifficultyModeEnum { get; set; }
        public int MigrationUnlocksAtLevel { get; set; }
        public ulong UnlockNotification { get; set; }
        public ulong TextStyle { get; set; }
    }

    public class DifficultyTierPrototype : Prototype
    {
        public int DEPTier { get; set; }
        public DifficultyTier Tier { get; set; }
        public float BonusExperiencePct { get; set; }
        public float DamageMobToPlayerPct { get; set; }
        public float DamagePlayerToMobPct { get; set; }
        public float ItemFindCreditsPct { get; set; }
        public float ItemFindRarePct { get; set; }
        public float ItemFindSpecialPct { get; set; }
        public int UnlockLevel { get; set; }
        public ulong UIColor { get; set; }
        public ulong UIDisplayName { get; set; }
        public int BonusItemFindBonusDifficultyMult { get; set; }
    }

    public class DifficultyGlobalsPrototype : Prototype
    {
        public ulong MobConLevelCurve { get; set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefault { get; set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefaultPCZ { get; set; }
        public ulong NumNearbyPlayersDmgDefaultMtoP { get; set; }
        public ulong NumNearbyPlayersDmgDefaultPtoM { get; set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRank { get; set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRankPCZ { get; set; }
        public ulong DifficultyIndexDamageDefaultMtoP { get; set; }
        public ulong DifficultyIndexDamageDefaultPtoM { get; set; }
        public DifficultyIndexDamageByRankPrototype[] DifficultyIndexDamageByRank { get; set; }
        public float PvPDamageMultiplier { get; set; }
        public float PvPCritDamageMultiplier { get; set; }
        public ulong PvPDamageScalarFromLevelCurve { get; set; }
        public ulong TeamUpDamageScalarFromLevelCurve { get; set; }
        public EvalPrototype EvalDamageLevelDeltaMtoP { get; set; }
        public EvalPrototype EvalDamageLevelDeltaPtoM { get; set; }
    }
}
