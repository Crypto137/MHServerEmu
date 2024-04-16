

namespace MHServerEmu.Games.Entities.Physics
{
    public class EntityPhysics
    {
        public WorldEntity Entity;
        public int RegisteredPhysicsFrameId { get; set; }
        public int CollisionId { get; private set; }
        public Dictionary<ulong, OverlapEntityEntry> OverlappedEntities { get; private set; }
        public SortedSet<ulong> AttachedEntities { get; private set; }
    }

    public class OverlapEntityEntry
    {

    }
}
