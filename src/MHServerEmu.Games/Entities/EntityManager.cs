﻿using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Physics;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    [Flags]
    public enum GetEntityFlags
    {
        None                = 0,
        DestroyedOnly       = 1 << 0,
        UnpackedOnly        = 1 << 1
    }

    public enum EntityCollection
    {
        Simulated = 0,
        Locomotion = 1,
        All = 3,
    }

    public class EntityInvasiveCollection : InvasiveList<Entity>
    {
        public EntityInvasiveCollection(EntityCollection collectionType, int maxIterators = 8) : base(maxIterators, (int)collectionType) { }
        public override InvasiveListNode<Entity> GetInvasiveListNode(Entity element, int listId) => element.GetInvasiveListNode(listId);
    }

    public class EntityManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;

        private readonly Dictionary<ulong, Entity> _entityDict = new();
        private readonly Dictionary<ulong, Entity> _entityDbGuidDict = new();
        private readonly HashSet<Player> _players = new();
        private readonly Queue<ulong> _entityDeletionQueue = new();

        private ulong _nextEntityId = 1;
        private ulong GetNextEntityId() { return _nextEntityId++; }
        public ulong PeekNextEntityId() { return _nextEntityId; }

        public bool IsDestroyingAllEntities { get; private set; } = false;

        public IEnumerable<Player> Players { get => _players; }

        public PhysicsManager PhysicsManager { get; set; }
        public EntityInvasiveCollection AllEntities { get; private set; }
        public EntityInvasiveCollection SimulatedEntities { get; private set; }
        public EntityInvasiveCollection LocomotionEntities { get; private set; }

        public EntityManager(Game game)
        {            
            _game = game;
            PhysicsManager = new();
            AllEntities = new(EntityCollection.All);
            SimulatedEntities = new(EntityCollection.Simulated);
            LocomotionEntities = new(EntityCollection.Locomotion);
        }

        public bool Initialize()
        {
            return PhysicsManager.Initialize(_game);
        }

        public Entity CreateEntity(EntitySettings settings)
        {
            if (IsDestroyingAllEntities) return null;   // Prevent new entities from being created during cleanup

            if (settings.EntityRef == PrototypeId.Invalid)
                return Logger.WarnReturn<Entity>(null, "CreateEntity(): Invalid prototype ref provided in settings");

            if (settings.Id == 0) settings.Id = GetNextEntityId();

            Entity entity;
            // TODO: ProcessPendingDestroyImmediate()

            // Check for id collisions
            entity = GetEntity(settings.Id, GetEntityFlags.UnpackedOnly);
            if (entity != null)
                return Logger.WarnReturn<Entity>(null, $"CreateEntity(): Collision in entity id, existing entity found: {entity}");

            if (settings.DbGuid != 0)
            {
                entity = GetEntityByDbGuid(settings.DbGuid, GetEntityFlags.UnpackedOnly);
                if (entity != null)
                    return Logger.WarnReturn<Entity>(null, $"CreateEntity(): Collision in entity dbid, existing entity found: {entity}");
            }

            entity = _game.AllocateEntity(settings.EntityRef);

            entity.ModifyCollectionMembership(EntityCollection.All, true);

            _entityDict.Add(settings.Id, entity);
            if (settings.DbGuid != 0)
                _entityDbGuidDict[settings.DbGuid] = entity;

            // TODO  SetStatus

            entity.PreInitialize(settings);
            entity.Initialize(settings);
            FinalizeEntity(entity, settings);

            return entity;
        }

        private void FinalizeEntity(Entity entity, EntitySettings settings)
        {
            entity.OnPostInit(settings);
            // TODO InventoryLocation
            if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.EnterGameWorld))
            {
                var owner = entity.GetOwner();
                if (owner == null || owner.IsInGame)
                    entity.EnterGame(settings);
            }
            if (entity is WorldEntity worldEntity)
            {
                worldEntity.RegisterActions(settings.Actions);
                // custom StartAction
                worldEntity.AppendStartAction(settings.ActionsTarget);
                if (settings.RegionId != 0)
                {
                    Region region = _game.RegionManager.GetRegion(settings.RegionId);
                    var position = settings.Position;
                    if (worldEntity.ShouldSnapToFloorOnSpawn)
                    {
                        position = RegionLocation.ProjectToFloor(region, position);
                        position = worldEntity.FloorToCenter(position);
                    }
                    worldEntity.EnterWorld(region, position, settings.Orientation, settings);
                }
            }
        }

        public bool DestroyEntity(Entity entity)
        {
            if (entity == null) return Logger.WarnReturn(false, "DestroyEntity(): entity == null");

            if (entity.TestStatus(EntityStatus.PendingDestroy)) return Logger.WarnReturn(false,
                $"DestroyEntity(): Entity already marked as PendingDestroy, this means that something was using an entity reference even though it was pending destroy which needs to be fixed! Entity: {entity}");

            if (entity.TestStatus(EntityStatus.Destroyed)) return Logger.WarnReturn(false,
                $"DestroyEntity(): Entity already marked as Destroy, this means that something was using an entity reference even though it was destroyed which needs to be fixed! Entity: {entity}");

            entity.SetStatus(EntityStatus.PendingDestroy, true);

            // Destroy entities belonging to this entity
            entity.DestroyContained();

            // Remove this entity from the inventory it is in
            if (entity.InventoryLocation.IsValid)
                entity.ChangeInventoryLocation(null);

            // Finish destruction
            entity.SetStatus(EntityStatus.PendingDestroy, false);
            entity.SetStatus(EntityStatus.Destroyed, true);
            _entityDeletionQueue.Enqueue(entity.Id);    // Enqueue entity for deletion at the end of the next frame

            // Remove entity from the game
            entity.ExitGame();

            // Remove DbId lookup
            if (entity.DatabaseUniqueId != 0)
                _entityDbGuidDict.Remove(entity.DatabaseUniqueId);

            return true;
        }

        public bool AddPlayer(Player player)
        {
            if (player == null) return Logger.WarnReturn(false, "AddPlayer(): player == null");
            bool playerAdded = _players.Add(player);
            if (playerAdded == false) Logger.Warn($"AddPlayer(): Failed to add player {player}");
            return playerAdded;
        }

        public bool RemovePlayer(Player player)
        {
            if (player == null) return Logger.WarnReturn(false, "RemovePlayer(): player == null");
            bool playerRemoved = _players.Remove(player);
            if (playerRemoved == false) Logger.Warn($"RemovePlayer(): Failed to remove player {player}");
            return playerRemoved;
        }

        public T GetEntity<T>(ulong entityId, GetEntityFlags flags = GetEntityFlags.None) where T : Entity
        {
            // This validation happens here rather than in the private method because the private method
            // is used in CreateEntity() to check for id/dbGuid collisions.
            if (entityId == Entity.InvalidId) return Logger.WarnReturn<T>(null, "GetEntity(): entityId == Entity.InvalidId");

            // Prevent destroyed entities from being accessed externally.
            return GetEntity(entityId, flags & ~GetEntityFlags.DestroyedOnly) as T;
        }

        public T GetEntityByDbGuid<T>(ulong dbGuid, GetEntityFlags flags = GetEntityFlags.None) where T: Entity
        {
            // Same as above, but for DbGuid
            if (dbGuid == 0) return Logger.WarnReturn<T>(null, "GetEntityByDbGuid(): dbGuid == 0");

            return GetEntityByDbGuid(dbGuid, flags & ~GetEntityFlags.DestroyedOnly) as T;
        }

        public Transition GetTransitionInRegion(Destination destination, ulong regionId)
        {
            PrototypeId areaRef = destination.AreaRef;
            PrototypeId cellRef = destination.CellRef;
            PrototypeId entityRef = destination.EntityRef;
            foreach (var entity in _entityDict.Values)
                if (entity.RegionId == regionId)
                {
                    if (entity is not Transition transition) continue;
                    if (areaRef != 0 && areaRef != (PrototypeId)transition.RegionLocation.Area.PrototypeId) continue;
                    if (cellRef != 0 && cellRef != transition.RegionLocation.Cell.PrototypeId) continue;
                    if (transition.BaseData.EntityPrototypeRef == entityRef)
                        return transition;
                }

            return default;
        }

        public IEnumerable<Entity> IterateEntities()
        {
            foreach (var entity in _entityDict.Values)
                yield return entity;
        }

        public IEnumerable<Entity> IterateEntities(Cell cell)
        {
            foreach (var entity in _entityDict.Values)
                if (entity is WorldEntity worldEntity && worldEntity.Cell == cell)
                    yield return entity;
        }

        public IEnumerable<Entity> IterateEntities(Region region)
        {
            foreach (var entity in _entityDict.Values)
                if (entity is WorldEntity worldEntity && worldEntity.Region == region)
                    yield return entity;
        }

        public void PhysicsResolveEntities()
        {
            PhysicsManager.ResolveEntities();
        }

        public void LocomoteEntities()
        {
            foreach (var entity in LocomotionEntities.Iterate())
                if (entity is WorldEntity worldEntity)
                    worldEntity?.Locomotor.Locomote();
        }

        public void ProcessDeferredLists()
        {
            // TODO: ProcessCondemnedPowerList()
            ProcessDestroyed();
        }

        private Entity GetEntity(ulong entityId, GetEntityFlags flags)
        {
            if (_entityDict.TryGetValue(entityId, out Entity entity) && ValidateEntityForGet(entity, flags))
                return entity;

            // It appears there should be some kind of fallback to packed entities, but this code is not present in the client.
            //if (flags.HasFlag(GetEntityFlags.DestroyedOnly) == false && flags.HasFlag(GetEntityFlags.UnpackedOnly) == false)
            //    return null;    // TODO: TryUnpackArchivedEntity(entityId);

            return null;
        }

        private Entity GetEntityByDbGuid(ulong dbGuid, GetEntityFlags flags)
        {
            if (_entityDbGuidDict.TryGetValue(dbGuid, out Entity entity) && ValidateEntityForGet(entity, flags))
                return entity;

            // It appears there should be some kind of fallback to packed entities, but this code is not present in the client.
            //if (flags.HasFlag(GetEntityFlags.DestroyedOnly) == false && flags.HasFlag(GetEntityFlags.UnpackedOnly) == false)
            //    return null;    // TODO: TryUnpackArchivedEntityByDbGuid(dbGuid);

            return null;
        }

        private bool ValidateEntityForGet(Entity entity, GetEntityFlags flags)
        {
            if (entity == null) return false;
            return entity.TestStatus(EntityStatus.Destroyed) == flags.HasFlag(GetEntityFlags.DestroyedOnly);
        }

        private bool ProcessDestroyed()
        {
            if (_game == null) return Logger.WarnReturn(false, "ProcessDestroyed(): _game == null");

            // Delete all destroyed entities
            while (_entityDeletionQueue.Count != 0)
            {
                ulong entityId = _entityDeletionQueue.Dequeue();

                if (_entityDict.TryGetValue(entityId, out Entity entity) == false)
                    Logger.Warn($"ProcessDestroyed(): Failed to get entity for enqueued id {entityId}");
                else
                {
                    Logger.Trace($"Deleting entity {entity}");
                    DeleteEntity(entity);
                }
            }

            return true;
        }

        private bool DeleteEntity(Entity entity)
        {
            if (entity == null) return Logger.WarnReturn(false, "DeleteEntity(): entity == null");
            _entityDict.Remove(entity.Id);
            entity.OnDeallocate();
            return true;
        }
    }
}
