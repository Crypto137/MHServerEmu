namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootActionPrototype : LootNodePrototype
    {
        public LootNodePrototype Target { get; protected set; }
    }

    public class LootActionFirstTimePrototype : LootActionPrototype
    {
        public bool FirstTime { get; protected set; }
    }

    public class LootActionLoopOverAvatarsPrototype : LootActionPrototype
    {
    }
}
