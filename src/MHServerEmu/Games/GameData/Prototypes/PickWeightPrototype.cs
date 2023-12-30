namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PickMethodPrototype : Prototype
    {
    }

    public class PickAllPrototype : PickMethodPrototype
    {
    }

    public class PickWeightPrototype : PickMethodPrototype
    {
        public short Choices { get; protected set; }
    }
}
