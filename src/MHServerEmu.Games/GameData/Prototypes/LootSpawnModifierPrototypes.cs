namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootLocationModifierPrototype : Prototype
    {
    }

    public class LootSearchRadiusPrototype : LootLocationModifierPrototype
    {
        public float MinRadius { get; protected set; }
        public float MaxRadius { get; protected set; }
    }

    public class LootBoundsOverridePrototype : LootLocationModifierPrototype
    {
        public float Radius { get; protected set; }
    }

    public class LootLocationOffsetPrototype : LootLocationModifierPrototype
    {
        public float Offset { get; protected set; }
    }

    public class DropInPlacePrototype : LootLocationModifierPrototype
    {
        public bool Check { get; protected set; }
    }
}
