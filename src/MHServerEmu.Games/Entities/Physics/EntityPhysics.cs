using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities.Physics
{
    public class EntityPhysics
    {
        private InlineArray2<Vector3> _externalForces;
        private InlineArray2<Vector3> _repulsionForces;
        private InlineArray2<bool> _hasExternalForces;

        public WorldEntity Entity { get; private set; } = null;
        public uint RegisteredPhysicsFrameId { get; set; } = 0;
        public int CollisionId { get; private set; } = -1;
        public SortedDictionary<ulong, OverlapEntityEntry> OverlappedEntities { get; } = new();
        public SortedVector<ulong> AttachedEntities { get; private set; }

        public EntityPhysics()
        {
        }

        public void Initialize(WorldEntity entity)
        {
            Entity = entity;
        }

        public bool HasExternalForces()
        {
            return _hasExternalForces[GetCurrentForceReadIndex()];
        }

        public Vector3 GetExternalForces()
        {
            return _externalForces[GetCurrentForceReadIndex()];
        }

        public Vector3 GetRepulsionForces()
        {
            return _repulsionForces[GetCurrentForceReadIndex()];
        }

        private int GetCurrentForceReadIndex()
        {
            PhysicsManager physicsMgr = GetPhysicsManager();
            if (!Verify.IsNotNull(physicsMgr)) return 0;
            return physicsMgr.CurrentForceReadIndex;
        }

        private PhysicsManager GetPhysicsManager()
        {
            if (!Verify.IsNotNull(Entity)) return null;

            Game game = Entity.Game;
            if (!Verify.IsNotNull(game)) return null;

            EntityManager entityMan = game.EntityManager;   // variable name from the client
            if (!Verify.IsNotNull(entityMan)) return null;

            return entityMan.PhysicsManager;
        }

        private int GetCurrentForceWriteIndex()
        {
            PhysicsManager physicsMgr = GetPhysicsManager();
            if (!Verify.IsNotNull(physicsMgr)) return 0;
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
            int index = GetCurrentForceReadIndex();

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
            if (!Verify.IsNotNull(Entity)) return;

            if (Entity.Locomotor != null)
            {
                if (!Verify.IsTrue(Vector3.IsFinite(force))) return;
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
            PhysicsManager physicsMgr = GetPhysicsManager();
            if (!Verify.IsNotNull(physicsMgr)) return;

            physicsMgr.AddKnockbackForce(Entity, position, time, speed, acceleration);
        }

        private void ApplyForce(in Vector3 force, bool external)
        {
            if (!Verify.IsTrue(Vector3.IsFinite(force))) return;

            if (Vector3.IsNearZero(force))
                return;

            if (!Verify.IsTrue(Entity.IsInWorld)) return;
            if (!Verify.IsNotNull(Entity.Locomotor)) return;

            int index = GetCurrentForceWriteIndex();
            _hasExternalForces[index] |= external;
            _externalForces[index] += force;
            Entity.RegisterForPendingPhysicsResolve();
        }

        public void DetachAllChildren()
        {
            if (!Verify.IsNotNull(Entity)) return;

            EntityManager entityManager = Entity.Game.EntityManager;

            using var attachedEntitiesHandle = ListPool<ulong>.Instance.Get(out List<ulong> attachedEntities);
            if (GetAttachedEntities(attachedEntities))
            {
                foreach (ulong entityId in attachedEntities)
                {
                    WorldEntity childEntity = entityManager.GetEntity<WorldEntity>(entityId);
                    if (childEntity != null)
                        DetachChild(childEntity.Physics);
                }
            }

            AttachedEntities?.Clear();
        }

        public void AttachChild(EntityPhysics childEntityPhysics)
        {
            if (!Verify.IsNotNull(Entity)) return;
            if (!Verify.IsNotNull(childEntityPhysics.Entity)) return;
            if (!Verify.IsTrue(Entity.IsInWorld && Entity.TestStatus(EntityStatus.ExitingWorld) == false)) return;
            if (!Verify.IsTrue(childEntityPhysics.Entity.IsInWorld && childEntityPhysics.Entity.TestStatus(EntityStatus.ExitingWorld) == false)) return;

            AttachedEntities ??= new();
            AttachedEntities.Add(childEntityPhysics.Entity.Id);
        }

        public void DetachChild(EntityPhysics childEntityPhysics)
        {
            if (!Verify.IsNotNull(AttachedEntities)) return;
            if (!Verify.IsNotNull(Entity)) return;
            if (!Verify.IsNotNull(childEntityPhysics.Entity)) return;

            AttachedEntities.Remove(childEntityPhysics.Entity.Id);
        }

        public void AcquireCollisionId()
        {
            if (!Verify.IsTrue(CollisionId == -1)) return;

            Region region = Entity?.Region;
            if (region != null)
                CollisionId = region.AcquireCollisionId();
        }

        public void ReleaseCollisionId()
        {
            Region region = Entity?.Region;
            if (region != null && CollisionId != -1)
            {
                region.ReleaseCollisionId(CollisionId);
                CollisionId = -1;
            }
        }

        public bool IsOverlappingEntity(ulong entityId)
        {
            if (!Verify.IsNotNull(Entity)) return false;
            if (!Verify.IsNotNull(Entity.Game)) return false;

            if (OverlappedEntities.TryGetValue(entityId, out OverlapEntityEntry overlappedEntity) == false)
                return false;

            return overlappedEntity.Overlapped;
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
