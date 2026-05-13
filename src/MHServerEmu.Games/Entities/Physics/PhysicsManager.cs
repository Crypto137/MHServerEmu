using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities.Physics
{
    public class PhysicsManager
    {
        public int CurrentForceReadIndex { get => _currentForceReadWriteState ? 1 : 0; }
        public int CurrentForceWriteIndex { get => _currentForceReadWriteState ? 0 : 1; }

        private readonly List<ForceSystem> _pendingForceSystems = new();
        private readonly List<ForceSystem> _activeForceSystems = new();
        private readonly Queue<OverlapEvent> _overlapEvents = new();
        
        private List<ulong> _entitiesPendingResolve = new();    // entities to resolve next frame
        private List<ulong> _entitiesResolving = new();         // entities to resolve this frame
        private bool _currentForceReadWriteState = false;
        private uint _physicsFrame = 1;

        private Game _game;

        public PhysicsManager()
        {
        }

        public bool Initialize(Game game)
        {
            _game = game;
            // TODO?: m_forceSystemMemberPool.Initialize( sizeof(ForceSystemMember), 64, 1 )
            return true;
        }

        public void ResolveEntities()
        {
            if (!Verify.IsNotNull(_game)) return;
            if (!Verify.IsTrue(_entitiesResolving.Count == 0)) return;

            // Double buffer style list swap
            (_entitiesResolving, _entitiesPendingResolve) = (_entitiesPendingResolve, _entitiesResolving);
            _physicsFrame++;

            SwapCurrentForceReadWriteIndices();
            ApplyForceSystems();

            using PhysicsContext physicsContext = new();
            ResolveEntitiesAllowPenetration(physicsContext, _entitiesResolving);
            ResolveEntitiesOverlapState(physicsContext);

            _entitiesResolving.Clear();

            foreach (Region region in _game.RegionManager)
                region.ClearCollidedEntities();
        }

        private void ResolveEntitiesOverlapState(in PhysicsContext physicsContext)
        {
            EntityManager entityManager = _game.EntityManager;

            foreach (ulong entityId in _entitiesResolving)
                ResolveEntitiesOverlapState(entityManager.GetEntity<WorldEntity>(entityId), _overlapEvents);

            foreach (WorldEntity worldEntity in physicsContext.AttachedEntities)
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
                EntityPhysics entityPhysics = worldEntity.Physics;
                EntityManager entityManager = _game.EntityManager;

                using var overlappedEntitiesHandle = ListPool<KeyValuePair<ulong, OverlapEntityEntry>>.Instance.Get(out var overlappedEntities);
                overlappedEntities.AddRange(entityPhysics.OverlappedEntities);

                foreach (var overlappedEntry in overlappedEntities)
                {
                    if (overlappedEntry.Value.Frame != _physicsFrame)
                    {
                        WorldEntity overlappedEntity = entityManager.GetEntity<WorldEntity>(overlappedEntry.Key);
                        if (Verify.IsNotNull(overlappedEntity, $"Failed to get overlapped entity {overlappedEntry.Key} referencing {worldEntity}"))
                            overlapEvents.Enqueue(new(OverlapEventType.Remove, worldEntity, overlappedEntity));
                        else
                            entityPhysics.OverlappedEntities.Remove(overlappedEntry.Key);
                    }
                }
            }
        }

        private void ResolveEntitiesAllowPenetration(in PhysicsContext physicsContext, List<ulong> entitiesResolving)
        {
            if (!Verify.IsNotNull(_game)) return;

            RegionManager regionManager = _game.RegionManager;
            if (!Verify.IsNotNull(regionManager)) return;
            
            EntityManager entityManager = _game.EntityManager;
            foreach (ulong entityId in entitiesResolving)
            {
                WorldEntity worldEntity = entityManager.GetEntity<WorldEntity>(entityId);
                if (worldEntity == null || worldEntity.TestStatus(EntityStatus.Destroyed) || worldEntity.IsInWorld == false)
                    continue;

                EntityPhysics entityPhysics = worldEntity.Physics;
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

        private void UpdateAttachedEntityPositions(in PhysicsContext physicsContext, WorldEntity parentEntity)
        {
            if (!Verify.IsNotNull(parentEntity)) return;

            using var attachedEntitiesHandle = ListPool<ulong>.Instance.Get(out List<ulong> attachedEntities);
            if (parentEntity.Physics.GetAttachedEntities(attachedEntities))
            {
                Vector3 parentEntityPosition = parentEntity.RegionLocation.Position;
                Orientation parentEntityOrientation = parentEntity.Orientation;

                EntityManager entityManager = _game.EntityManager;
                foreach (ulong attachedEntityId in attachedEntities)
                {
                    WorldEntity attachedEntity = entityManager.GetEntity<WorldEntity>(attachedEntityId);
                    if (attachedEntity != null && attachedEntity.IsInWorld)
                    {
                        WorldEntityPrototype worldEntityProto = attachedEntity.WorldEntityPrototype;
                        if (!Verify.IsNotNull(worldEntityProto)) return;
                        
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

        private void ApplyForceSystems()
        {
            _activeForceSystems.AddRange(_pendingForceSystems);
            _pendingForceSystems.Clear();

            // NOTE: our iteration order here is reversed compared to the client.
            for (int i = _activeForceSystems.Count - 1; i >= 0; i--)
            {
                if (ApplyForceSystemCheckCompletion(_activeForceSystems[i]))
                    _activeForceSystems.RemoveAt(i);
            }
        }

        private bool ApplyForceSystemCheckCompletion(ForceSystem forceSystem)
        {
            bool complete = true;

            EntityManager entityManager = _game.EntityManager; 

            foreach (ForceSystemMember member in forceSystem.Members)
            {
                if (!Verify.IsNotNull(member))
                    continue;

                bool active = false;

                WorldEntity entity = entityManager.GetEntity<WorldEntity>(member.EntityId);
                if (entity != null && entity.IsInWorld)
                {
                    if (Verify.IsTrue(entity.TestStatus(EntityStatus.Destroyed) == false, $"Entity {entity} is destroyed"))
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
                }

                if (active == false)
                    forceSystem.Members.Remove(member);
            }

            return complete;
        }

        private bool MoveEntity(WorldEntity entity, Vector3 vector, MoveEntityFlags moveFlags)
        {
            if (!Verify.IsNotNull(_game)) return false;

            if (!Verify.IsTrue(entity != null && entity.IsInWorld && entity.TestStatus(EntityStatus.Destroyed) == false,
                $"Entity {entity} is not in the world.  destroyed={entity?.TestStatus(EntityStatus.Destroyed)}"))
                return false;

            using var entityCollisionListHandle = ListPool<EntityCollision>.Instance.Get(out List<EntityCollision> entityCollisionList);
            bool moved = false;

            if (Vector3.IsNearZero(vector))
            {
                CheckForExistingCollisions(entity, true);
            }
            else
            {
                Locomotor locomotor = entity.Locomotor;
                if (!Verify.IsNotNull(locomotor)) return false;

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
                    {
                        moved = SweepEntityCollideToDestination(entity, desiredDestination, sliding, ref collidedDestination, entityCollisionList);
                    }
                    else
                    {
                        collidedDestination = desiredDestination;
                        moved = true;
                    }

                    if (moved)
                    {
                        locomotor.MovementImpeded = clipped || !Vector3.EpsilonSphereTest(collidedDestination, desiredDestination);

                        ChangePositionFlags changeFlags = ChangePositionFlags.PhysicsResolve;
                        if (sendToOwner == false)
                            changeFlags |= ChangePositionFlags.DoNotSendToOwner;
                        if (sendToClients == false)
                            changeFlags |= ChangePositionFlags.DoNotSendToClients;

                        entity.ChangeRegionPosition(collidedDestination, null, changeFlags);
                    }

                    if (sweepCollide)
                        HandleEntityCollisions(entityCollisionList, entity, true);

                    if (clipped && entity.TestStatus(EntityStatus.Destroyed) == false && entity.IsInWorld)
                        NotifyEntityCollision(entity, null, collidedDestination);
                }
            }

            return moved;
        }

        private static bool SweepEntityCollideToDestination(WorldEntity entity, Vector3 desiredDestination, bool sliding, ref Vector3 collidedDestination, List<EntityCollision> entityCollisionList)
        {
            if (!Verify.IsNotNull(entity)) return false;

            Region region = entity.Region;
            if (!Verify.IsNotNull(region)) return false;

            ref RegionLocation location = ref entity.RegionLocation;
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

            if (sliding == false && Vector3.IsNearZero(velocity))
                return false;

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

                    Locomotor locomotor = entity.Locomotor;
                    if (!Verify.IsNotNull(locomotor)) return false;

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

                return Vector3.IsNearZero(collidedDestination - location.Position) == false;
            }
            else
            {
                return true;
            }
        }

        private static void SweepEntityCollideToDestinationHelper(WorldEntity entity, in Aabb volume, Vector3 position, Vector3 destination, WorldEntity blockedEntity, out EntityCollision outCollision, List<EntityCollision> entityCollisionList)
        {
            outCollision = new();

            ref Bounds bounds = ref entity.EntityCollideBounds;
            ref RegionLocation location = ref entity.RegionLocation;
            Vector3 velocity = destination - position;
            Vector3 velocity2D = velocity.To2D();

            EntityRegionSPContext context = entity.GetEntityRegionSPContext();
            foreach (WorldEntity otherEntity in entity.Region.IterateEntitiesInVolume(volume, context))
            {
                if (otherEntity == entity || otherEntity == blockedEntity)
                    continue;

                if (entity.CanCollideWith(otherEntity) || otherEntity.CanCollideWith(entity))
                {
                    ref Bounds otherBounds = ref otherEntity.EntityCollideBounds;

                    float time = 1.0f;
                    Vector3? resultNormal = Vector3.ZAxis;
                    if (bounds.Sweep(ref otherBounds, Vector3.Zero, velocity, ref time, ref resultNormal) == false)
                        continue;

                    Vector3 normal = resultNormal.Value;
                    Vector3 collisionPosition = location.Position + velocity * time;
                    EntityCollision entityCollision = new(otherEntity, time, collisionPosition, normal);
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
            ref RegionLocation location = ref entity.RegionLocation;
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
            if (sweepResult == SweepResult.Failed)
                return false;

            clipped = sweepResult != SweepResult.Success;

            Vector3 resultNormal2D = Vector3.SafeNormalize2D(resultNormal.Value, Vector3.Zero);

            if (locomotor.IsMissile)
                resultPosition.Z = destination.Z;

            if (clipped && Vector3.IsNearZero(location.Position - resultPosition))
                resultPosition += resultNormal2D * 0.1f;

            Region region = entity.Region;
            if (Verify.IsNotNull(region))
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
                    if (!Verify.IsTrue(sweepResult != SweepResult.Failed)) return false;
                }
            }

            return Vector3.IsNearZero(resultPosition - location.Position) == false;
        }

        private void HandleEntityCollisions(List<EntityCollision> entityCollisionList, WorldEntity entity, bool applyRepulsionForces)
        {
            foreach (EntityCollision collisionRecord in entityCollisionList)
                HandlePossibleEntityCollision(entity, collisionRecord, applyRepulsionForces, false);
        }

        private void CheckForExistingCollisions(WorldEntity entity, bool applyRepulsionForces)
        {
            if (!Verify.IsNotNull(entity)) return;

            Region region = entity.Region;
            if (!Verify.IsNotNull(region)) return;

            Aabb bound = entity.EntityCollideBounds.ToAabb();            
            Vector3 position = entity.RegionLocation.Position;

            using var collisionsHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> collisions);
            EntityRegionSPContext context = entity.GetEntityRegionSPContext();
            foreach (WorldEntity otherEntity in region.IterateEntitiesInVolume(bound, context))
            {
                if (otherEntity != entity)
                    collisions.Add(otherEntity);
            }

            foreach (WorldEntity otherEntity in collisions)
            {
                EntityCollision entityCollision = new(otherEntity, 0.0f, position, Vector3.ZAxis);
                HandlePossibleEntityCollision(entity, entityCollision, applyRepulsionForces, true);
            }
        }

        private void HandlePossibleEntityCollision(WorldEntity entity, in EntityCollision entityCollision, bool applyRepulsionForces, bool boundsCheck)
        {
            if (!Verify.IsNotNull(entity)) return;

            WorldEntity otherEntity = entityCollision.OtherEntity;
            if (!Verify.IsNotNull(otherEntity)) return;

            if (CacheCollisionPair(entity, otherEntity) == false)
                return;

            EntityPhysics entityPhysics = entity.Physics;
            EntityPhysics otherPhysics = otherEntity.Physics;

            if (entity.CanBeBlockedBy(otherEntity))
            {
                if (boundsCheck)
                {
                    ref Bounds bounds = ref entity.EntityCollideBounds;
                    ref Bounds otherBounds = ref otherEntity.EntityCollideBounds;
                    if (bounds.Intersects(ref otherBounds) == false)
                        return;
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
                    ref Bounds bounds = ref entity.EntityCollideBounds;
                    ref Bounds otherBounds = ref otherEntity.EntityCollideBounds;
                    if (bounds.Intersects(ref otherBounds) == false)
                        return;
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
            if (!Verify.IsNotNull(who)) return;
            if (!Verify.IsNotNull(whom)) return;

            if (who.IsInWorld == false || whom.IsInWorld == false)
                return;

            if (type == OverlapEventType.Update)
            {
                if (Verify.IsTrue(who.Physics.OverlappedEntities.TryGetValue(whom.Id, out var overlappedEntity)))
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
                    if (overlapped)
                        NotifyEntityOverlapEnd(who, whom);
                }
            }
        }

        public void OnExitedWorld(EntityPhysics entityPhysics)
        {
            if (!Verify.IsNotNull(entityPhysics)) return;
            if (!Verify.IsNotNull(entityPhysics.Entity)) return;

            WorldEntity who = entityPhysics.Entity;

            EntityManager entityManager = _game.EntityManager;
            while (entityPhysics.OverlappedEntities.Count > 0)
            {
                var entry = entityPhysics.OverlappedEntities.First();
                
                ulong whomId = entry.Key;
                bool overlapped = entry.Value.Overlapped;
                entityPhysics.OverlappedEntities.Remove(whomId);

                WorldEntity whom = entityManager.GetEntity<WorldEntity>(whomId);
                if (whom == null)
                    continue;
                
                if (overlapped)
                    NotifyEntityOverlapEnd(who, whom);

                if (whom.Physics.OverlappedEntities.TryGetValue(who.Id, out var overlappedEntity))
                {
                    overlapped = overlappedEntity.Overlapped;
                    whom.Physics.OverlappedEntities.Remove(who.Id);
                    if (overlapped)
                        NotifyEntityOverlapEnd(whom, who);
                }
            }
        }

        private static void NotifyEntityCollision(WorldEntity who, WorldEntity whom, Vector3 whoPos)
        {
            if (!Verify.IsNotNull(who)) return;
            who.OnCollide(whom, whoPos);
            who.CollideEvent.Invoke(new(who, whom));
        }

        private static void NotifyEntityOverlapBegin(WorldEntity who, WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            if (!Verify.IsNotNull(who)) return;
            who.OnOverlapBegin(whom, whoPos, whomPos);
            who.OverlapBeginEvent.Invoke(new(who, whom));
        }

        private static void NotifyEntityOverlapEnd(WorldEntity who, WorldEntity whom)
        {
            if (!Verify.IsNotNull(who)) return;
            who.OnOverlapEnd(whom);
            who.OverlapEndEvent.Invoke(new(who, whom));
        }

        private void UpdateOverlapEntryHelper(EntityPhysics entityPhysics, WorldEntity otherEntity)
        {
            if (entityPhysics.OverlappedEntities.TryGetValue(otherEntity.Id, out var entry) == false)
                RegisterEntityForPendingPhysicsResolve(entityPhysics.Entity);

            entityPhysics.OverlappedEntities[otherEntity.Id] = new(entry.Overlapped, _physicsFrame);
        }

        private static void ApplyRepulsionForces(WorldEntity entityA, WorldEntity entityB)
        {
            if (!Verify.IsNotNull(entityA)) return;
            if (!Verify.IsNotNull(entityB)) return;

            bool hasSphereCollideA = entityA.EntityCollideBounds.Geometry == GeometryType.Sphere || entityA.EntityCollideBounds.Geometry == GeometryType.Capsule;
            bool hasSphereCollideB = entityB.EntityCollideBounds.Geometry == GeometryType.Sphere || entityB.EntityCollideBounds.Geometry == GeometryType.Capsule;
            if (hasSphereCollideA == false || hasSphereCollideB == false)
                return;

            Vector3 toEntity = entityA.GetVectorFrom(entityB).To2D();

            float toEntityDist;
            if (Vector3.IsNearZero(toEntity))
            {
                Game game = entityA.Game;
                if (!Verify.IsNotNull(game)) return;
                toEntity = Vector3.RandomUnitVector2D(game.Random);
                toEntityDist = 0.0f;
            }
            else
            {
                toEntityDist = Vector3.LengthTest(toEntity);
                if (toEntityDist > Segment.Epsilon)
                    toEntity /= toEntityDist;
                else
                    toEntity = Vector3.XAxis;
            }

            if (!Verify.IsTrue(Vector3.IsFinite(toEntity) && float.IsFinite(toEntityDist))) return;

            float collisionImpact = entityA.EntityCollideBounds.Radius + entityB.EntityCollideBounds.Radius - toEntityDist;
            if (collisionImpact < 0.001f)
                return;

            Vector3 repulseForce = toEntity * collisionImpact;

            if (entityA.CanBeRepulsed && entityB.CanRepulseOthers)
                entityA.Physics.AddRepulsionForce(repulseForce);

            if (entityB.CanBeRepulsed && entityA.CanRepulseOthers)
                entityB.Physics.AddRepulsionForce(-repulseForce);
        }

        private static bool CacheCollisionPair(WorldEntity entity, WorldEntity otherEntity)
        {
            int collisionId = entity.Physics.CollisionId;
            int otherCollisionId = otherEntity.Physics.CollisionId;

            if (collisionId == -1 || otherCollisionId == -1)
                return false;

            Region region = entity.Region;
            if (!Verify.IsNotNull(region)) return false;

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
            if (!Verify.IsNotNull(worldEntity)) return;

            EntityPhysics entityPhysics = worldEntity.Physics;
            if (entityPhysics.RegisteredPhysicsFrameId != _physicsFrame)
            {
                entityPhysics.RegisteredPhysicsFrameId = _physicsFrame;
                _entitiesPendingResolve.Add(worldEntity.Id);
            }
        }

        public void AddKnockbackForce(WorldEntity entity, Vector3 position, float time, float speed, float acceleration)
        {
            if (!Verify.IsNotNull(entity)) return;
            if (!Verify.IsTrue(entity.IsInWorld)) return;
            
            if (time <= 0)
                return;

            if (Segment.IsNearZero(speed) && Segment.IsNearZero(acceleration))
                return;

            ForceSystem pendingForce = GetPendingForceSystem(position);
            Vector3 epicenter = pendingForce.Epicenter;

            ForceSystemMember member = new()
            {
                EntityId = entity.Id,
                Position = entity.RegionLocation.Position,
                Time = time,
                Speed = speed,
                Acceleration = acceleration
            };

            Vector3 direction = (member.Position - epicenter).To2D();
            if (Vector3.IsNearZero(direction))
                member.Direction = entity.Forward;
            else
                member.Direction = Vector3.Normalize(direction);

            float distanceSq = Vector3.DistanceSquared(epicenter, member.Position);
            ForceSystemMemberList pendingMembers = pendingForce.Members;

            foreach (ForceSystemMember pendingMember in pendingMembers)
            {
                if (distanceSq > Vector3.DistanceSquared(epicenter, pendingMember.Position))
                {
                    pendingForce.Members.InsertBefore(member, pendingMember);
                    break;
                }
            }

            if (pendingMembers.Contains(member) == false)
                pendingMembers.AddBack(member);
        }

        private ForceSystem GetPendingForceSystem(Vector3 position)
        {
            foreach (ForceSystem pendingForce in _pendingForceSystems)
            {
                if (Vector3.EpsilonSphereTest(pendingForce.Epicenter, position, 0.1f))
                    return pendingForce;
            }

            ForceSystem newForce = new(position);
            _pendingForceSystems.Add(newForce);
            return newForce;
        }
    }

    [Flags]
    public enum MoveEntityFlags
    {
        None            = 0,
        SendToOwner     = 1 << 0,
        SweepCollide    = 1 << 1,
        Sliding         = 1 << 2,
        // 1 << 3 unused?
        SendToClients   = 1 << 4,
    }

    public readonly struct PhysicsContext : IDisposable
    {
        public List<WorldEntity> AttachedEntities { get; }

        public PhysicsContext()
        {
            AttachedEntities = ListPool<WorldEntity>.Instance.Get();
        }

        public void Dispose()
        {
            ListPool<WorldEntity>.Instance.Return(AttachedEntities);
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
