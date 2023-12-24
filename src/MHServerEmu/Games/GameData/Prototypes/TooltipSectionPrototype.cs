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
        public ulong Description { get; private set; }
        public ulong Value { get; private set; }
    }

    public class PowerTooltipSectionOverridePrototype : PowerTooltipSectionPrototype
    {
        public ulong DescTokenSourcePrefixOverride { get; private set; }
        public ulong ValueTokenSourcePrefixOverride { get; private set; }
    }

    public class PowerTooltipEntryPrototype : Prototype
    {
        public ulong TokenSourcePrefix { get; private set; }
        public ulong Translation { get; private set; }
        public EvalPrototype EvalCanDisplay { get; private set; }
        public PowerTooltipSectionPrototype[] TooltipSections { get; private set; }
    }

    public class TooltipSectionPrototype : Prototype
    {
        public ulong Style { get; private set; }
        public ulong Text { get; private set; }
        public bool ShowOnlyIfPreviousSectionHasText { get; private set; }
        public ulong AlignToPreviousSection { get; private set; }
        public ulong Font { get; private set; }
        public bool ShowOnlyIfNextSectionHasText { get; private set; }
        public TooltipSectionType SectionType { get; private set; }
        public int IconSize { get; private set; }
        public bool ShowOnlyWithGamepad { get; private set; }
        public Platforms Platforms { get; private set; }
    }

    public class TooltipSectionIconLabeledPrototype : TooltipSectionPrototype
    {
        public ulong IconPathDefault { get; private set; }
        public AffixPosition Position { get; private set; }
        public bool ShowIconQualityLayer { get; private set; }
    }

    public class TooltipSectionProceduralPrototype : TooltipSectionPrototype
    {
        public TooltipSectionKey Key { get; private set; }
    }

    public class TooltipSectionGamepadIconPrototype : TooltipSectionPrototype
    {
        public GamepadInput Input { get; private set; }
    }

    public class TooltipSectionItemAffixesPrototype : TooltipSectionPrototype
    {
        public AffixCategoryPrototype IncludeCategories { get; private set; }
        public AffixCategoryPrototype ExcludeCategories { get; private set; }
    }

    public class TooltipSectionBarPrototype : TooltipSectionPrototype
    {
        public bool DivideBarByRanks { get; private set; }
        public EvalPrototype CurrentValueEval { get; private set; }
        public EvalPrototype MaxValueEval { get; private set; }
    }

    public class TooltipSectionLegendaryBarPrototype : TooltipSectionBarPrototype
    {
    }

    public class TooltipSectionPetTechBarPrototype : TooltipSectionBarPrototype
    {
        public AffixPosition Position { get; private set; }
        public ulong MaxedStyle { get; private set; }
    }

    public class TooltipTemplatePrototype : Prototype
    {
        public TooltipSectionPrototype[] TooltipSectionList { get; private set; }
    }
}
