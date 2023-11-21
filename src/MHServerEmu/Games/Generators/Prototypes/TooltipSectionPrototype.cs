using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class PowerTooltipSectionPrototype : Prototype
    {
        public ulong Description;
        public ulong Value;
        public PowerTooltipSectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerTooltipSectionPrototype), proto); }
    }

    public class PowerTooltipSectionOverridePrototype : PowerTooltipSectionPrototype
    {
        public ulong DescTokenSourcePrefixOverride;
        public ulong ValueTokenSourcePrefixOverride;
        public PowerTooltipSectionOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerTooltipSectionOverridePrototype), proto); }
    }

    public class PowerTooltipEntryPrototype : Prototype
    {
        public ulong TokenSourcePrefix;
        public ulong Translation;
        public EvalPrototype EvalCanDisplay;
        public PowerTooltipSectionPrototype[] TooltipSections;
        public PowerTooltipEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerTooltipEntryPrototype), proto); }
    }

    public class TooltipSectionPrototype : Prototype
    {
        public ulong Style;
        public ulong Text;
        public bool ShowOnlyIfPreviousSectionHasText;
        public ulong AlignToPreviousSection;
        public ulong Font;
        public bool ShowOnlyIfNextSectionHasText;
        public TooltipSectionType SectionType;
        public int IconSize;
        public bool ShowOnlyWithGamepad;
        public Platforms Platforms;
        public TooltipSectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionPrototype), proto); }
    }
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
    public class TooltipSectionIconLabeledPrototype : TooltipSectionPrototype
    {
        public ulong IconPathDefault;
        public AffixPosition Position;
        public bool ShowIconQualityLayer;
        public TooltipSectionIconLabeledPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionIconLabeledPrototype), proto); }
    }

    public class TooltipSectionProceduralPrototype : TooltipSectionPrototype
    {
        public TooltipSectionKey Key;
        public TooltipSectionProceduralPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionProceduralPrototype), proto); }
    }
    public enum TooltipSectionKey
    {
        Invalid = 0,
        PowerStatsCurrentRank = 1,
    }
    public class TooltipSectionGamepadIconPrototype : TooltipSectionPrototype
    {
        public GamepadInput Input;
        public TooltipSectionGamepadIconPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionGamepadIconPrototype), proto); }
    }

    public class TooltipSectionItemAffixesPrototype : TooltipSectionPrototype
    {
        public AffixCategoryPrototype IncludeCategories;
        public AffixCategoryPrototype ExcludeCategories;
        public TooltipSectionItemAffixesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionItemAffixesPrototype), proto); }
    }

    public class TooltipSectionBarPrototype : TooltipSectionPrototype
    {
        public bool DivideBarByRanks;
        public EvalPrototype CurrentValueEval;
        public EvalPrototype MaxValueEval;
        public TooltipSectionBarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionBarPrototype), proto); }
    }

    public class TooltipSectionLegendaryBarPrototype : TooltipSectionBarPrototype
    {
        public TooltipSectionLegendaryBarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionLegendaryBarPrototype), proto); }
    }

    public class TooltipSectionPetTechBarPrototype : TooltipSectionBarPrototype
    {
        public AffixPosition Position;
        public ulong MaxedStyle;
        public TooltipSectionPetTechBarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipSectionPetTechBarPrototype), proto); }
    }

    public class TooltipTemplatePrototype : Prototype
    {
        public TooltipSectionPrototype[] TooltipSectionList;
        public TooltipTemplatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TooltipTemplatePrototype), proto); }
    }
}
