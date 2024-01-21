using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Fail)]
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

    [AssetEnum((int)None)]
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

    [AssetEnum((int)Popcorn)]
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

    [AssetEnum((int)None)]
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

    [AssetEnum((int)Default)]
    public enum HealthBarType
    {
        Default = 0,
        EliteMinion = 1,
        MiniBoss = 2,
        Boss = 3,
        None = 4,
    }

    [AssetEnum((int)Default)]
    public enum OverheadInfoDisplayType
    {
        Default = 0,
        Always = 1,
        Never = 2,
    }

    #endregion

    public class AffixPrototype : Prototype
    {
        public AffixPosition Position { get; protected set; }
        public ulong Properties { get; protected set; }
        public ulong DisplayNameText { get; protected set; }
        public int Weight { get; protected set; }
        public ulong[] TypeFilters { get; protected set; }
        public PropertyPickInRangeEntryPrototype[] PropertyEntries { get; protected set; }
        public ulong[] Keywords { get; protected set; }
        public DropRestrictionPrototype[] DropRestrictions { get; protected set; }
        public DuplicateHandlingBehavior DuplicateHandlingBehavior { get; protected set; }
    }

    public class AffixPowerModifierPrototype : AffixPrototype
    {
        public bool IsForSinglePowerOnly { get; protected set; }
        public EvalPrototype PowerBoostMax { get; protected set; }
        public EvalPrototype PowerGrantRankMax { get; protected set; }
        public ulong PowerKeywordFilter { get; protected set; }
        public EvalPrototype PowerUnlockLevelMax { get; protected set; }
        public EvalPrototype PowerUnlockLevelMin { get; protected set; }
        public EvalPrototype PowerBoostMin { get; protected set; }
        public EvalPrototype PowerGrantRankMin { get; protected set; }
        public ulong PowerProgTableTabRef { get; protected set; }
    }

    public class AffixRegionModifierPrototype : AffixPrototype
    {
        public ulong AffixTable { get; protected set; }
    }

    public class AffixRegionRestrictedPrototype : AffixPrototype
    {
        public ulong RequiredRegion { get; protected set; }
        public ulong[] RequiredRegionKeywords { get; protected set; }
    }

    public class AffixTeamUpPrototype : AffixPrototype
    {
        public bool IsAppliedToOwnerAvatar { get; protected set; }
    }

    public class AffixRunewordPrototype : AffixPrototype
    {
        public ulong Runeword { get; protected set; }
    }

    public class RunewordDefinitionEntryPrototype : Prototype
    {
        public ulong Rune { get; protected set; }
    }

    public class RunewordDefinitionPrototype : Prototype
    {
        public RunewordDefinitionEntryPrototype[] Runes { get; protected set; }
    }

    public class AffixEntryPrototype : Prototype
    {
        public ulong Affix { get; protected set; }
        public ulong Power { get; protected set; }
        public ulong Avatar { get; protected set; }
    }

    public class LeveledAffixEntryPrototype : AffixEntryPrototype
    {
        public int LevelRequired { get; protected set; }
        public ulong LockedDescriptionText { get; protected set; }
    }

    public class AffixDisplaySlotPrototype : Prototype
    {
        public ulong[] AffixKeywords { get; protected set; }
        public ulong DisplayText { get; protected set; }
    }

    public class ModPrototype : Prototype
    {
        public ulong TooltipTitle { get; protected set; }
        public ulong UIIcon { get; protected set; }
        public ulong TooltipDescription { get; protected set; }
        public PrototypePropertyCollection Properties { get; protected set; }     // Property list, should this be a property collection?
        public ulong[] PassivePowers { get; protected set; }
        public ulong Type { get; protected set; }
        public int RanksMax { get; protected set; }
        public ulong RankCostCurve { get; protected set; }
        public ulong TooltipTemplateCurrentRank { get; protected set; }
        public EvalPrototype[] EvalOnCreate { get; protected set; }
        public ulong TooltipTemplateNextRank { get; protected set; }
        public PropertySetEntryPrototype[] PropertiesForTooltips { get; protected set; }
        public ulong UIIconHiRes { get; protected set; }
    }

    public class ModTypePrototype : Prototype
    {
        public ulong AggregateProperty { get; protected set; }
        public ulong TempProperty { get; protected set; }
        public ulong BaseProperty { get; protected set; }
        public ulong CurrencyIndexProperty { get; protected set; }
        public ulong CurrencyCurve { get; protected set; }
        public bool UseCurrencyIndexAsValue { get; protected set; }
    }

    public class ModGlobalsPrototype : Prototype
    {
        public ulong RankModType { get; protected set; }
        public ulong SkillModType { get; protected set; }
        public ulong EnemyBoostModType { get; protected set; }
        public ulong PvPUpgradeModType { get; protected set; }
        public ulong TalentModType { get; protected set; }
        public ulong OmegaBonusModType { get; protected set; }
        public ulong OmegaHowToTooltipTemplate { get; protected set; }
        public ulong InfinityHowToTooltipTemplate { get; protected set; }
    }

    public class SkillPrototype : ModPrototype
    {
        public ulong DamageBonusByRank { get; protected set; }
    }

    public class TalentSetPrototype : ModPrototype
    {
        public ulong UITitle { get; protected set; }
        public ulong[] Talents { get; protected set; }
    }

    public class TalentPrototype : ModPrototype
    {
    }

    public class OmegaBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites { get; protected set; }
        public int UIHexIndex { get; protected set; }
    }

    public class OmegaBonusSetPrototype : ModPrototype
    {
        public ulong UITitle { get; protected set; }
        public ulong[] OmegaBonuses { get; protected set; }
        public OmegaPageType UIPageType { get; protected set; }
        public bool Unlocked { get; protected set; }
        public ulong UIColor { get; protected set; }
        public ulong UIBackgroundImage { get; protected set; }
    }

    public class InfinityGemBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites { get; protected set; }
    }

    public class InfinityGemSetPrototype : ModPrototype
    {
        public ulong UITitle { get; protected set; }
        public ulong[] Bonuses { get; protected set; }    // VectorPrototypeRefPtr InfinityGemBonusPrototype
        public InfinityGem Gem { get; protected set; }
        public bool Unlocked { get; protected set; }
        public ulong UIColor { get; protected set; }
        public ulong UIBackgroundImage { get; protected set; }
        public ulong UIDescription { get; protected set; }
        public new ulong UIIcon { get; protected set; }
        public ulong UIIconRadialNormal { get; protected set; }
        public ulong UIIconRadialSelected { get; protected set; }
    }

    public class RankPrototype : ModPrototype
    {
        public Rank Rank { get; protected set; }
        public HealthBarType HealthBarType { get; protected set; }
        public LootRollModifierPrototype[] LootModifiers { get; protected set; }
        public LootDropEventType LootTableParam { get; protected set; }
        public OverheadInfoDisplayType OverheadInfoDisplayType { get; protected set; }
        public ulong[] Keywords { get; protected set; }
        public int BonusItemFindPoints { get; protected set; }
    }

    public class EnemyBoostSetPrototype : Prototype
    {
        public ulong[] Modifiers { get; protected set; }
    }

    public class EnemyBoostPrototype : ModPrototype
    {
        public ulong ActivePower { get; protected set; }
        public bool ShowVisualFX { get; protected set; }
        public bool DisableForControlledAgents { get; protected set; }
        public bool CountsAsAffixSlot { get; protected set; }
    }

    public class AffixTableEntryPrototype : Prototype
    {
        public ulong AffixTable { get; protected set; }
        public int ChancePct { get; protected set; }
    }

    public class RankAffixEntryPrototype : Prototype
    {
        public AffixTableEntryPrototype[] Affixes { get; protected set; }
        public ulong Rank { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class RarityPrototype : Prototype
    {
        public ulong DowngradeTo { get; protected set; }
        public ulong TextStyle { get; protected set; }
        public ulong Weight { get; protected set; }
        public ulong DisplayNameText { get; protected set; }
        public int BroadcastToPartyLevelMax { get; protected set; }
        public AffixEntryPrototype[] AffixesBuiltIn { get; protected set; }
        public int ItemLevelBonus { get; protected set; }
    }
}
