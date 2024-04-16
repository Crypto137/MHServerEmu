using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities.Physics
{
    public class PhysicsManager
    {
        public int CurrentForceReadIndex => _currentForceReadWriteState ? 1 : 0;
        public int CurrentForceWriteIndex => _currentForceReadWriteState ? 0 : 1;

        private Game _game { get; }
        private List<ForceSystem> _pendingForceSystems { get; }
        private List<ForceSystem> _activeForceSystems { get; }
        private Queue<OverlapEvent> _overlapEvents { get; }
        private List<ulong> _entitiesPendingResolve { get; }
        private List<ulong> _entitiesResolving { get; }
        private int _physicsFrames;
        private bool _currentForceReadWriteState;

        public PhysicsManager(Game game)
        {
            _game = game;
            _pendingForceSystems = new();
            _activeForceSystems = new();
            _overlapEvents = new();
            _entitiesPendingResolve = new();
            _entitiesResolving = new();
            _currentForceReadWriteState = false;
            _physicsFrames = 1;
        }

        public void ResolveEntities()
        {
            if (_game == null || _entitiesResolving.Count > 0) return;

            _entitiesResolving.Clear();
            _entitiesResolving.AddRange(_entitiesPendingResolve);
            _entitiesPendingResolve.Clear();
            _physicsFrames++;

            SwapCurrentForceReadWriteIndices();
            ApplyForceSystems();
            PhysicsContext physicsContext = new();
            ResolveEntitiesAllowPenetration(physicsContext, _entitiesResolving);
            ResolveEntitiesOverlapState(physicsContext);

            _entitiesResolving.Clear();

            foreach (Region region in _game.RegionIterator())
                region.ClearCollidedEntities();
        }

        private void ResolveEntitiesOverlapState(PhysicsContext physicsContext)
        {
            throw new NotImplementedException();
        }

        private void ResolveEntitiesAllowPenetration(PhysicsContext physicsContext, List<ulong> entitiesResolving)
        {
            throw new NotImplementedException();
        }

        private void ApplyForceSystems()
        {
            _activeForceSystems.AddRange(_pendingForceSystems);
            _pendingForceSystems.Clear();

            for (int i = _activeForceSystems.Count - 1; i >= 0; i--)
                if (ApplyForceSystemCheckCompletion(_activeForceSystems[i]))
                    _activeForceSystems.RemoveAt(i);
        }

        private bool ApplyForceSystemCheckCompletion(ForceSystem forceSystem)
        {
            bool complete = true;

            foreach (var member in forceSystem.Members.Iterate())
            {
                if (member == null) continue;
                bool active = false;

                WorldEntity entity = _game.EntityManager.GetEntity<WorldEntity>(member.EntityId);
                if (entity != null && entity.IsInWorld())
                    if (entity.TestStatus(EntityStatus.Destroyed))
                    {
                        float time = Math.Min((float)_game.FixedTimeBetweenUpdates.TotalSeconds, member.Time);
                        float distance = member.Speed * time + member.Acceleration * time * time / 2;
                        Vector3 vector = member.Direction * distance;
                        bool moved = MoveEntity(entity, vector, MoveEntityFlags.SendToOwner | MoveEntityFlags.SendToClients | MoveEntityFlags.SweepCollide);

                        bool collision = Vector3.LengthSquared(member.Position + vector - entity.RegionLocation.Position) > 0.01f;

                        member.Position = entity.RegionLocation.Position;
                        member.Time -= time;
                        member.Speed += member.Acceleration * time;

                        active = collision == false && Segment.IsNearZero(member.Time) == false;
                        complete &= !active;

                        if (moved) entity.UpdateNavigationInfluence();
                    }

                if (active == false)
                    forceSystem.Members.Remove(member);
            }

            return complete;
        }

        private bool MoveEntity(WorldEntity entity, Vector3 vector, MoveEntityFlags moveFlags)
        {
            if (_game == null || entity == null || entity.IsInWorld() == false || entity.TestStatus(EntityStatus.Destroyed))
                return false;

            List<EntityCollision> entityCollisionList = new();
            bool moved = false;

            if (Vector3.IsNearZero(vector))
                CheckForExistingCollisions(entity, true);
            else
            {
                var locomotor = entity.Locomotor;
                if (locomotor == null)  return false;

                bool noMissile = locomotor.IsMissile() == false;
                bool allowMove = noMissile && moveFlags.HasFlag(MoveEntityFlags.AllowMove);
                bool sweepCollide = moveFlags.HasFlag(MoveEntityFlags.SweepCollide);
                bool sendToOwner = moveFlags.HasFlag(MoveEntityFlags.SendToOwner);
                bool sendToClients = moveFlags.HasFlag(MoveEntityFlags.SendToClients);
                bool allowSweep = noMissile;

                if (GetDesiredDestination(entity, vector, allowSweep, out Vector3 desiredDestination, out bool noSweep))
                {
                    Vector3 collidedDestination = Vector3.Zero;
                    if (sweepCollide)
                        moved = SweepEntityCollideToDestination(entity, desiredDestination, allowMove, collidedDestination, entityCollisionList);
                    else
                    {
                        collidedDestination = desiredDestination;
                        moved = true;
                    }

                    if (moved)
                    {
                        locomotor.MovementImpeded = noSweep || !Vector3.EpsilonSphereTest(collidedDestination, desiredDestination);

                        ChangePositionFlags changeFlags = ChangePositionFlags.PhysicsResolve;
                        changeFlags |= !sendToOwner ? ChangePositionFlags.NoSendToOwner : 0;
                        changeFlags |= !sendToClients ? ChangePositionFlags.NoSendToClients : 0;

                        entity.ChangeRegionPosition(collidedDestination, null, changeFlags);
                    }

                    if (sweepCollide)
                        HandleEntityCollisions(entityCollisionList, entity, true);

                    if (noSweep && entity.TestStatus(EntityStatus.Destroyed) == false && entity.IsInWorld())
                        NotifyEntityCollision(entity, null, collidedDestination);
                }
            }

            return moved;
        }

        private bool SweepEntityCollideToDestination(WorldEntity entity, Vector3 desiredDestination, bool allowMove, Vector3 collidedDestination, List<EntityCollision> entityCollisionList)
        {
            throw new NotImplementedException();
        }

        private bool GetDesiredDestination(WorldEntity entity, Vector3 vector, bool allowSweep, out Vector3 desiredDestination, out bool noSweep)
        {
            throw new NotImplementedException();
        }

        private void NotifyEntityCollision(WorldEntity who, WorldEntity other, Vector3 collidedDestination)
        {
            throw new NotImplementedException();
        }

        private void HandleEntityCollisions(List<EntityCollision> entityCollisionList, WorldEntity entity, bool repulsionForces)
        {
            throw new NotImplementedException();
        }

        private void CheckForExistingCollisions(WorldEntity entity, bool v)
        {
            throw new NotImplementedException();
        }

        private void SwapCurrentForceReadWriteIndices()
        {
            _currentForceReadWriteState = !_currentForceReadWriteState;
        }
    }

    [Flags]
    public enum MoveEntityFlags
    {
        SendToOwner = 1 << 0,
        SweepCollide = 1 << 2,
        AllowMove = 1 << 3,
        SendToClients = 1 << 4,
    }

    public class PhysicsContext
    {
        public List<WorldEntity> AttachedEntities;
    }

    public class OverlapEvent
    {

    }

    public class EntityCollision
    {

    }
}
