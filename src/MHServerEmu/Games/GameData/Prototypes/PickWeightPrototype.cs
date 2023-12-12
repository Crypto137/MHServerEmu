namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PickMethodPrototype : Prototype
    {
        public PickMethodPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PickMethodPrototype), proto); }
    }

    public class PickAllPrototype : PickMethodPrototype
    {
        public PickAllPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PickAllPrototype), proto); }
    }

    public class PickWeightPrototype : PickMethodPrototype
    {
        public short Choices;
        public PickWeightPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PickWeightPrototype), proto); }
    }
}
