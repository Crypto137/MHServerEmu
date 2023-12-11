using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum MetaStateChallengeTierEnum
    {
        None = 0,
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5,
    }

    #endregion

    public class RegionAffixPrototype : Prototype
    {
        public ulong Name { get; set; }
        public ulong EnemyBoost { get; set; }
        public int Difficulty { get; set; }
        public ulong AvatarPower { get; set; }
        public ulong MetaState { get; set; }
        public MetaStateChallengeTierEnum ChallengeTier { get; set; }
        public int AdditionalLevels { get; set; }
        public ulong Category { get; set; }
        public ulong[] RestrictsAffixes { get; set; }
        public int UISortOrder { get; set; }
        public ulong[] KeywordsBlacklist { get; set; }
        public ulong[] KeywordsWhitelist { get; set; }
        public EnemyBoostEntryPrototype[] EnemyBoostsFiltered { get; set; }
        public ulong[] AffixRarityRestrictions { get; set; }
        public EvalPrototype Eval { get; set; }
    }

    public class RegionAffixTableTierEntryPrototype : Prototype
    {
        public ulong LootTable { get; set; }
        public int Tier { get; set; }
        public ulong Name { get; set; }
    }

    public class RegionAffixWeightedEntryPrototype : Prototype
    {
        public ulong Affix { get; set; }
        public int Weight { get; set; }
    }

    public class RegionAffixTablePrototype : Prototype
    {
        public EvalPrototype EvalTier { get; set; }
        public EvalPrototype EvalXPBonus { get; set; }
        public RegionAffixWeightedEntryPrototype[] RegionAffixes { get; set; }
        public RegionAffixTableTierEntryPrototype[] Tiers { get; set; }
        public ulong LootSource { get; set; }
    }

    public class RegionAffixCategoryPrototype : Prototype
    {
        public int MaxPicks { get; set; }
        public int MinPicks { get; set; }
    }

    public class EnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost { get; set; }
        public ulong[] RanksAllowed { get; set; }
        public ulong[] RanksPrevented { get; set; }
    }
}
