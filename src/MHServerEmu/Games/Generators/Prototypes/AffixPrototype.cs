using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class AffixPrototype : Prototype
    {
        public AffixPosition Position;
        public ulong Properties;
        public ulong DisplayNameText;
        public int Weight;
        public ulong TypeFilters;
        public PropertyPickInRangeEntryPrototype[] PropertyEntries;
        public ulong[] Keywords;
        public DropRestrictionPrototype[] DropRestrictions;
        public DuplicateHandlingBehavior DuplicateHandlingBehavior;
        public AffixPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixPrototype), proto); }
    }

    public enum DuplicateHandlingBehavior {
	    Fail,
	    Ignore,
	    Overwrite,
	    Append,
    }

    public class AffixPowerModifierPrototype : AffixPrototype
    {
        public bool IsForSinglePowerOnly;
        public EvalPrototype PowerBoostMax;
        public EvalPrototype PowerGrantRankMax;
        public ulong PowerKeywordFilter;
        public EvalPrototype PowerUnlockLevelMax;
        public EvalPrototype PowerUnlockLevelMin;
        public EvalPrototype PowerBoostMin;
        public EvalPrototype PowerGrantRankMin;
        public ulong PowerProgTableTabRef;
        public AffixPowerModifierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixPowerModifierPrototype), proto); }
    }

    public class AffixRegionModifierPrototype : AffixPrototype
    {
        public ulong AffixTable;
        public AffixRegionModifierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixRegionModifierPrototype), proto); }
    }

    public class AffixRegionRestrictedPrototype : AffixPrototype
    {
        public ulong RequiredRegion;
        public ulong[] RequiredRegionKeywords;
        public AffixRegionRestrictedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixRegionRestrictedPrototype), proto); }
    }

    public class AffixTeamUpPrototype : AffixPrototype
    {
        public bool IsAppliedToOwnerAvatar;
        public AffixTeamUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixTeamUpPrototype), proto); }
    }

    public class AffixRunewordPrototype : AffixPrototype
    {
        public ulong Runeword;
        public AffixRunewordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixRunewordPrototype), proto); }
    }

    public class RunewordDefinitionEntryPrototype : Prototype
    {
        public ulong Rune;
        public RunewordDefinitionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RunewordDefinitionEntryPrototype), proto); }
    }

    public class RunewordDefinitionPrototype : Prototype
    {
        public RunewordDefinitionEntryPrototype[] Runes;
        public RunewordDefinitionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RunewordDefinitionPrototype), proto); }
    }

    public class AffixEntryPrototype : Prototype
    {
        public ulong Affix;
        public ulong Power;
        public ulong Avatar;
        public AffixEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixEntryPrototype), proto); }
    }

    public class LeveledAffixEntryPrototype : AffixEntryPrototype
    {
        public int LevelRequired;
        public ulong LockedDescriptionText;
        public LeveledAffixEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeveledAffixEntryPrototype), proto); }
    }

    public class AffixDisplaySlotPrototype : Prototype
    {
        public ulong[] AffixKeywords;
        public ulong DisplayText;
        public AffixDisplaySlotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixDisplaySlotPrototype), proto); }
    }

    public class ModPrototype : Prototype
    {
        public ulong TooltipTitle;
        public ulong UIIcon;
        public ulong TooltipDescription;
        public ulong Properties;
        public ulong PassivePowers;
        public ulong Type;
        public int RanksMax;
        public ulong RankCostCurve;
        public ulong TooltipTemplateCurrentRank;
        public EvalPrototype[] EvalOnCreate;
        public ulong TooltipTemplateNextRank;
        public PropertySetEntryPrototype[] PropertiesForTooltips;
        public ulong UIIconHiRes;
        public ModPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ModPrototype), proto); }
    }

    public class ModTypePrototype : Prototype
    {
        public ulong AggregateProperty;
        public ulong TempProperty;
        public ulong BaseProperty;
        public ulong CurrencyIndexProperty;
        public ulong CurrencyCurve;
        public bool UseCurrencyIndexAsValue;
        public ModTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ModTypePrototype), proto); }
    }

    public class ModGlobalsPrototype : Prototype
    {
        public ulong RankModType;
        public ulong SkillModType;
        public ulong EnemyBoostModType;
        public ulong PvPUpgradeModType;
        public ulong TalentModType;
        public ulong OmegaBonusModType;
        public ulong OmegaHowToTooltipTemplate;
        public ulong InfinityHowToTooltipTemplate;
        public ModGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ModGlobalsPrototype), proto); }
    }

    public class SkillPrototype : ModPrototype
    {
        public ulong DamageBonusByRank;
        public SkillPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SkillPrototype), proto); }
    }

    public class TalentSetPrototype : ModPrototype
    {
        public ulong UITitle;
        public ulong[] Talents;
        public TalentSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TalentSetPrototype), proto); }
    }

    public class TalentPrototype : ModPrototype
    {
        public TalentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TalentPrototype), proto); }
    }

    public class OmegaBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites;
        public int UIHexIndex;
        public OmegaBonusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OmegaBonusPrototype), proto); }
    }

    public class OmegaBonusSetPrototype : ModPrototype
    {
        public ulong UITitle;
        public OmegaBonusPrototype OmegaBonuses;
        public OmegaPageType UIPageType;
        public bool Unlocked;
        public ulong UIColor;
        public ulong UIBackgroundImage;
        public OmegaBonusSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OmegaBonusSetPrototype), proto); }
    }

    public enum OmegaPageType {
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

    public class InfinityGemBonusPrototype : ModPrototype
    {
        public ulong[] Prerequisites;
        public InfinityGemBonusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InfinityGemBonusPrototype), proto); }
    }

    public class InfinityGemSetPrototype : ModPrototype
    {
        public ulong UITitle;
        public InfinityGemBonusPrototype Bonuses;
        public InfinityGem Gem;
        public bool Unlocked;
        public ulong UIColor;
        public ulong UIBackgroundImage;
        public ulong UIDescription;
        public new ulong UIIcon;
        public ulong UIIconRadialNormal;
        public ulong UIIconRadialSelected;
        public InfinityGemSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InfinityGemSetPrototype), proto); }
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

    public class RankPrototype : ModPrototype
    {
        public Rank Rank;
        public HealthBarType HealthBarType;
        public LootRollModifierPrototype[] LootModifiers;
        public LootDropEventType LootTableParam;
        public OverheadInfoDisplayType OverheadInfoDisplayType;
        public ulong[] Keywords;
        public int BonusItemFindPoints;
        public RankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RankPrototype), proto); }
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
    public class EnemyBoostSetPrototype : Prototype
    {
        public ulong[] Modifiers;
        public EnemyBoostSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EnemyBoostSetPrototype), proto); }
    }

    public class EnemyBoostPrototype : ModPrototype
    {
        public ulong ActivePower;
        public bool ShowVisualFX;
        public bool DisableForControlledAgents;
        public bool CountsAsAffixSlot;
        public EnemyBoostPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EnemyBoostPrototype), proto); }
    }

    public class AffixTableEntryPrototype : Prototype
    {
        public ulong AffixTable;
        public int ChancePct;
        public AffixTableEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixTableEntryPrototype), proto); }
    }

    public class RankAffixEntryPrototype : Prototype
    {
        public AffixTableEntryPrototype[] Affixes;
        public ulong Rank;
        public int Weight;
        public RankAffixEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RankAffixEntryPrototype), proto); }
    }

    public class RarityPrototype : Prototype
    {
        public ulong DowngradeTo;
        public ulong TextStyle;
        public ulong Weight;
        public ulong DisplayNameText;
        public int BroadcastToPartyLevelMax;
        public AffixEntryPrototype[] AffixesBuiltIn;
        public int ItemLevelBonus;
        public RarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RarityPrototype), proto); }
    }
}
