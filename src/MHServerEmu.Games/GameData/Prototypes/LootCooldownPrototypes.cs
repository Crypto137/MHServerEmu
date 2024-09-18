using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootCooldownPrototype : Prototype
    {
        public PrototypeId Channel { get; protected set; }

        [DoNotCopy]
        public virtual PrototypeId CooldownRef { get => PrototypeId.Invalid; }
    }

    public class LootCooldownEntityPrototype : LootCooldownPrototype
    {
        public PrototypeId Entity { get; protected set; }

        [DoNotCopy]
        public override PrototypeId CooldownRef { get => Entity; }
    }

    public class LootCooldownVendorTypePrototype : LootCooldownPrototype
    {
        public PrototypeId VendorType { get; protected set; }

        [DoNotCopy]
        public override PrototypeId CooldownRef { get => VendorType; }
    }

    public class LootCooldownHierarchyPrototype : Prototype
    {
        public PrototypeId Entity { get; protected set; }
        public PrototypeId[] LocksOut { get; protected set; }
    }

    public class LootCooldownRolloverTimeEntryPrototype : Prototype
    {
        public float WallClockTime24Hr { get; protected set; }
        public Weekday WallClockTimeDay { get; protected set; }
    }

    public class LootCooldownChannelPrototype : Prototype
    {
    }

    public class LootCooldownChannelRollOverPrototype : LootCooldownChannelPrototype
    {
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; protected set; }
    }

    public class LootCooldownChannelTimePrototype : LootCooldownChannelPrototype
    {
        public float DurationMinutes { get; protected set; }
    }

    public class LootCooldownChannelCountPrototype : LootCooldownChannelPrototype
    {
        public int MaxDrops { get; protected set; }
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; protected set; }
    }
}
