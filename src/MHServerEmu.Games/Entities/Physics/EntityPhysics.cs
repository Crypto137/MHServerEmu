using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Physics
{
    public class EntityPhysics
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public WorldEntity Entity;
        public uint RegisteredPhysicsFrameId { get; set; }
        public int CollisionId { get; private set; }
        public SortedDictionary<ulong, OverlapEntityEntry> OverlappedEntities { get; private set; }
        public SortedVector<ulong> AttachedEntities { get; private set; }

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
            for (int i = 0;  i < 2; i++)
            {
                _externalForces[i] = Vector3.Zero;
                _repulsionForces[i] = Vector3.Zero;
            }
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
            return Entity.Bounds.CollisionType == BoundsCollisionType.Overlapping
                || (Entity.Locomotor != null && Entity.Locomotor.HasLocomotionNoEntityCollide)
                || Entity.IsInKnockback;
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

        public bool GetAttachedEntities(List<ulong> attachedEntities)
        {
            if (AttachedEntities == null)
                return false;

            // SortedVector implements ICollection, so it should be more efficient to AddRange instead of iterating
            attachedEntities.AddRange(AttachedEntities);
            return attachedEntities.Count > 0;
        }

        public bool GetOverlappingEntities(List<ulong> overlappingEntities)
        {
            foreach (var kvp in OverlappedEntities)
            {
                if (kvp.Value.Overlapped)
                    overlappingEntities.Add(kvp.Key);
            }

            return overlappingEntities.Count > 0;
        }

        public void AddRepulsionForce(in Vector3 force)
        {
            if (Entity == null) return;
            if (Entity.Locomotor != null)
            {
                if (Vector3.IsFinite(force) == false) return;
                _repulsionForces[GetCurrentForceWriteIndex()] += force;
                Entity.RegisterForPendingPhysicsResolve();
            }
        }

        public bool HasAttachedEntities()
        {
            return AttachedEntities != null && AttachedEntities.Count > 0;
        }

        public void ApplyInternalForce(in Vector3 force)
        {
            ApplyForce(force, false);
        }

        public void ApplyKnockbackForce(in Vector3 position, float time, float speed, float acceleration)
        {
            // Logger.Debug($"ApplyKnockbackForce(): entity=[{Entity.PrototypeName}] source=[{position}], time={time}, acceleration={acceleration}");

            var physicsMgr = GetPhysicsManager();
            if (physicsMgr == null || Entity.IsInWorld == false) return;
            if (Segment.IsNearZero(speed) && Segment.IsNearZero(acceleration)) return;
            if (time <= 0) return;

            physicsMgr.AddKnockbackForce(Entity, position, time, speed, acceleration);
        }

        private void ApplyForce(in Vector3 force, bool external)
        {
            if (!Vector3.IsFinite(force) || Vector3.IsNearZero(force) || !Entity.IsInWorld || Entity.Locomotor == null)
                return;
            var index = GetCurrentForceWriteIndex();
            _hasExternalForces[index] |= external;
            _externalForces[index] += force;
            Entity.RegisterForPendingPhysicsResolve();
        }

        public void DetachAllChildren()
        {
            if (Entity == null) return;
            var manager = Entity.Game.EntityManager;

            List<ulong> attachedEntities = ListPool<ulong>.Instance.Get();
            if (GetAttachedEntities(attachedEntities))
                foreach (var entityId in attachedEntities)
                {
                    var childEntity = manager.GetEntity<WorldEntity>(entityId);
                    if (childEntity != null)
                        DetachChild(childEntity.Physics);
                }
            AttachedEntities?.Clear();
            ListPool<ulong>.Instance.Return(attachedEntities);
        }

        public void AttachChild(EntityPhysics physics)
        {
            if (Entity == null && physics.Entity == null) return;
            if (Entity.IsInWorld == false || Entity.TestStatus(EntityStatus.ExitingWorld)) return;
            if (physics.Entity.IsInWorld == false || physics.Entity.TestStatus(EntityStatus.ExitingWorld)) return;

            AttachedEntities ??= new();
            AttachedEntities.Add(physics.Entity.Id);
        }

        public void DetachChild(EntityPhysics physics)
        {
            if (AttachedEntities != null && Entity != null && physics.Entity != null)
                AttachedEntities.Remove(physics.Entity.Id);
        }

        public void AcquireCollisionId()
        {
            if (CollisionId != -1) return;
            var region = Entity?.Region;
            if (region != null)
                CollisionId =   region.AcquireCollisionId();
        }

        public void ReleaseCollisionId()
        {
            var region = Entity?.Region;
            if (region != null && CollisionId != -1)
            {
                region.ReleaseCollisionId(CollisionId);
                CollisionId = -1;
            }
        }

        public bool IsOverlappingEntity(ulong entityId)
        {
            if (Entity == null || Entity.Game == null) return false;
            if (OverlappedEntities.TryGetValue(entityId, out var overlappedEntity))
                return overlappedEntity.Overlapped;
            return false;
        }
    }

    public readonly struct OverlapEntityEntry
    {
        public readonly bool Overlapped;
        public readonly uint Frame;

        public OverlapEntityEntry(bool overlapped = false, uint frame = 0)
        {
            Overlapped = overlapped;
            Frame = frame;
        }
    }
}
