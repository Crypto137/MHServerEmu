using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyPrototype : Prototype
    {
    }

    public class PropertyEntryPrototype : Prototype
    {
    }

    public class PropertyPickInRangeEntryPrototype : PropertyEntryPrototype
    {
        public PropertyId Prop { get; protected set; }
        public EvalPrototype ValueMax { get; protected set; }
        public EvalPrototype ValueMin { get; protected set; }
        public bool RollAsInteger { get; protected set; }
        public LocaleStringId TooltipOverrideText { get; protected set; }
    }

    public class PropertySetEntryPrototype : PropertyEntryPrototype
    {
        public PropertyId Prop { get; protected set; }
        public LocaleStringId TooltipOverrideText { get; protected set; }
        public EvalPrototype Value { get; protected set; }
    }
}
