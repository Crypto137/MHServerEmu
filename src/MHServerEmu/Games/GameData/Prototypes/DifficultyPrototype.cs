using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum DEPRECATEDDifficultyMode
    {
        Normal = 0,
        Heroic = 1,
        SuperHeroic = 2,
    }

    #endregion

    public class RegionDifficultySettingsPrototype : Prototype
    {
        public ulong TuningTable { get; protected set; }
    }

    public class NumNearbyPlayersDmgByRankPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public ulong MobToPlayerCurve { get; protected set; }
        public ulong PlayerToMobCurve { get; protected set; }
    }

    public class DifficultyIndexDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public ulong MobToPlayerCurve { get; protected set; }
        public ulong PlayerToMobCurve { get; protected set; }
    }

    public class TuningDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public float TuningMobToPlayer { get; protected set; }
        public float TuningPlayerToMob { get; protected set; }
    }

    public class NegStatusRankCurveEntryPrototype : Prototype
    {
        public Rank Rank { get; protected set; }
        public ulong TenacityModifierCurve { get; protected set; }
    }

    public class NegStatusPropCurveEntryPrototype : Prototype
    {
        public ulong NegStatusProp { get; protected set; }
        public NegStatusRankCurveEntryPrototype[] RankEntries { get; protected set; }
    }

    public class RankAffixTableByDifficultyEntryPrototype : Prototype
    {
        public ulong DifficultyMin { get; protected set; }
        public ulong DifficultyMax { get; protected set; }
        public RankAffixEntryPrototype[] RankAffixTable { get; protected set; }
    }

    public class TuningPrototype : Prototype
    {
        public ulong Name { get; protected set; }
        public float PlayerInflictedDamageTimerSec { get; protected set; }
        public float PlayerNearbyRange { get; protected set; }
        public NegStatusPropCurveEntryPrototype[] NegativeStatusCurves { get; protected set; }
        public ulong LootFindByLevelDeltaCurve { get; protected set; }
        public ulong SpecialItemFindByLevelDeltaCurve { get; protected set; }
        public ulong LootFindByDifficultyIndexCurve { get; protected set; }
        public ulong PlayerXPByDifficultyIndexCurve { get; protected set; }
        public ulong DeathPenaltyCondition { get; protected set; }
        public ulong PctXPFromParty { get; protected set; }
        public ulong PctXPFromRaid { get; protected set; }
        public ulong Tier { get; protected set; }
        public bool UseTierMinimapColor { get; protected set; }
        public RankAffixEntryPrototype[] RankAffixTable { get; protected set; }
        public float PctXPMultiplier { get; protected set; }
        public bool NumNearbyPlayersScalingEnabled { get; protected set; }
        public float TuningDamageMobToPlayer { get; protected set; }
        public float TuningDamageMobToPlayerDCL { get; protected set; }
        public float TuningDamagePlayerToMob { get; protected set; }
        public float TuningDamagePlayerToMobDCL { get; protected set; }
        public TuningDamageByRankPrototype[] TuningDamageByRank { get; protected set; }
        public TuningDamageByRankPrototype[] TuningDamageByRankDCL { get; protected set; }
        public RankAffixTableByDifficultyEntryPrototype[] RankAffixTableByDifficulty { get; protected set; }
    }

    public class DifficultyModePrototype : Prototype
    {
        public ulong IconPath { get; protected set; }
        public ulong Name { get; protected set; }
        public DEPRECATEDDifficultyMode DifficultyModeEnum { get; protected set; }
        public int MigrationUnlocksAtLevel { get; protected set; }
        public ulong UnlockNotification { get; protected set; }
        public ulong TextStyle { get; protected set; }
    }

    public class DifficultyTierPrototype : Prototype
    {
        public int DEPTier { get; protected set; }
        public DifficultyTier Tier { get; protected set; }
        public float BonusExperiencePct { get; protected set; }
        public float DamageMobToPlayerPct { get; protected set; }
        public float DamagePlayerToMobPct { get; protected set; }
        public float ItemFindCreditsPct { get; protected set; }
        public float ItemFindRarePct { get; protected set; }
        public float ItemFindSpecialPct { get; protected set; }
        public int UnlockLevel { get; protected set; }
        public ulong UIColor { get; protected set; }
        public ulong UIDisplayName { get; protected set; }
        public int BonusItemFindBonusDifficultyMult { get; protected set; }
    }

    public class DifficultyGlobalsPrototype : Prototype
    {
        public ulong MobConLevelCurve { get; protected set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefault { get; protected set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefaultPCZ { get; protected set; }
        public ulong NumNearbyPlayersDmgDefaultMtoP { get; protected set; }
        public ulong NumNearbyPlayersDmgDefaultPtoM { get; protected set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRank { get; protected set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRankPCZ { get; protected set; }
        public ulong DifficultyIndexDamageDefaultMtoP { get; protected set; }
        public ulong DifficultyIndexDamageDefaultPtoM { get; protected set; }
        public DifficultyIndexDamageByRankPrototype[] DifficultyIndexDamageByRank { get; protected set; }
        public float PvPDamageMultiplier { get; protected set; }
        public float PvPCritDamageMultiplier { get; protected set; }
        public ulong PvPDamageScalarFromLevelCurve { get; protected set; }
        public ulong TeamUpDamageScalarFromLevelCurve { get; protected set; }
        public EvalPrototype EvalDamageLevelDeltaMtoP { get; protected set; }
        public EvalPrototype EvalDamageLevelDeltaPtoM { get; protected set; }
    }
}
