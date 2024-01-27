using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum TooltipSectionType
    {
        Default = 0,
        DividerLine = 1,
        Bar = 2,
        Bar_PetTech = 3,
        IconLabeled = 4,
        IconTable = 5,
        Procedural = 6,
        Table = 7,
        TableRow = 8,
        ItemAffixes = 9,
    }

    [AssetEnum]
    public enum TooltipSectionKey   // UI/Tooltips/TooltipProceduralKey.type
    {
        Invalid = 0,
        PowerStatsCurrentRank = 1,
    }

    #endregion

    public class PowerTooltipSectionPrototype : Prototype
    {
        public PrototypeId Description { get; protected set; }
        public PrototypeId Value { get; protected set; }
    }

    public class PowerTooltipSectionOverridePrototype : PowerTooltipSectionPrototype
    {
        public LocaleStringId DescTokenSourcePrefixOverride { get; protected set; }
        public LocaleStringId ValueTokenSourcePrefixOverride { get; protected set; }
    }

    public class PowerTooltipEntryPrototype : Prototype
    {
        public LocaleStringId TokenSourcePrefix { get; protected set; }
        public PrototypeId Translation { get; protected set; }
        public EvalPrototype EvalCanDisplay { get; protected set; }
        public PowerTooltipSectionPrototype[] TooltipSections { get; protected set; }
    }

    public class TooltipSectionPrototype : Prototype
    {
        public PrototypeId Style { get; protected set; }
        public LocaleStringId Text { get; protected set; }
        public bool ShowOnlyIfPreviousSectionHasText { get; protected set; }
        public AssetId AlignToPreviousSection { get; protected set; }
        public AssetId Font { get; protected set; }
        public bool ShowOnlyIfNextSectionHasText { get; protected set; }
        public TooltipSectionType SectionType { get; protected set; }
        public int IconSize { get; protected set; }
        public bool ShowOnlyWithGamepad { get; protected set; }
        public Platforms Platforms { get; protected set; }
    }

    public class TooltipSectionIconLabeledPrototype : TooltipSectionPrototype
    {
        public AssetId IconPathDefault { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public bool ShowIconQualityLayer { get; protected set; }
    }

    public class TooltipSectionProceduralPrototype : TooltipSectionPrototype
    {
        public TooltipSectionKey Key { get; protected set; }
    }

    public class TooltipSectionGamepadIconPrototype : TooltipSectionPrototype
    {
        public GamepadInput Input { get; protected set; }
    }

    public class TooltipSectionItemAffixesPrototype : TooltipSectionPrototype
    {
        public PrototypeId[] IncludeCategories { get; protected set; }     // VectorPrototypeRefPtr AffixCategoryPrototype
        public PrototypeId[] ExcludeCategories { get; protected set; }     // VectorPrototypeRefPtr AffixCategoryPrototype
    }

    public class TooltipSectionBarPrototype : TooltipSectionPrototype
    {
        public bool DivideBarByRanks { get; protected set; }
        public EvalPrototype CurrentValueEval { get; protected set; }
        public EvalPrototype MaxValueEval { get; protected set; }
    }

    public class TooltipSectionLegendaryBarPrototype : TooltipSectionBarPrototype
    {
    }

    public class TooltipSectionPetTechBarPrototype : TooltipSectionBarPrototype
    {
        public AffixPosition Position { get; protected set; }
        public PrototypeId MaxedStyle { get; protected set; }
    }

    public class TooltipTemplatePrototype : Prototype
    {
        public TooltipSectionPrototype[] TooltipSectionList { get; protected set; }
    }
}
