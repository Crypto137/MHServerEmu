using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities.Physics
{
    public class PhysicsManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public int CurrentForceReadIndex => _currentForceReadWriteState ? 1 : 0;
        public int CurrentForceWriteIndex => _currentForceReadWriteState ? 0 : 1;

        private Game _game;
        private readonly List<ForceSystem> _pendingForceSystems;
        private readonly List<ForceSystem> _activeForceSystems;
        private readonly Queue<OverlapEvent> _overlapEvents;
        private readonly List<ulong> _entitiesPendingResolve;
        private readonly List<ulong> _entitiesResolving;
        private bool _currentForceReadWriteState;
        private uint _physicsFrames;

        public PhysicsManager()
        {
            _pendingForceSystems = new();
            _activeForceSystems = new();
            _overlapEvents = new();
            _entitiesPendingResolve = new();
            _entitiesResolving = new();
            _currentForceReadWriteState = false;
            _physicsFrames = 1;
        }

        public bool Initialize(Game game)
        {
            _game = game;
            // TODO?: m_forceSystemMemberPool.Initialize( sizeof(ForceSystemMember), 64, 1 )
            return true;
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

            foreach (Region region in _game.RegionManager)
                region.ClearCollidedEntities();
        }

        private void ResolveEntitiesOverlapState(PhysicsContext physicsContext)
        {
            var entityManager = _game.EntityManager;

            foreach (var entityId in _entitiesResolving)
                ResolveEntitiesOverlapState(entityManager.GetEntity<WorldEntity>(entityId), _overlapEvents);

            foreach (var worldEntity in physicsContext.AttachedEntities)
                ResolveEntitiesOverlapState(worldEntity, _overlapEvents);

            while (_overlapEvents.Count > 0)
            {
                OverlapEvent overlapEvent = _overlapEvents.Dequeue();
                ResolveOverlapEvent(overlapEvent.Type, overlapEvent.Who, overlapEvent.Whom, overlapEvent.WhoPos, overlapEvent.WhomPos);
                ResolveOverlapEvent(overlapEvent.Type, overlapEvent.Whom, overlapEvent.Who, overlapEvent.WhomPos, overlapEvent.WhoPos);
            }
        }

        private void ResolveEntitiesOverlapState(WorldEntity worldEntity, Queue<OverlapEvent> overlapEvents)
        {
            if (worldEntity != null && worldEntity.IsInWorld)
            {
                var entityPhysics = worldEntity.Physics;
                var manager = _game.EntityManager;
                foreach (var overlappedEntry in entityPhysics.OverlappedEntities.ToArray())
                    if (overlappedEntry.Value.Frame != _physicsFrames)
                    {
                        var overlappedEntity = manager.GetEntity<WorldEntity>(overlappedEntry.Key);
                        if (overlappedEntity != null)
                            overlapEvents.Enqueue(new (OverlapEventType.Remove, worldEntity, overlappedEntity));
                        else
                            entityPhysics.OverlappedEntities.Remove(overlappedEntry.Key);
                    }
            }
        }

        private void ResolveEntitiesAllowPenetration(PhysicsContext physicsContext, List<ulong> entitiesResolving)
        {
            if (_game == null) return;
            RegionManager regionManager = _game.RegionManager;
            if (regionManager == null) return;
            var entityManager = _game.EntityManager;

            foreach (var entityId in entitiesResolving)
            {
                var worldEntity = entityManager.GetEntity<WorldEntity>(entityId);
                if (worldEntity == null || worldEntity.TestStatus(EntityStatus.Destroyed) || worldEntity.IsInWorld == false)  continue;

                var entityPhysics = worldEntity.Physics;
                Vector3 externalForces = entityPhysics.GetExternalForces();
                Vector3 repulsionForces = entityPhysics.GetRepulsionForces();

                MoveEntityFlags moveFlags = 0;
                if (entityPhysics.HasExternalForces())
                    moveFlags |= MoveEntityFlags.SendToOwner | MoveEntityFlags.SendToClients;
                if (worldEntity.IsMovementAuthoritative)
                    moveFlags |= MoveEntityFlags.SendToOwner;
                if (_game.AdminCommandManager.TestAdminFlag(AdminFlags.LocomotionSync) == false)
                    moveFlags |= MoveEntityFlags.SendToClients;

                if (Vector3.IsNearZero(repulsionForces, 0.5f) == false)
                {
                    float repulsionForcesLength = Vector3.Length(repulsionForces);
                    float collideRadius = worldEntity.EntityCollideBounds.Radius;
                    if (repulsionForcesLength > collideRadius)
                        repulsionForces *= (collideRadius / repulsionForcesLength);
                    externalForces += repulsionForces;
                }

                moveFlags |= MoveEntityFlags.SweepCollide | MoveEntityFlags.Sliding;
                MoveEntity(worldEntity, externalForces, moveFlags);

                entityPhysics.OnPhysicsUpdateFinished();
                UpdateAttachedEntityPositions(physicsContext, worldEntity);
            }
        }

        private void UpdateAttachedEntityPositions(PhysicsContext physicsContext, WorldEntity parentEntity)
        {
            if (parentEntity == null) return;

            List<ulong> attachedEntities = ListPool<ulong>.Instance.Get();
            if (parentEntity.Physics.GetAttachedEntities(attachedEntities))
            {
                Vector3 parentEntityPosition = parentEntity.RegionLocation.Position;
                Orientation parentEntityOrientation = parentEntity.Orientation;

                var entityManager = _game.EntityManager;

                foreach (var attachedEntityId in attachedEntities)
                {
                    var attachedEntity = entityManager.GetEntity<WorldEntity>(attachedEntityId);
                    if (attachedEntity != null && attachedEntity.IsInWorld)
                    {
                        var worldEntityProto = attachedEntity.WorldEntityPrototype;
                        if (worldEntityProto != null)
                        {
                            attachedEntity.ChangeRegionPosition(
                                parentEntityPosition,
                                worldEntityProto.UpdateOrientationWithParent ? parentEntityOrientation : null,
                                ChangePositionFlags.PhysicsResolve);
                            CheckForExistingCollisions(attachedEntity, false);
                            physicsContext.AttachedEntities.Add(attachedEntity);
                        }
                    }
                }
            }
            ListPool<ulong>.Instance.Return(attachedEntities);
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

            EntityManager entityManager = _game.EntityManager; 

            foreach (var member in forceSystem.Members.Iterate())
            {
                if (member == null) continue;
                bool active = false;

                WorldEntity entity = entityManager.GetEntity<WorldEntity>(member.EntityId);
                if (entity != null && entity.IsInWorld)
                    if (entity.TestStatus(EntityStatus.Destroyed) == false)
                    {
                        float deltaTime = Math.Min((float)_game.FixedTimeBetweenUpdates.TotalSeconds, member.Time);
                        float distance = member.Speed * deltaTime + member.Acceleration * deltaTime * deltaTime / 2;
                        Vector3 vector = member.Direction * distance;
                        bool moved = MoveEntity(entity, vector, MoveEntityFlags.SendToOwner | MoveEntityFlags.SendToClients | MoveEntityFlags.SweepCollide);

                        bool collision = Vector3.LengthSquared(member.Position + vector - entity.RegionLocation.Position) > 0.01f;

                        member.Position = entity.RegionLocation.Position;
                        member.Time -= deltaTime;
                        member.Speed += member.Acceleration * deltaTime;

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
            if (_game == null || entity == null || entity.IsInWorld == false || entity.TestStatus(EntityStatus.Destroyed))
                return false;

            List<EntityCollision> entityCollisionList = ListPool<EntityCollision>.Instance.Get();
            bool moved = false;

            if (Vector3.IsNearZero(vector))
                CheckForExistingCollisions(entity, true);
            else
            {
                var locomotor = entity.Locomotor;
                if (locomotor == null)
                {
                    ListPool<EntityCollision>.Instance.Return(entityCollisionList);
                    return Logger.WarnReturn(false, "MoveEntity(): locomotor == null");
                }

                bool noMissile = locomotor.IsMissile == false;
                bool sliding = noMissile && moveFlags.HasFlag(MoveEntityFlags.Sliding);
                bool sweepCollide = moveFlags.HasFlag(MoveEntityFlags.SweepCollide);
                bool sendToOwner = moveFlags.HasFlag(MoveEntityFlags.SendToOwner);
                bool sendToClients = moveFlags.HasFlag(MoveEntityFlags.SendToClients);
                bool allowSweep = noMissile;

                Vector3 desiredDestination = new();
                if (GetDesiredDestination(entity, vector, allowSweep, ref desiredDestination, out bool clipped))
                {
                    Vector3 collidedDestination = Vector3.Zero;
                    if (sweepCollide)
                        moved = SweepEntityCollideToDestination(entity, desiredDestination, sliding, ref collidedDestination, entityCollisionList);
                    else
                    {
                        collidedDestination = desiredDestination;
                        moved = true;
                    }

                    if (moved)
                    {
                        locomotor.MovementImpeded = clipped || !Vector3.EpsilonSphereTest(collidedDestination, desiredDestination);

                        ChangePositionFlags changeFlags = ChangePositionFlags.PhysicsResolve;
                        changeFlags |= !sendToOwner ? ChangePositionFlags.DoNotSendToOwner : 0;
                        changeFlags |= !sendToClients ? ChangePositionFlags.DoNotSendToClients : 0;

                        entity.ChangeRegionPosition(collidedDestination, null, changeFlags);
                    }

                    if (sweepCollide)
                        HandleEntityCollisions(entityCollisionList, entity, true);

                    if (clipped && entity.TestStatus(EntityStatus.Destroyed) == false && entity.IsInWorld)
                        NotifyEntityCollision(entity, null, collidedDestination);
                }
            }

            ListPool<EntityCollision>.Instance.Return(entityCollisionList);
            return moved;
        }

        private static bool SweepEntityCollideToDestination(WorldEntity entity, Vector3 desiredDestination, bool sliding, ref Vector3 collidedDestination, List<EntityCollision> entityCollisionList)
        {
            if (entity == null || entity.Region == null) return false;

            var location = entity.RegionLocation;
            Vector3 velocity = desiredDestination - location.Position;

            Aabb collideBounds = entity.EntityCollideBounds.ToAabb();
            collideBounds += collideBounds.Translate(velocity);

            SweepEntityCollideToDestinationHelper(entity, collideBounds, location.Position, desiredDestination, null, out EntityCollision collision, entityCollisionList);
            entityCollisionList.Sort();

            if (collision.OtherEntity != null)
            {
                while (entityCollisionList.Count > 0 && entityCollisionList[^1].Time > collision.Time)
                    entityCollisionList.RemoveAt(entityCollisionList.Count - 1);
                velocity *= collision.Time;
            }

            if (!sliding && Vector3.IsNearZero(velocity)) return false;

            collidedDestination = location.Position + velocity;

            if (sliding && collision.OtherEntity != null)
            {
                Vector3 normal2D = Vector3.SafeNormalize2D(collision.Normal, Vector3.Zero);
                Vector3 slidingVelocity = desiredDestination - collidedDestination;
                Vector3 slidingVelocity2D = slidingVelocity.To2D();

                float dot = Vector3.Dot(slidingVelocity2D, normal2D);
                if (dot < 0.0f)
                {
                    slidingVelocity2D -= normal2D * dot;

                    var locomotor = entity.Locomotor;
                    if (locomotor == null) return false;

                    Vector3 newDesiredDestination = new();
                    Vector3? normal = null;
                    locomotor.SweepFromTo(collidedDestination, collidedDestination + slidingVelocity2D, ref newDesiredDestination, ref normal);

                    Vector3 newVelocity = newDesiredDestination - collidedDestination;
                    if (Vector3.IsNearZero(newVelocity) == false)
                    {
                        SweepEntityCollideToDestinationHelper(entity, collideBounds, collidedDestination, newDesiredDestination, collision.OtherEntity, out EntityCollision newCollision, entityCollisionList);
                        collidedDestination += newVelocity * newCollision.Time;
                    }
                }
                return !Vector3.IsNearZero(collidedDestination - location.Position);
            }
            else
                return true;
        }

        private static void SweepEntityCollideToDestinationHelper(WorldEntity entity, in Aabb volume, Vector3 position, Vector3 destination, WorldEntity blockedEntity, out EntityCollision outCollision, List<EntityCollision> entityCollisionList)
        {
            Bounds bounds = entity.EntityCollideBounds;
            RegionLocation location = entity.RegionLocation;
            Vector3 velocity = destination - position;
            Vector3 velocity2D = velocity.To2D();
            outCollision = new();
            var context = entity.GetEntityRegionSPContext();
            foreach (var otherEntity in entity.Region.IterateEntitiesInVolume(volume, context))
                if (entity != otherEntity && blockedEntity != otherEntity)
                {
                    if (entity.CanCollideWith(otherEntity) || otherEntity.CanCollideWith(entity))
                    {
                        Bounds otherBounds = otherEntity.EntityCollideBounds;

                        float time = 1.0f;
                        Vector3? resultNormal = Vector3.ZAxis;
                        if (bounds.Sweep(otherBounds, Vector3.Zero, velocity, ref time, ref resultNormal) == false) continue;
                        Vector3 normal = resultNormal.Value;
                        Vector3 collisionPosition = location.Position + velocity * time;
                        EntityCollision entityCollision = new (otherEntity, time, collisionPosition, normal);
                        entityCollisionList.Add(entityCollision);

                        if (entity.CanBeBlockedBy(otherEntity))
                        {
                            float dot = Vector3.Dot(velocity2D, normal);
                            if (dot < 0.0f && (outCollision.OtherEntity == null || time < outCollision.Time))
                                outCollision = entityCollision;
                        }
                    }
                }           
        }

        private static bool GetDesiredDestination(WorldEntity entity, Vector3 vector, bool allowSweep, ref Vector3 resultPosition, out bool clipped)
        {
            RegionLocation location = entity.RegionLocation;
            Vector3 destination = location.Position + vector;
            clipped = false;
            Locomotor locomotor = entity.Locomotor;
            if (locomotor == null)
            {
                resultPosition = location.Position;
                return true;
            }

            Vector3? resultNormal = Vector3.ZAxis;
            SweepResult sweepResult = locomotor.SweepTo(destination, ref resultPosition, ref resultNormal);
            if (sweepResult == SweepResult.Failed) return false;
            clipped = (sweepResult != SweepResult.Success);

            Vector3 resultNormal2D = Vector3.SafeNormalize2D(resultNormal.Value, Vector3.Zero);

            if (locomotor.IsMissile)
                resultPosition.Z = destination.Z;

            if (clipped && Vector3.IsNearZero(location.Position - resultPosition))
                resultPosition += resultNormal2D * 0.1f;

            Region region = entity.Region;
            if (region != null)
                resultPosition.Z = Math.Clamp(resultPosition.Z, region.Aabb.Min.Z, region.Aabb.Max.Z);

            if (clipped && allowSweep)
            {
                Vector3 velocity = destination - resultPosition;
                Vector3 velocity2D = velocity.To2D();

                float dot = Vector3.Dot(velocity2D, resultNormal2D);
                if (dot < 0.0f)
                {
                    velocity2D += resultNormal2D * (-dot);

                    Vector3 fromPosition = resultPosition;
                    destination = resultPosition + velocity2D;
                    resultNormal = null;
                    sweepResult = locomotor.SweepFromTo(fromPosition, destination, ref resultPosition, ref resultNormal);
                    if (sweepResult == SweepResult.Failed) return false;
                }
            }

            if (Vector3.IsNearZero(resultPosition - location.Position)) return false;

            return true;
        }

        private void HandleEntityCollisions(List<EntityCollision> entityCollisionList, WorldEntity entity, bool applyRepulsionForces)
        {
            foreach (var collisionRecord in entityCollisionList)
                HandlePossibleEntityCollision(entity, collisionRecord, applyRepulsionForces, false);
        }

        private void CheckForExistingCollisions(WorldEntity entity, bool applyRepulsionForces)
        {
            if (entity == null) return;
            Region region = entity.Region;
            if (region == null || region.IsGenerated == false) return;

            Aabb bound = entity.EntityCollideBounds.ToAabb();            
            Vector3 position = entity.RegionLocation.Position;

            List<WorldEntity> collisions = ListPool<WorldEntity>.Instance.Get();
            var context = entity.GetEntityRegionSPContext();
            foreach (var otherEntity in region.IterateEntitiesInVolume(bound, context))
                if (entity != otherEntity)
                    collisions.Add(otherEntity);

            foreach (var otherEntity in collisions)
            {
                EntityCollision entityCollision = new (otherEntity, 0.0f, position, Vector3.ZAxis);
                HandlePossibleEntityCollision(entity, entityCollision, applyRepulsionForces, true);
            }

            ListPool<WorldEntity>.Instance.Return(collisions);
        }

        private void HandlePossibleEntityCollision(WorldEntity entity, in EntityCollision entityCollision, bool applyRepulsionForces, bool boundsCheck)
        {
            if (entity == null || entityCollision.OtherEntity == null) return;

            WorldEntity otherEntity = entityCollision.OtherEntity;

            if (CacheCollisionPair(entity, otherEntity) == false) return;

            EntityPhysics entityPhysics = entity.Physics;
            EntityPhysics otherPhysics = otherEntity.Physics;

            if (entity.CanBeBlockedBy(otherEntity))
            {
                if (boundsCheck)
                {
                    Bounds bounds = entity.EntityCollideBounds;
                    Bounds otherBounds = otherEntity.EntityCollideBounds;
                    if (bounds.Intersects(otherBounds) == false) return;
                }

                if (applyRepulsionForces)
                    ApplyRepulsionForces(entity, otherEntity);

                NotifyEntityCollision(entity, otherEntity, entityCollision.Position);

                if (otherEntity.CanCollideWith(entity))
                    NotifyEntityCollision(otherEntity, entity, otherEntity.RegionLocation.Position);
            }
            else if (entity.CanCollideWith(otherEntity) || otherEntity.CanCollideWith(entity))
            {
                if (boundsCheck)
                {
                    Bounds bounds = entity.EntityCollideBounds;
                    Bounds otherBounds = otherEntity.EntityCollideBounds;
                    if (bounds.Intersects(otherBounds) == false) return;
                }

                if (entityPhysics.IsTrackingOverlap() || otherPhysics.IsTrackingOverlap())
                {
                    UpdateOverlapEntryHelper(entityPhysics, otherEntity);
                    UpdateOverlapEntryHelper(otherPhysics, entity);

                    Vector3 entityPosition = entityCollision.Position;
                    Vector3 otherEntityPosition = otherEntity.RegionLocation.Position;
                    ResolveOverlapEvent(OverlapEventType.Update, entity, otherEntity, entityPosition, otherEntityPosition);
                    ResolveOverlapEvent(OverlapEventType.Update, otherEntity, entity, otherEntityPosition, entityPosition);
                }
            }
        }

        private static void ResolveOverlapEvent(OverlapEventType type, WorldEntity who, WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            if (who == null || whom == null) return;
            if (who.IsInWorld == false || whom.IsInWorld == false) return;

            if (type == OverlapEventType.Update)
            {
                if (who.Physics.OverlappedEntities.TryGetValue(whom.Id, out var overlappedEntity))
                {
                    bool overlapped = who.CanCollideWith(whom);
                    if (overlappedEntity.Overlapped != overlapped)
                    {
                        who.Physics.OverlappedEntities[whom.Id] = new(overlapped, overlappedEntity.Frame);
                        if (overlapped)
                            NotifyEntityOverlapBegin(who, whom, whoPos, whomPos);
                        else
                            NotifyEntityOverlapEnd(who, whom);
                    }
                }
            }
            else if (type == OverlapEventType.Remove)
            {
                if (who.Physics.OverlappedEntities.TryGetValue(whom.Id, out var overlappedEntity))
                {
                    bool overlapped = overlappedEntity.Overlapped;
                    who.Physics.OverlappedEntities.Remove(whom.Id);
                    if (overlapped) NotifyEntityOverlapEnd(who, whom);
                }
            }
        }

        public void OnExitedWorld(EntityPhysics entityPhysics)
        {
            if (entityPhysics != null && entityPhysics.Entity != null)
            {
                var who = entityPhysics.Entity;
                var entityManager = _game.EntityManager;

                while (entityPhysics.OverlappedEntities.Count > 0)
                {
                    var entry = entityPhysics.OverlappedEntities.First();
                    var whomId = entry.Key;
                    bool overlapped = entry.Value.Overlapped;
                    entityPhysics.OverlappedEntities.Remove(whomId);
                    var whom = entityManager.GetEntity<WorldEntity>(whomId);
                    if (whom == null) continue;
                    if (overlapped) NotifyEntityOverlapEnd(who, whom);

                    if (whom.Physics.OverlappedEntities.TryGetValue(who.Id, out var overlappedEntity))
                    {
                        overlapped = overlappedEntity.Overlapped;
                        whom.Physics.OverlappedEntities.Remove(who.Id);
                        if (overlapped) NotifyEntityOverlapEnd(whom, who);
                    }
                }
            }
        }

        private static void NotifyEntityCollision(WorldEntity who, WorldEntity whom, Vector3 whoPos)
        {
            who?.OnCollide(whom, whoPos);
            var evt = new EntityCollisionEvent(who, whom);
            who.CollideEvent.Invoke(evt);
        }

        private static void NotifyEntityOverlapBegin(WorldEntity who, WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            who?.OnOverlapBegin(whom, whoPos, whomPos);
            var evt = new EntityCollisionEvent(who, whom);
            who.OverlapBeginEvent.Invoke(evt);
        }

        private static void NotifyEntityOverlapEnd(WorldEntity who, WorldEntity whom)
        {
            who?.OnOverlapEnd(whom);
            var evt = new EntityCollisionEvent(who, whom);
            who.OverlapEndEvent.Invoke(evt);
        }

        private void UpdateOverlapEntryHelper(EntityPhysics entityPhysics, WorldEntity otherEntity)
        {
            if (entityPhysics.OverlappedEntities.TryGetValue(otherEntity.Id, out var entry) == false)
                RegisterEntityForPendingPhysicsResolve(entityPhysics.Entity);

            entityPhysics.OverlappedEntities[otherEntity.Id] = new(entry.Overlapped, _physicsFrames);
        }

        private static void ApplyRepulsionForces(WorldEntity entity, WorldEntity otherEntity)
        {
            if (entity == null || otherEntity == null) return;
            bool hasSphereCollide = entity.EntityCollideBounds.Geometry == GeometryType.Sphere || entity.EntityCollideBounds.Geometry == GeometryType.Capsule;
            bool hasOtherSphereCollide = otherEntity.EntityCollideBounds.Geometry == GeometryType.Sphere || otherEntity.EntityCollideBounds.Geometry == GeometryType.Capsule;
            if (!hasSphereCollide || !hasOtherSphereCollide) return;

            Vector3 vector = entity.GetVectorFrom(otherEntity).To2D();

            float distance;
            if (Vector3.IsNearZero(vector))
            {
                Game game = entity.Game;
                if (game == null) return;
                vector = Vector3.RandomUnitVector2D(game.Random);
                distance = 0.0f;
            }
            else
            {
                distance = Vector3.LengthTest(vector);
                if (distance > Segment.Epsilon)
                    vector /= distance;
                else
                    vector = Vector3.XAxis;
            }

            if (!Vector3.IsFinite(vector) || !float.IsFinite(distance))  return;

            float collisionImpact = entity.EntityCollideBounds.Radius + otherEntity.EntityCollideBounds.Radius - distance;
            if (collisionImpact < 0.001f) return;

            Vector3 repulseForce = vector * collisionImpact;

            if (entity.CanBeRepulsed && otherEntity.CanRepulseOthers)
                entity.Physics.AddRepulsionForce(repulseForce);
            if (otherEntity.CanBeRepulsed && entity.CanRepulseOthers)
                otherEntity.Physics.AddRepulsionForce(-repulseForce);
        }

        private static bool CacheCollisionPair(WorldEntity entity, WorldEntity otherEntity)
        {
            int collisionId = entity.Physics.CollisionId;
            int otherCollisionId = otherEntity.Physics.CollisionId;

            if (collisionId == -1 || otherCollisionId == -1) return false;

            Region region = entity.Region;
            if (region == null) return false;

            if (entity.Id < otherEntity.Id)
                return region.CollideEntities(collisionId, otherCollisionId); 
            else
                return region.CollideEntities(otherCollisionId, collisionId);
        }

        private void SwapCurrentForceReadWriteIndices()
        {
            _currentForceReadWriteState = !_currentForceReadWriteState;
        }

        public void RegisterEntityForPendingPhysicsResolve(WorldEntity worldEntity)
        {
            if (worldEntity == null) return;
            var entityPhysics = worldEntity.Physics;
            if (entityPhysics.RegisteredPhysicsFrameId != _physicsFrames)
            {
                entityPhysics.RegisteredPhysicsFrameId = _physicsFrames;
                _entitiesPendingResolve.Add(worldEntity.Id);
            }
        }

        public void AddKnockbackForce(WorldEntity entity, Vector3 position, float time, float speed, float acceleration)
        {
            ForceSystem pendingForce = GetPendingForceSystem(position);
            var epicenter = pendingForce.Epicenter;

            var member = new ForceSystemMember
            {
                EntityId = entity.Id,
                Position = entity.RegionLocation.Position,
                Time = time,
                Speed = speed,
                Acceleration = acceleration
            };

            var direction = (member.Position - epicenter).To2D();
            if (Vector3.IsNearZero(direction))
                member.Direction = entity.Forward;
            else
                member.Direction = Vector3.Normalize(direction);

            float distanceSq = Vector3.DistanceSquared(epicenter, member.Position);
            var pendingMembers = pendingForce.Members;

            foreach (var pendingMember in pendingMembers.Iterate())
                if (distanceSq > Vector3.DistanceSquared(epicenter, pendingMember.Position))
                {
                    pendingForce.Members.InsertBefore(member, pendingMember);
                    break;
                }

            if (pendingMembers.Contains(member) == false) pendingMembers.AddBack(member);
        }

        private ForceSystem GetPendingForceSystem(Vector3 position)
        {
            foreach (var pendingForce in _pendingForceSystems)
                if (Vector3.EpsilonSphereTest(pendingForce.Epicenter, position, 0.1f)) 
                    return pendingForce;

            ForceSystem newForce = new(position);
            _pendingForceSystems.Add(newForce);
            return newForce;
        }
    }

    [Flags]
    public enum MoveEntityFlags
    {
        SendToOwner = 1 << 0,
        SweepCollide = 1 << 2,
        Sliding = 1 << 3,
        SendToClients = 1 << 4,
    }

    public class PhysicsContext
    {
        public List<WorldEntity> AttachedEntities { get; private set; }

        public PhysicsContext()
        {
            AttachedEntities = new();
        }
    }

    public enum OverlapEventType
    {
        Update,
        Remove
    }

    public class OverlapEvent
    {
        public OverlapEventType Type;
        public WorldEntity Who;
        public WorldEntity Whom;
        public Vector3 WhoPos;
        public Vector3 WhomPos;

        public OverlapEvent(OverlapEventType type, WorldEntity who, WorldEntity whom)
        {
            Type = type;
            Who = who;
            Whom = whom;
            WhoPos = Vector3.Zero;
            WhomPos = Vector3.Zero;
        }
    }

    public struct EntityCollision : IComparable<EntityCollision>
    {
        public WorldEntity OtherEntity;
        public float Time;
        public Vector3 Position;
        public Vector3 Normal;

        public EntityCollision()
        {
            OtherEntity = null;
            Time = 1.0f;
            Position = Vector3.Zero;
            Normal = Vector3.Zero;
        }

        public EntityCollision(WorldEntity otherEntity, float time, Vector3 position, Vector3 normal)
        {
            OtherEntity = otherEntity;
            Time = time;
            Position = position;
            Normal = normal;
        }

        public readonly int CompareTo(EntityCollision other)
        {
            return Time.CompareTo(other.Time);
        }
    }
}
