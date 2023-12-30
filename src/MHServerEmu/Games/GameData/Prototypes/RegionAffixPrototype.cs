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
        public ulong Name { get; protected set; }
        public ulong EnemyBoost { get; protected set; }
        public int Difficulty { get; protected set; }
        public ulong AvatarPower { get; protected set; }
        public ulong MetaState { get; protected set; }
        public MetaStateChallengeTierEnum ChallengeTier { get; protected set; }
        public int AdditionalLevels { get; protected set; }
        public ulong Category { get; protected set; }
        public ulong[] RestrictsAffixes { get; protected set; }
        public int UISortOrder { get; protected set; }
        public ulong[] KeywordsBlacklist { get; protected set; }
        public ulong[] KeywordsWhitelist { get; protected set; }
        public EnemyBoostEntryPrototype[] EnemyBoostsFiltered { get; protected set; }
        public ulong[] AffixRarityRestrictions { get; protected set; }
        public EvalPrototype Eval { get; protected set; }
    }

    public class RegionAffixTableTierEntryPrototype : Prototype
    {
        public ulong LootTable { get; protected set; }
        public int Tier { get; protected set; }
        public ulong Name { get; protected set; }
    }

    public class RegionAffixWeightedEntryPrototype : Prototype
    {
        public ulong Affix { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class RegionAffixTablePrototype : Prototype
    {
        public EvalPrototype EvalTier { get; protected set; }
        public EvalPrototype EvalXPBonus { get; protected set; }
        public RegionAffixWeightedEntryPrototype[] RegionAffixes { get; protected set; }
        public RegionAffixTableTierEntryPrototype[] Tiers { get; protected set; }
        public ulong LootSource { get; protected set; }
    }

    public class RegionAffixCategoryPrototype : Prototype
    {
        public int MaxPicks { get; protected set; }
        public int MinPicks { get; protected set; }
    }

    public class EnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost { get; protected set; }
        public ulong[] RanksAllowed { get; protected set; }
        public ulong[] RanksPrevented { get; protected set; }
    }
}
