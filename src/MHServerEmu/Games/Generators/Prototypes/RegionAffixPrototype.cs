using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class RegionAffixPrototype : Prototype
    {
        public ulong Name;
        public ulong EnemyBoost;
        public int Difficulty;
        public ulong AvatarPower;
        public ulong MetaState;
        public MetaStateChallengeTierEnum ChallengeTier;
        public int AdditionalLevels;
        public ulong Category;
        public ulong[] RestrictsAffixes;
        public int UISortOrder;
        public ulong[] KeywordsBlacklist;
        public ulong[] KeywordsWhitelist;
        public EnemyBoostEntryPrototype[] EnemyBoostsFiltered;
        public ulong[] AffixRarityRestrictions;
        public EvalPrototype Eval;
        public RegionAffixPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionAffixPrototype), proto); }
    }
    public enum MetaStateChallengeTierEnum
    {
        None = 0,
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5,
    }
    public class RegionAffixTableTierEntryPrototype : Prototype
    {
        public ulong LootTable;
        public int Tier;
        public ulong Name;
        public RegionAffixTableTierEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionAffixTableTierEntryPrototype), proto); }
    }

    public class RegionAffixWeightedEntryPrototype : Prototype
    {
        public ulong Affix;
        public int Weight;
        public RegionAffixWeightedEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionAffixWeightedEntryPrototype), proto); }
    }

    public class RegionAffixTablePrototype : Prototype
    {
        public EvalPrototype EvalTier;
        public EvalPrototype EvalXPBonus;
        public RegionAffixWeightedEntryPrototype[] RegionAffixes;
        public RegionAffixTableTierEntryPrototype[] Tiers;
        public ulong LootSource;
        public RegionAffixTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionAffixTablePrototype), proto); }
    }

    public class RegionAffixCategoryPrototype : Prototype
    {
        public int MaxPicks;
        public int MinPicks;
        public RegionAffixCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionAffixCategoryPrototype), proto); }
    }

    public class EnemyBoostEntryPrototype : Prototype
    {
        public ulong EnemyBoost;
        public ulong[] RanksAllowed;
        public ulong[] RanksPrevented;
        public EnemyBoostEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EnemyBoostEntryPrototype), proto); }
    }
}
