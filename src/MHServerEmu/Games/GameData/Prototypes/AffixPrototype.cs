using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum DuplicateHandlingBehavior
    {
        Fail,
        Ignore,
        Overwrite,
        Append,
    }

    [AssetEnum]
    public enum OmegaPageType
    {
        Psionics = 12,
        CosmicEntities = 1,
        ArcaneAttunement = 2,
        InterstellarExploration = 3,
        SpecialWeapons = 4,
        ExtraDimensionalTravel = 5,
        MolecularAdjustment = 13,
        RadioactiveOrigins = 7,
        TemporalManipulation = 8,
        Nanotechnology = 9,
        SupernaturalInvestigation = 10,
        HumanAugmentation = 11,
        NeuralEnhancement = 0,
        Xenobiology = 6,
    }

    [AssetEnum]
    public enum InfinityGem
    {
        Soul = 3,
        Time = 5,
        Space = 4,
        Mind = 0,
        Reality = 2,
        Power = 1,
        None = 7,
    }

    [AssetEnum]
    public enum Rank
    {
        Popcorn,
        Champion,
        Elite,
        MiniBoss,
        Boss,
        Player,
        GroupBoss,
        TeamUp,
    }

    [AssetEnum]
    public enum LootDropEventType
    {
        None = 0,
        OnInteractedWith = 3,
        OnHealthBelowPct = 2,
        OnHealthBelowPctHit = 1,
        OnKilled = 4,
        OnKilledChampion = 5,
        OnKilledElite = 6,
        OnKilledMiniBoss = 7,
        OnHit = 8,
        OnDamagedForPctHealth = 9,
    }

    [AssetEnum]
    public enum HealthBarType
    {
        Default = 0,
        EliteMinion = 1,
        MiniBoss = 2,
        Boss = 3,
        None = 4,
    }

    [AssetEnum]
    public enum OverheadInfoDisplayType
    {
        Default = 0,
        Always = 1,
        Never = 2,
    }

    #endregion

    public class AffixPrototype : Prototype
    {
        public AffixPosition Position { get; private set; }
        public ulong Properties { get; private set; }
        public ulong DisplayNameText { get; private set; }
        public int Weight { get; private set; }
        public ulong TypeFilters { get; private set; }
        public PropertyPickInRangeEntryPrototype[] PropertyEntries { get; private set; }
        public ulong[] Keywords { get; private set; }
        public DropRestrictionPrototype[] DropRestrictions { get; private set; }
        public DuplicateHandlingBehavior DuplicateHandlingBehavior { get; private set; }
    }

    public class AffixPowerModifierPrototype : AffixPrototype
    {
        public bool IsForSinglePowerOnly { get; private set; }
        public EvalPrototype PowerBoostMax { get; private set; }
        public EvalPrototype PowerGrantRankMax { get; private set; }
        public ulong PowerKeywordFilter { get; private set; }
        public EvalPrototype PowerUnlockLevelMax { get; private set; }
        public EvalPrototype PowerUnlockLevelMin { get; private set; }
        public EvalPrototype PowerBoostMin { get; private set; }
        public EvalPrototype PowerGrantRankMin { get; private set; }
        public ulong PowerProgTableTabRef { get; private set; }
    }

    public class AffixRegionModifierPrototype : AffixPrototype
    {
        public ulong AffixTable { get; private set; }
    }

    public class AffixRegionRestrictedPrototype : AffixPrototype
    {
        public ulong RequiredRegion { get; private set; }
        public ulong[] RequiredRegionKeywords { get; private set; }
    }

    public class AffixTeamUpPrototype : AffixPrototype
    {
        public bool IsAppliedToOwnerAvatar { get; private set; }
    }

    public class AffixRunewordPrototype : AffixPrototype
    {
        public ulong Runeword { get; private set; }
    }

    public class RunewordDefinitionEntryPrototype : Prototype
    {
        public ulong Rune { get; private set; }
    }

    public class RunewordDefinitionPrototype : Prototype
    {
        public RunewordDefinitionEntryPrototype[] Runes { get; private set; }
    }

    public class AffixEntryPrototype : Prototype
    {
        public ulong Affix { get; private set; }
        public ulong Power { get; private set; }
        public ulong Avatar { get; private set; }
    }

    public class LeveledAffixEntryPrototype : AffixEntryPrototype
    {
        public int LevelRequired { get; private set; }
        public ulong LockedDescriptionText { get; private set; }
    }

    public class AffixDisplaySlotPrototype : Prototype
    {
        public ulong[] AffixKeywords { get; private set; }
        public ulong DisplayText { get; private set; }
    }

    public class ModPrototype : Prototype
    {
        public ulong TooltipTitle { get; private set; }
        public ulong UIIcon { get; private set; }
        public ulong TooltipDescription { get; private set; }
        public ulong Properties { get; private set; }
        public ulong PassivePowers { get; private set; }
        public ulong Type { get; private set; }
        public int RanksMax { get; private set; }
        public ulong RankCostCurve { get; private set; }
        public ulong TooltipTemplateCurrentRank { get; private set; }
        public EvalPrototype[] EvalOnCreate { get; private set; }
        public ulong TooltipTemplateNextRank { get; private set; }
        public PropertySetEntryPrototype[] PropertiesForTooltips { get; private set; }
        public ulong UIIconHiRes { get; private set; }
    }

    public class ModTypePrototype : Prototype
    {
        public ulong AggregateProperty { get; private set; }
        public ulong TempProperty { get; private set; }
        public ulong BaseProperty { get; private set; }
        public ulong CurrencyIndexProperty { get; private set; }
        public ulong CurrencyCurve { get; private set; }
        public bool UseCurrencyIndexAsValue { get; private set; }
    }

    public class ModGlobalsPrototype : Prototype
    {
        public ulong RankModType { get; private set; }
        public ulong SkillModType { get; private set; }
        public ulong EnemyBoostModType { get; private set; }
        public ulong PvPUpgradeModType { get; private set; }
        public ulong TalentModType { get; private set; }
        public ulong OmegaBonusModType { get; private set; }
        public ulong OmegaHowToTooltipTemplate { get; private set; }
        public ulong InfinityHowToTooltipTemplate { get; private set; }
    }

    public class SkillPrototype : ModPrototype
    {
        public ulong DamageBonusByRank { get; private set; }
    }

    public class TalentSetPrototype : ModPrototype
    {
        public ulong UITitle { get; private set; }
        public ulong[] Talents { get; private set; }
    }

    public class TalentPrototype : ModPrototype
    {
    }

    public class OmegaBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites { get; private set; }
        public int UIHexIndex { get; private set; }
    }

    public class OmegaBonusSetPrototype : ModPrototype
    {
        public ulong UITitle { get; private set; }
        public OmegaBonusPrototype OmegaBonuses { get; private set; }
        public OmegaPageType UIPageType { get; private set; }
        public bool Unlocked { get; private set; }
        public ulong UIColor { get; private set; }
        public ulong UIBackgroundImage { get; private set; }
    }

    public class InfinityGemBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites { get; private set; }
    }

    public class InfinityGemSetPrototype : ModPrototype
    {
        public ulong UITitle { get; private set; }
        public InfinityGemBonusPrototype Bonuses { get; private set; }
        public InfinityGem Gem { get; private set; }
        public bool Unlocked { get; private set; }
        public ulong UIColor { get; private set; }
        public ulong UIBackgroundImage { get; private set; }
        public ulong UIDescription { get; private set; }
        public new ulong UIIcon { get; private set; }
        public ulong UIIconRadialNormal { get; private set; }
        public ulong UIIconRadialSelected { get; private set; }
    }

    public class RankPrototype : ModPrototype
    {
        public Rank Rank { get; private set; }
        public HealthBarType HealthBarType { get; private set; }
        public LootRollModifierPrototype[] LootModifiers { get; private set; }
        public LootDropEventType LootTableParam { get; private set; }
        public OverheadInfoDisplayType OverheadInfoDisplayType { get; private set; }
        public ulong[] Keywords { get; private set; }
        public int BonusItemFindPoints { get; private set; }
    }

    public class EnemyBoostSetPrototype : Prototype
    {
        public ulong[] Modifiers { get; private set; }
    }

    public class EnemyBoostPrototype : ModPrototype
    {
        public ulong ActivePower { get; private set; }
        public bool ShowVisualFX { get; private set; }
        public bool DisableForControlledAgents { get; private set; }
        public bool CountsAsAffixSlot { get; private set; }
    }

    public class AffixTableEntryPrototype : Prototype
    {
        public ulong AffixTable { get; private set; }
        public int ChancePct { get; private set; }
    }

    public class RankAffixEntryPrototype : Prototype
    {
        public AffixTableEntryPrototype[] Affixes { get; private set; }
        public ulong Rank { get; private set; }
        public int Weight { get; private set; }
    }

    public class RarityPrototype : Prototype
    {
        public ulong DowngradeTo { get; private set; }
        public ulong TextStyle { get; private set; }
        public ulong Weight { get; private set; }
        public ulong DisplayNameText { get; private set; }
        public int BroadcastToPartyLevelMax { get; private set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; private set; }
        public int ItemLevelBonus { get; private set; }
    }
}
