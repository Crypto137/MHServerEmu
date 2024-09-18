namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootLocationNodePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public LootLocationModifierPrototype[] Modifiers { get; protected set; }
    }

    public class LootLocationTablePrototype : LootLocationNodePrototype
    {
        public LootLocationNodePrototype[] Choices { get; protected set; }
    }
}
