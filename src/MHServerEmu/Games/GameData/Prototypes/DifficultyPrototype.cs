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
        public ulong TuningTable { get; private set; }
    }

    public class NumNearbyPlayersDmgByRankPrototype : Prototype
    {
        public Rank Rank { get; private set; }
        public ulong MobToPlayerCurve { get; private set; }
        public ulong PlayerToMobCurve { get; private set; }
    }

    public class DifficultyIndexDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; private set; }
        public ulong MobToPlayerCurve { get; private set; }
        public ulong PlayerToMobCurve { get; private set; }
    }

    public class TuningDamageByRankPrototype : Prototype
    {
        public Rank Rank { get; private set; }
        public float TuningMobToPlayer { get; private set; }
        public float TuningPlayerToMob { get; private set; }
    }

    public class NegStatusRankCurveEntryPrototype : Prototype
    {
        public Rank Rank { get; private set; }
        public ulong TenacityModifierCurve { get; private set; }
    }

    public class NegStatusPropCurveEntryPrototype : Prototype
    {
        public ulong NegStatusProp { get; private set; }
        public NegStatusRankCurveEntryPrototype[] RankEntries { get; private set; }
    }

    public class RankAffixTableByDifficultyEntryPrototype : Prototype
    {
        public ulong DifficultyMin { get; private set; }
        public ulong DifficultyMax { get; private set; }
        public RankAffixEntryPrototype[] RankAffixTable { get; private set; }
    }

    public class TuningPrototype : Prototype
    {
        public ulong Name { get; private set; }
        public float PlayerInflictedDamageTimerSec { get; private set; }
        public float PlayerNearbyRange { get; private set; }
        public NegStatusPropCurveEntryPrototype[] NegativeStatusCurves { get; private set; }
        public ulong LootFindByLevelDeltaCurve { get; private set; }
        public ulong SpecialItemFindByLevelDeltaCurve { get; private set; }
        public ulong LootFindByDifficultyIndexCurve { get; private set; }
        public ulong PlayerXPByDifficultyIndexCurve { get; private set; }
        public ulong DeathPenaltyCondition { get; private set; }
        public ulong PctXPFromParty { get; private set; }
        public ulong PctXPFromRaid { get; private set; }
        public ulong Tier { get; private set; }
        public bool UseTierMinimapColor { get; private set; }
        public RankAffixEntryPrototype[] RankAffixTable { get; private set; }
        public float PctXPMultiplier { get; private set; }
        public bool NumNearbyPlayersScalingEnabled { get; private set; }
        public float TuningDamageMobToPlayer { get; private set; }
        public float TuningDamageMobToPlayerDCL { get; private set; }
        public float TuningDamagePlayerToMob { get; private set; }
        public float TuningDamagePlayerToMobDCL { get; private set; }
        public TuningDamageByRankPrototype[] TuningDamageByRank { get; private set; }
        public TuningDamageByRankPrototype[] TuningDamageByRankDCL { get; private set; }
        public RankAffixTableByDifficultyEntryPrototype[] RankAffixTableByDifficulty { get; private set; }
    }

    public class DifficultyModePrototype : Prototype
    {
        public ulong IconPath { get; private set; }
        public ulong Name { get; private set; }
        public DEPRECATEDDifficultyMode DifficultyModeEnum { get; private set; }
        public int MigrationUnlocksAtLevel { get; private set; }
        public ulong UnlockNotification { get; private set; }
        public ulong TextStyle { get; private set; }
    }

    public class DifficultyTierPrototype : Prototype
    {
        public int DEPTier { get; private set; }
        public DifficultyTier Tier { get; private set; }
        public float BonusExperiencePct { get; private set; }
        public float DamageMobToPlayerPct { get; private set; }
        public float DamagePlayerToMobPct { get; private set; }
        public float ItemFindCreditsPct { get; private set; }
        public float ItemFindRarePct { get; private set; }
        public float ItemFindSpecialPct { get; private set; }
        public int UnlockLevel { get; private set; }
        public ulong UIColor { get; private set; }
        public ulong UIDisplayName { get; private set; }
        public int BonusItemFindBonusDifficultyMult { get; private set; }
    }

    public class DifficultyGlobalsPrototype : Prototype
    {
        public ulong MobConLevelCurve { get; private set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefault { get; private set; }
        public RegionDifficultySettingsPrototype RegionSettingsDefaultPCZ { get; private set; }
        public ulong NumNearbyPlayersDmgDefaultMtoP { get; private set; }
        public ulong NumNearbyPlayersDmgDefaultPtoM { get; private set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRank { get; private set; }
        public NumNearbyPlayersDmgByRankPrototype[] NumNearbyPlayersDmgByRankPCZ { get; private set; }
        public ulong DifficultyIndexDamageDefaultMtoP { get; private set; }
        public ulong DifficultyIndexDamageDefaultPtoM { get; private set; }
        public DifficultyIndexDamageByRankPrototype[] DifficultyIndexDamageByRank { get; private set; }
        public float PvPDamageMultiplier { get; private set; }
        public float PvPCritDamageMultiplier { get; private set; }
        public ulong PvPDamageScalarFromLevelCurve { get; private set; }
        public ulong TeamUpDamageScalarFromLevelCurve { get; private set; }
        public EvalPrototype EvalDamageLevelDeltaMtoP { get; private set; }
        public EvalPrototype EvalDamageLevelDeltaPtoM { get; private set; }
    }
}
