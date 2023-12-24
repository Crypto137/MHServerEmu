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
        public ulong Name { get; private set; }
        public ulong EnemyBoost { get; private set; }
        public int Difficulty { get; private set; }
        public ulong AvatarPower { get; private set; }
        public ulong MetaState { get; private set; }
        public MetaStateChallengeTierEnum ChallengeTier { get; private set; }
        public int AdditionalLevels { get; private set; }
        public ulong Category { get; private set; }
        public ulong[] RestrictsAffixes { get; private set; }
        public int UISortOrder { get; private set; }
        public ulong[] KeywordsBlacklist { get; private set; }
        public ulong[] KeywordsWhitelist { get; private set; }
        public EnemyBoostEntryPrototype[] EnemyBoostsFiltered { get; private set; }
        public ulong[] AffixRarityRestrictions { get; private set; }
        public EvalPrototype Eval { get; private set; }
    }

    public class RegionAffixTableTierEntryPrototype : Prototype
    {
        public ulong LootTable { get; private set; }
        public int Tier { get; private set; }
        public ulong Name { get; private set; }
    }

    public class RegionAffixWeightedEntryPrototype : Prototype
    {
        public ulong Affix { get; private set; }
        public int Weight { get; private set; }
    }

    public class RegionAffixTablePrototype : Prototype
    {
        public EvalPrototype EvalTier { get; private set; }
        public EvalPrototype EvalXPBonus { get; private set; }
        public RegionAffixWeightedEntryPrototype[] RegionAffixes { get; private set; }
        public RegionAffixTableTierEntryPrototype[] Tiers { get; private set; }
        public ulong LootSource { get; private set; }
    }

    public class RegionAffixCategoryPrototype : Prototype
    {
        public int MaxPicks { get; private set; }
        public int MinPicks { get; private set; }
    }

    public class EnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost { get; private set; }
        public ulong[] RanksAllowed { get; private set; }
        public ulong[] RanksPrevented { get; private set; }
    }
}
