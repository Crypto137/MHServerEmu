using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Regions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
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
        public LocaleStringId Name { get; protected set; }
        public PrototypeId EnemyBoost { get; protected set; }
        public int Difficulty { get; protected set; }
        public PrototypeId AvatarPower { get; protected set; }
        public PrototypeId MetaState { get; protected set; }
        public MetaStateChallengeTierEnum ChallengeTier { get; protected set; }
        public int AdditionalLevels { get; protected set; }
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RegionAffixCategoryPrototype Category { get; protected set; }
        public PrototypeId[] RestrictsAffixes { get; protected set; }
        public int UISortOrder { get; protected set; }
        public PrototypeId[] KeywordsBlacklist { get; protected set; }
        public PrototypeId[] KeywordsWhitelist { get; protected set; }
        public EnemyBoostEntryPrototype[] EnemyBoostsFiltered { get; protected set; }
        public PrototypeId[] AffixRarityRestrictions { get; protected set; }
        public EvalPrototype Eval { get; protected set; }
#if GAME_VERSION_1_53
        public PrototypeId DifficultyTier { get; protected set; }
#endif

        //---

        private KeywordsMask _keywordsBlacklistMask;
        private KeywordsMask _keywordsWhitelistMask;

        public override void PostProcess()
        {
            base.PostProcess();

            _keywordsBlacklistMask = KeywordPrototype.GetBitMaskForKeywordList(KeywordsBlacklist);
            _keywordsWhitelistMask = KeywordPrototype.GetBitMaskForKeywordList(KeywordsWhitelist);
        }

        public bool CanApplyToRegion(Region region)
        {
            if (KeywordsBlacklist.HasValue())
            {
                foreach (Area area in region.Areas.Values)
                {
                    if (area.GetKeywordsMask().TestAny(_keywordsBlacklistMask))
                        return false;
                }
            }

            if (KeywordsWhitelist.HasValue())
            {
                foreach (Area area in region.Areas.Values)
                {
                    if (area.GetKeywordsMask().TestAny(_keywordsWhitelistMask))
                        return true;
                }

                return false;
            }

            return true;
        }
    }

    public class RegionAffixTableTierEntryPrototype : Prototype
    {
        public PrototypeId LootTable { get; protected set; }
        public int Tier { get; protected set; }
        public LocaleStringId Name { get; protected set; }
    }

    public class RegionAffixWeightedEntryPrototype : Prototype
    {
        public PrototypeId Affix { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class RegionAffixTablePrototype : Prototype
    {
        public EvalPrototype EvalTier { get; protected set; }
        public EvalPrototype EvalXPBonus { get; protected set; }
        public RegionAffixWeightedEntryPrototype[] RegionAffixes { get; protected set; }
        public RegionAffixTableTierEntryPrototype[] Tiers { get; protected set; }
        public AssetId LootSource { get; protected set; }

        //---

        public RegionAffixTableTierEntryPrototype GetByTier(int affixTier)
        {
            if (Tiers.IsNullOrEmpty())
                return null;

            foreach (RegionAffixTableTierEntryPrototype entry in Tiers)
            {
                if (!Verify.IsNotNull(entry))
                    continue;

                if (entry.Tier == affixTier)
                    return entry;
            }

            return null;
        }
    }

    public class RegionAffixCategoryPrototype : Prototype
    {
        public int MaxPicks { get; protected set; }
        public int MinPicks { get; protected set; }
    }

    public class EnemyBoostEntryPrototype : Prototype
    {
        public PrototypeId EnemyBoost { get; protected set; }
        public PrototypeId[] RanksAllowed { get; protected set; }
        public PrototypeId[] RanksPrevented { get; protected set; }
    }
}
