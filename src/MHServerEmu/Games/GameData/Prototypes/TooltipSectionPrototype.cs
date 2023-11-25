namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

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

    public enum TooltipSectionKey
    {
        Invalid = 0,
        PowerStatsCurrentRank = 1,
    }

    #endregion

    public class PowerTooltipSectionPrototype : Prototype
    {
        public ulong Description { get; set; }
        public ulong Value { get; set; }
    }

    public class PowerTooltipSectionOverridePrototype : PowerTooltipSectionPrototype
    {
        public ulong DescTokenSourcePrefixOverride { get; set; }
        public ulong ValueTokenSourcePrefixOverride { get; set; }
    }

    public class PowerTooltipEntryPrototype : Prototype
    {
        public ulong TokenSourcePrefix { get; set; }
        public ulong Translation { get; set; }
        public EvalPrototype EvalCanDisplay { get; set; }
        public PowerTooltipSectionPrototype[] TooltipSections { get; set; }
    }

    public class TooltipSectionPrototype : Prototype
    {
        public ulong Style { get; set; }
        public ulong Text { get; set; }
        public bool ShowOnlyIfPreviousSectionHasText { get; set; }
        public ulong AlignToPreviousSection { get; set; }
        public ulong Font { get; set; }
        public bool ShowOnlyIfNextSectionHasText { get; set; }
        public TooltipSectionType SectionType { get; set; }
        public int IconSize { get; set; }
        public bool ShowOnlyWithGamepad { get; set; }
        public Platforms Platforms { get; set; }
    }

    public class TooltipSectionIconLabeledPrototype : TooltipSectionPrototype
    {
        public ulong IconPathDefault { get; set; }
        public AffixPosition Position { get; set; }
        public bool ShowIconQualityLayer { get; set; }
    }

    public class TooltipSectionProceduralPrototype : TooltipSectionPrototype
    {
        public TooltipSectionKey Key { get; set; }
    }

    public class TooltipSectionGamepadIconPrototype : TooltipSectionPrototype
    {
        public GamepadInput Input { get; set; }
    }

    public class TooltipSectionItemAffixesPrototype : TooltipSectionPrototype
    {
        public AffixCategoryPrototype IncludeCategories { get; set; }
        public AffixCategoryPrototype ExcludeCategories { get; set; }
    }

    public class TooltipSectionBarPrototype : TooltipSectionPrototype
    {
        public bool DivideBarByRanks { get; set; }
        public EvalPrototype CurrentValueEval { get; set; }
        public EvalPrototype MaxValueEval { get; set; }
    }

    public class TooltipSectionLegendaryBarPrototype : TooltipSectionBarPrototype
    {
    }

    public class TooltipSectionPetTechBarPrototype : TooltipSectionBarPrototype
    {
        public AffixPosition Position { get; set; }
        public ulong MaxedStyle { get; set; }
    }

    public class TooltipTemplatePrototype : Prototype
    {
        public TooltipSectionPrototype[] TooltipSectionList { get; set; }
    }
}
