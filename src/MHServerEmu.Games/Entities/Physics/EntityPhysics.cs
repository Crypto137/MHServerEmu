using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Physics
{
    public class EntityPhysics
    {
        public WorldEntity Entity;
        public int RegisteredPhysicsFrameId { get; set; }
        public int CollisionId { get; private set; }
        public SortedDictionary<ulong, OverlapEntityEntry> OverlappedEntities { get; private set; }
        public SortedSet<ulong> AttachedEntities { get; private set; }

        private readonly Vector3[] _externalForces;
        private readonly Vector3[] _repulsionForces;
        private readonly bool[] _hasExternalForces;

        public EntityPhysics()
        {
            Entity = null;
            RegisteredPhysicsFrameId = 0;
            CollisionId = -1;
            OverlappedEntities = new();
            AttachedEntities = null;
            _externalForces = new Vector3[2];
            _repulsionForces = new Vector3[2];
            _hasExternalForces = new bool[2];
        }

        public void Initialize(WorldEntity entity)
        {
            Entity = entity;
        }

        public bool HasExternalForces() => _hasExternalForces[GetCurrentForceReadIndex()];
        public Vector3 GetExternalForces() => _externalForces[GetCurrentForceReadIndex()];
        public Vector3 GetRepulsionForces() => _repulsionForces[GetCurrentForceReadIndex()];

        private int GetCurrentForceReadIndex()
        {
            var physicsMgr = GetPhysicsManager();
            if (physicsMgr == null) return 0;
            return physicsMgr.CurrentForceReadIndex;
        }

        private PhysicsManager GetPhysicsManager() => Entity?.Game?.EntityManager?.PhysicsManager;

        private int GetCurrentForceWriteIndex()
        {
            var physicsMgr = GetPhysicsManager();
            if (physicsMgr == null) return 0;
            return physicsMgr.CurrentForceWriteIndex;
        }

        public bool IsTrackingOverlap()
        {
            if (Entity == null) return false;
            return Entity.Bounds.CollisionType == BoundsCollisionType.Overlapping ||
            (Entity.Locomotor != null && Entity.Locomotor.HasLocomotionNoEntityCollide) ||
            Entity.IsInKnockback;
        }

        public void OnPhysicsUpdateFinished()
        {
            var index = GetCurrentForceReadIndex();

            if (Entity.IsInWorld && Vector3.IsNearZero(_repulsionForces[index]) == false)
                Entity.RegisterForPendingPhysicsResolve();

            _externalForces[index] = Vector3.Zero;
            _repulsionForces[index] = Vector3.Zero;
            _hasExternalForces[index] = false;
        }

        public bool GetAttachedEntities(out List<ulong> attachedEntities)
        {            
            if (AttachedEntities == null) {
                attachedEntities = null;
                return false; 
            }
            attachedEntities = AttachedEntities.ToList();
            return attachedEntities.Count > 0;
        }

        public void AddRepulsionForce(Vector3 force)
        {
            if (Entity == null) return;
            if (Entity.Locomotor != null)
            {
                if (Vector3.IsFinite(force) == false) return;
                _repulsionForces[GetCurrentForceWriteIndex()] += force;
                Entity.RegisterForPendingPhysicsResolve();
            }
        }

    }

    public class OverlapEntityEntry
    {
        public bool Overlapped;
        public int Frame;

        public OverlapEntityEntry()
        {
            Overlapped = false;
            Frame = 0;
        }
    }
}
