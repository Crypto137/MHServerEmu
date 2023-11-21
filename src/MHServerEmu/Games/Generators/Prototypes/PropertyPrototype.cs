using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class PropertyPrototype : Prototype
    {
        public PropertyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropertyPrototype), proto); }
    }

    public class PropertyEntryPrototype : Prototype
    {
        public PropertyEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropertyEntryPrototype), proto); }
    }

    public class PropertyPickInRangeEntryPrototype : PropertyEntryPrototype
    {
        public ulong Prop;
        public EvalPrototype ValueMax;
        public EvalPrototype ValueMin;
        public bool RollAsInteger;
        public ulong TooltipOverrideText;
        public PropertyPickInRangeEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropertyPickInRangeEntryPrototype), proto); }
    }

    public class PropertySetEntryPrototype : PropertyEntryPrototype
    {
        public ulong Prop;
        public ulong TooltipOverrideText;
        public EvalPrototype Value;
        public PropertySetEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PropertySetEntryPrototype), proto); }
    }
}
