namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum DuplicateHandlingBehavior
    {
        Fail,
        Ignore,
        Overwrite,
        Append,
    }

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
    public enum HealthBarType
    {
        Default = 0,
        EliteMinion = 1,
        MiniBoss = 2,
        Boss = 3,
        None = 4,
    }

    public enum OverheadInfoDisplayType
    {
        Default = 0,
        Always = 1,
        Never = 2,
    }

    #endregion

    public class AffixPrototype : Prototype
    {
        public AffixPosition Position { get; set; }
        public ulong Properties { get; set; }
        public ulong DisplayNameText { get; set; }
        public int Weight { get; set; }
        public ulong TypeFilters { get; set; }
        public PropertyPickInRangeEntryPrototype[] PropertyEntries { get; set; }
        public ulong[] Keywords { get; set; }
        public DropRestrictionPrototype[] DropRestrictions { get; set; }
        public DuplicateHandlingBehavior DuplicateHandlingBehavior { get; set; }
    }

    public class AffixPowerModifierPrototype : AffixPrototype
    {
        public bool IsForSinglePowerOnly { get; set; }
        public EvalPrototype PowerBoostMax { get; set; }
        public EvalPrototype PowerGrantRankMax { get; set; }
        public ulong PowerKeywordFilter { get; set; }
        public EvalPrototype PowerUnlockLevelMax { get; set; }
        public EvalPrototype PowerUnlockLevelMin { get; set; }
        public EvalPrototype PowerBoostMin { get; set; }
        public EvalPrototype PowerGrantRankMin { get; set; }
        public ulong PowerProgTableTabRef { get; set; }
    }

    public class AffixRegionModifierPrototype : AffixPrototype
    {
        public ulong AffixTable { get; set; }
    }

    public class AffixRegionRestrictedPrototype : AffixPrototype
    {
        public ulong RequiredRegion { get; set; }
        public ulong[] RequiredRegionKeywords { get; set; }
    }

    public class AffixTeamUpPrototype : AffixPrototype
    {
        public bool IsAppliedToOwnerAvatar { get; set; }
    }

    public class AffixRunewordPrototype : AffixPrototype
    {
        public ulong Runeword { get; set; }
    }

    public class RunewordDefinitionEntryPrototype : Prototype
    {
        public ulong Rune { get; set; }
    }

    public class RunewordDefinitionPrototype : Prototype
    {
        public RunewordDefinitionEntryPrototype[] Runes { get; set; }
    }

    public class AffixEntryPrototype : Prototype
    {
        public ulong Affix { get; set; }
        public ulong Power { get; set; }
        public ulong Avatar { get; set; }
    }

    public class LeveledAffixEntryPrototype : AffixEntryPrototype
    {
        public int LevelRequired { get; set; }
        public ulong LockedDescriptionText { get; set; }
    }

    public class AffixDisplaySlotPrototype : Prototype
    {
        public ulong[] AffixKeywords { get; set; }
        public ulong DisplayText { get; set; }
    }

    public class ModPrototype : Prototype
    {
        public ulong TooltipTitle { get; set; }
        public ulong UIIcon { get; set; }
        public ulong TooltipDescription { get; set; }
        public ulong Properties { get; set; }
        public ulong PassivePowers { get; set; }
        public ulong Type { get; set; }
        public int RanksMax { get; set; }
        public ulong RankCostCurve { get; set; }
        public ulong TooltipTemplateCurrentRank { get; set; }
        public EvalPrototype[] EvalOnCreate { get; set; }
        public ulong TooltipTemplateNextRank { get; set; }
        public PropertySetEntryPrototype[] PropertiesForTooltips { get; set; }
        public ulong UIIconHiRes { get; set; }
    }

    public class ModTypePrototype : Prototype
    {
        public ulong AggregateProperty { get; set; }
        public ulong TempProperty { get; set; }
        public ulong BaseProperty { get; set; }
        public ulong CurrencyIndexProperty { get; set; }
        public ulong CurrencyCurve { get; set; }
        public bool UseCurrencyIndexAsValue { get; set; }
    }

    public class ModGlobalsPrototype : Prototype
    {
        public ulong RankModType { get; set; }
        public ulong SkillModType { get; set; }
        public ulong EnemyBoostModType { get; set; }
        public ulong PvPUpgradeModType { get; set; }
        public ulong TalentModType { get; set; }
        public ulong OmegaBonusModType { get; set; }
        public ulong OmegaHowToTooltipTemplate { get; set; }
        public ulong InfinityHowToTooltipTemplate { get; set; }
    }

    public class SkillPrototype : ModPrototype
    {
        public ulong DamageBonusByRank { get; set; }
    }

    public class TalentSetPrototype : ModPrototype
    {
        public ulong UITitle { get; set; }
        public ulong[] Talents { get; set; }
    }

    public class TalentPrototype : ModPrototype
    {
    }

    public class OmegaBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites { get; set; }
        public int UIHexIndex { get; set; }
    }

    public class OmegaBonusSetPrototype : ModPrototype
    {
        public ulong UITitle { get; set; }
        public OmegaBonusPrototype OmegaBonuses { get; set; }
        public OmegaPageType UIPageType { get; set; }
        public bool Unlocked { get; set; }
        public ulong UIColor { get; set; }
        public ulong UIBackgroundImage { get; set; }
    }

    public class InfinityGemBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites { get; set; }
    }

    public class InfinityGemSetPrototype : ModPrototype
    {
        public ulong UITitle { get; set; }
        public InfinityGemBonusPrototype Bonuses { get; set; }
        public InfinityGem Gem { get; set; }
        public bool Unlocked { get; set; }
        public ulong UIColor { get; set; }
        public ulong UIBackgroundImage { get; set; }
        public ulong UIDescription { get; set; }
        public new ulong UIIcon { get; set; }
        public ulong UIIconRadialNormal { get; set; }
        public ulong UIIconRadialSelected { get; set; }
    }

    public class RankPrototype : ModPrototype
    {
        public Rank Rank { get; set; }
        public HealthBarType HealthBarType { get; set; }
        public LootRollModifierPrototype[] LootModifiers { get; set; }
        public LootDropEventType LootTableParam { get; set; }
        public OverheadInfoDisplayType OverheadInfoDisplayType { get; set; }
        public ulong[] Keywords { get; set; }
        public int BonusItemFindPoints { get; set; }
    }

    public class EnemyBoostSetPrototype : Prototype
    {
        public ulong[] Modifiers { get; set; }
    }

    public class EnemyBoostPrototype : ModPrototype
    {
        public ulong ActivePower { get; set; }
        public bool ShowVisualFX { get; set; }
        public bool DisableForControlledAgents { get; set; }
        public bool CountsAsAffixSlot { get; set; }
    }

    public class AffixTableEntryPrototype : Prototype
    {
        public ulong AffixTable { get; set; }
        public int ChancePct { get; set; }
    }

    public class RankAffixEntryPrototype : Prototype
    {
        public AffixTableEntryPrototype[] Affixes { get; set; }
        public ulong Rank { get; set; }
        public int Weight { get; set; }
    }

    public class RarityPrototype : Prototype
    {
        public ulong DowngradeTo { get; set; }
        public ulong TextStyle { get; set; }
        public ulong Weight { get; set; }
        public ulong DisplayNameText { get; set; }
        public int BroadcastToPartyLevelMax { get; set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; set; }
        public int ItemLevelBonus { get; set; }
    }
}
