using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Physics;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
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

    public readonly struct DestroyEntityEvent(Entity entity) : IGameEventData
    {
        public readonly Entity Entity = entity;
    }

    public class EntityManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // NOTE: We can use machine id argument if we ever implement multi-GIS architecture
        private static readonly IdGenerator EntityDbGuidGenerator = new(IdType.Entity, 0);

        private readonly Game _game;

        private readonly Dictionary<ulong, Entity> _entityDict = new();
        private readonly Dictionary<ulong, Entity> _entityDbGuidDict = new();
        private readonly HashSet<ulong> _entitiesPendingCondemnedPowerDeletion = new();

        private readonly LinkedList<ulong> _entitiesPendingDestruction = new();

        public Event<DestroyEntityEvent> DestroyEntityEvent = new();

        private ulong _nextEntityId = 1;
        private ulong GetNextEntityId() { return _nextEntityId++; }

        public int EntityCount { get => _entityDict.Count; }
        public int PlayerCount { get => Players.Count; }

        public bool IsDestroyingAllEntities { get; private set; } = false;

        // NOTE: We break encapsulation here to allow the PlayerIterator to access this HashSet's struct enumerator and avoid boxing.
        // As an alternative, we could also move PlayerIterator to EntityManager as a nested struct.
        public HashSet<Player> Players { get; } = new();

        public PhysicsManager PhysicsManager { get; set; }
        public EntityInvasiveCollection AllEntities { get; private set; }
        public EntityInvasiveCollection SimulatedEntities { get; private set; }
        public EntityInvasiveCollection LocomotionEntities { get; private set; }

        public bool IsAIEnabled { get; private set; } = true;

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

            // Pre-process settings
            PrototypeId entityProtoRef = settings.EntityRef;

            if (entityProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn<Entity>(null, "CreateEntity(): Invalid prototype ref provided in settings");

            if (DataDirectory.Instance.PrototypeIsA<EntityPrototype>(entityProtoRef) == false)
                return Logger.WarnReturn<Entity>(null, $"CreateEntity(): {entityProtoRef} is not a valid entity prototype ref");

            if (settings.Id == 0)
                settings.Id = GetNextEntityId();

            if (settings.DbGuid == 0)
            {
                if (DataDirectory.Instance.GetPrototypeClassType(entityProtoRef) == typeof(PlayerPrototype))
                    return Logger.WarnReturn<Entity>(null, "CreateEntity(): Player entities require a valid database guid generated during account creation");

                settings.DbGuid = EntityDbGuidGenerator.Generate();
                //Logger.Debug($"CreateEntity(): Generated database guid 0x{settings.DbGuid:X} for entity {entityProtoRef.GetName()}");
            }

            // Newly created entities are always new on server and not something that simply entered a client's AOI
            settings.OptionFlags |= EntitySettingsOptionFlags.IsNewOnServer;

            // Create the entity
            Entity entity;

            // If the requested id is already used by an entity that is pending deletion, finish its deletion immediately
            Entity destroyedEntity = GetDestroyedEntity<Entity>(settings.Id);
            if (destroyedEntity != null)
                ProcessPendingDestroyImmediate(destroyedEntity);

            // Check for id collisions
            entity = GetEntity(settings.Id, GetEntityFlags.UnpackedOnly);
            if (entity != null)
                return Logger.WarnReturn<Entity>(null, $"CreateEntity(): Collision in entity id, existing entity found: {entity}");

            // Server entities should always have valid database guids, so we can omit the client-side check for invalid db guid here
            entity = GetEntityByDbGuid(settings.DbGuid, GetEntityFlags.UnpackedOnly);
            if (entity != null)
                return Logger.WarnReturn<Entity>(null, $"CreateEntity(): Collision in entity dbid, existing entity found: {entity}");

            entity = _game.AllocateEntity(settings.EntityRef);

            entity.ModifyCollectionMembership(EntityCollection.All, true);

            _entityDict.Add(settings.Id, entity);
            if (settings.DbGuid != 0)
                _entityDbGuidDict[settings.DbGuid] = entity;

            // Set status flags

            // DBOps - database operations?
            if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.SuspendDBOpsWhileCreating))
                entity.SetStatus(EntityStatus.DisableDBOps, true);

            // Items seem to ignore binding checks during creation
            entity.SetStatus(EntityStatus.SkipItemBindingCheck, true);

            // Set for client-only entities (should be irrelevant for our purposes)
            if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.ClientOnly))
                entity.SetStatus(EntityStatus.ClientOnly, true);

            // Deserialization flag - currently unused until we implement persistent archives
            if (settings.ArchiveData != null)
                entity.SetStatus(EntityStatus.HasArchiveData, true);

            // Set for avatars, seems to be used for interaction with UE3 (client only?)
            if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.DeferAdapterChanges))
                entity.SetStatus(EntityStatus.DeferAdapterChanges, true);

            bool initSuccess = entity.PreInitialize(settings);
            initSuccess &= entity.Initialize(settings);
            
            // Deserialize archive data if provided
            if (settings.ArchiveSerializeType != ArchiveSerializeType.Invalid && settings.ArchiveData?.Length > 0)
            {
                using (Archive archive = new(settings.ArchiveSerializeType, settings.ArchiveData))
                {
                    Serializer.Transfer(archive, ref entity);
                    // TODO: entity.Bind() - do we need this server-side?
                    entity.OnUnpackComplete(archive);
                }
            }

            entity.ApplyInitialReplicationState(ref settings);

            // Finish deserialization
            entity.SetStatus(EntityStatus.HasArchiveData, false);

            // TODO: entity.AdjustDifficulty()

            initSuccess &= FinalizeEntity(entity, settings);

            if (initSuccess == false)
            {
                // Entity initialization failed
                Logger.Warn($"CreateEntity(): Initialization failed for entity [{entity}]");
                entity.Destroy();
                return null;
            }

            // Resume DB ops
            if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.SuspendDBOpsWhileCreating))
            {
                if (entity.TestStatus(EntityStatus.DisableDBOps) == false)
                    Logger.Warn($"CreateEntity(): Expected status set to disable db ops on {entity}");
                entity.SetStatus(EntityStatus.DisableDBOps, false);
            }

            // Re-enable item binding
            entity.SetStatus(EntityStatus.SkipItemBindingCheck, false);

            settings.Results.Entity = entity;
            return entity;
        }

        private bool FinalizeEntity(Entity entity, EntitySettings settings)
        {
            if (entity == null) return Logger.WarnReturn(false, "FinalizeEntity(): entity == null");

            entity.OnPostInit(settings);

            // Add the new entity to an inventory if there is a location specified
            InventoryLocation invLoc = settings.InventoryLocation;
            if (invLoc != null && invLoc.ContainerId != Entity.InvalidId)
            {
                ulong ownerId = invLoc.ContainerId;
                PrototypeId ownerInventoryRef = invLoc.InventoryRef;

                settings.Results.InventoryResult = InventoryResult.UnknownFailure;

                // Validate inventory location
                if (ownerInventoryRef == PrototypeId.Invalid)
                    return Logger.WarnReturn(false, $"FinalizeEntity(): Invalid owner invRef during create. invLoc={invLoc}, entity={entity}");

                Entity owner = GetEntity<Entity>(settings.InventoryLocation.ContainerId);
                if (owner == null)
                    return Logger.WarnReturn(false, $"FinalizeEntity(): Unable to find owner entity with id {ownerId} for placement of entity {entity} into invLoc {invLoc}, maybe it despawned?");

                Inventory ownerInventory = owner.GetInventoryByRef(ownerInventoryRef);
                if (ownerInventory == null)
                    return Logger.WarnReturn(false, $"FinalizeEntity(): Unable to find inventory {ownerInventory} in owner entity {owner} to put entity {entity} in it");

                // Attempt to put the entity in the inventory it belongs to
                settings.Results.InventoryResult = Inventory.ChangeEntityInventoryLocationOnCreate(entity, ownerInventory, invLoc.Slot,
                    settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.IsPacked), settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.DoNotAllowStackingOnCreate) == false,
                    settings.InventoryLocationPrevious);

                // Report error if something went wrong
                if (settings.Results.InventoryResult != InventoryResult.Success)
                {
                    if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.LogInventoryErrors))
                        Logger.Warn($"CreateEntity(): Unable to add entity {entity} at invLoc {invLoc} of owner entity {owner} (error: {settings.Results.InventoryResult})");

                    return false;
                }

                if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.ClientOnly) && entity.IsDestroyed)
                    return true;
            }

            if (settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.EnterGame))
            {
                var owner = entity.GetOwner();
                if (owner == null || owner.IsInGame)
                    entity.EnterGame(settings);
            }

            if (entity is WorldEntity worldEntity)
            {
                worldEntity.RegisterActions(settings.Actions);

                if (settings.RegionId != 0)
                {
                    Region region = _game.RegionManager.GetRegion(settings.RegionId);
                    var position = settings.Position;
                    if (worldEntity.ShouldSnapToFloorOnSpawn)
                    {
                        position = RegionLocation.ProjectToFloor(region, position);
                        position = worldEntity.FloorToCenter(position);
                    }

                    // NOTE: While this is not client-accurate, if we don't clean up the entity here, it will stay in the message handler collection and cause a memory leak
                    if (worldEntity.EnterWorld(region, position, settings.Orientation, settings) == false)
                        return false;
                }
            }

            return true;
        }

        public bool DestroyEntity(Entity entity)
        {
            if (entity == null) return Logger.WarnReturn(false, "DestroyEntity(): entity == null");

            if (entity.TestStatus(EntityStatus.PendingDestroy)) return Logger.WarnReturn(false,
                $"DestroyEntity(): Entity already marked as PendingDestroy, this means that something was using an entity reference even though it was pending destroy which needs to be fixed! Entity: {entity}");

            if (entity.TestStatus(EntityStatus.Destroyed)) return Logger.WarnReturn(false,
                $"DestroyEntity(): Entity already marked as Destroy, this means that something was using an entity reference even though it was destroyed which needs to be fixed! Entity: {entity}");

            entity.SetStatus(EntityStatus.PendingDestroy, true);

            // invoke destroyed event
            DestroyEntityEvent.Invoke(new(entity));

            // Destroy entities belonging to this entity
            entity.DestroyContained();

            // Remove this entity from the inventory it is in
            if (entity.InventoryLocation.IsValid)
                entity.ChangeInventoryLocation(null);

            // Finish destruction
            entity.SetStatus(EntityStatus.PendingDestroy, false);
            entity.SetStatus(EntityStatus.Destroyed, true);

            // Enqueue entity for deletion at the end of the frame
            _entitiesPendingDestruction.AddLast(GetDestroyListNode(entity.Id));    

            // Remove entity from the game
            entity.ExitGame();

            // Remove DbId lookup
            if (entity.DatabaseUniqueId != 0)
                _entityDbGuidDict.Remove(entity.DatabaseUniqueId);

            return true;
        }

        public void DestroyAllEntities()
        {
            IsDestroyingAllEntities = true;

            bool removed;
            int loopGuard = 100;

            do
            {
                removed = false;
                foreach (Entity entity in _entityDict.Values)
                {
                    if (entity.TestStatus(EntityStatus.Destroyed))
                        continue;

                    if (entity.IsRootOwner == false)
                        continue;

                    entity.Destroy();
                    removed = true;
                }
            } while (removed && (loopGuard-- > 0));

            if (loopGuard == 0)
            {
                Logger.Warn("DestroyAllEntities(): loopGuard == 0");
                foreach (Entity entity in _entityDict.Values)
                {
                    if (entity.TestStatus(EntityStatus.Destroyed) == false)
                        Logger.Warn($"DestroyAllEntities(): Entity is not 'Destroyed' after DestroyAllEntities() {entity}");
                }
            }

            IsDestroyingAllEntities = false;
        }

        public bool AddPlayer(Player player)
        {
            if (player == null) return Logger.WarnReturn(false, "AddPlayer(): player == null");
            bool playerAdded = Players.Add(player);
            if (playerAdded == false) Logger.Warn($"AddPlayer(): Failed to add player {player}");
            return playerAdded;
        }

        public bool RemovePlayer(Player player)
        {
            if (player == null) return Logger.WarnReturn(false, "RemovePlayer(): player == null");
            bool playerRemoved = Players.Remove(player);
            if (playerRemoved == false) Logger.Warn($"RemovePlayer(): Failed to remove player {player}");
            return playerRemoved;
        }

        public void RegisterEntityForCondemnedPowerDeletion(ulong entityId)
        {
            _entitiesPendingCondemnedPowerDeletion.Add(entityId);
        }

        public T GetEntity<T>(ulong entityId, GetEntityFlags flags = GetEntityFlags.None) where T : Entity
        {
            if (entityId == Entity.InvalidId) return null;

            // Prevent destroyed entities from being accessed externally.
            return GetEntity(entityId, flags & ~GetEntityFlags.DestroyedOnly) as T;
        }

        public T GetEntityByDbGuid<T>(ulong dbGuid, GetEntityFlags flags = GetEntityFlags.None) where T: Entity
        {
            // Same as above, but for DbGuid
            if (dbGuid == 0) return Logger.WarnReturn<T>(null, "GetEntityByDbGuid(): dbGuid == 0");

            return GetEntityByDbGuid(dbGuid, flags & ~GetEntityFlags.DestroyedOnly) as T;
        }

        public bool IsEntityArchived(ulong entityId)
        {
            // TODO?
            return false;
        }

        public IEnumerable<Entity> IterateEntities()
        {
            foreach (var entity in _entityDict.Values)
                yield return entity;
        }

        public IEnumerable<Entity> IterateEntities(Area area)
        {
            foreach (var entity in _entityDict.Values)
                if (entity is WorldEntity worldEntity && worldEntity.Area == area)
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
            ProcessCondemnedPowerList();
            ProcessDestroyed();
        }

        private Entity GetEntity(ulong entityId, GetEntityFlags flags)
        {
            // We should have a valid entity id by this point
            if (entityId == Entity.InvalidId) return Logger.WarnReturn<Entity>(null, "GetEntity(): entityId == Entity.InvalidId");

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

        private T GetDestroyedEntity<T>(ulong entityId) where T : Entity
        {
            return GetEntity(entityId, GetEntityFlags.DestroyedOnly) as T;
        }

        private bool ValidateEntityForGet(Entity entity, GetEntityFlags flags)
        {
            if (entity == null) return false;
            return entity.TestStatus(EntityStatus.Destroyed) == flags.HasFlag(GetEntityFlags.DestroyedOnly);
        }

        private void ProcessCondemnedPowerList()
        {
            foreach (ulong entityId in _entitiesPendingCondemnedPowerDeletion)
            {
                if (_entityDict.TryGetValue(entityId, out Entity entity) == false)
                    continue;

                if (entity is not WorldEntity worldEntity)
                {
                    Logger.Warn("ProcessCondemnedPowerList(): entity is not WorldEntity");
                    continue;
                }

                PowerCollection powerCollection = worldEntity.PowerCollection;
                if (powerCollection == null)
                {
                    Logger.Warn("ProcessCondemnedPowerList(): powerCollection == null");
                    continue;
                }

                powerCollection.DeleteCondemnedPowers();
            }

            _entitiesPendingCondemnedPowerDeletion.Clear();
        }

        private static LinkedListNode<ulong> GetDestroyListNode(ulong entityId)
        {
            return EntityDestroyListNodePool.Instance.Get(entityId);
        }

        private static void ReturnDestroyListNode(LinkedListNode<ulong> destroyNode)
        {
            EntityDestroyListNodePool.Instance.Return(destroyNode);
        }

        private bool ProcessDestroyed()
        {
            if (_game == null) return Logger.WarnReturn(false, "ProcessDestroyed(): _game == null");

            // Delete all destroyed entities
            while (_entitiesPendingDestruction.Count > 0)
            {
                LinkedListNode<ulong> deleteNode = _entitiesPendingDestruction.First;
                ulong entityId = deleteNode.Value;

                if (_entityDict.TryGetValue(entityId, out Entity entity))
                    DeleteEntity(entity);
                else
                    Logger.Warn($"ProcessDestroyed(): Failed to get entity for enqueued id {entityId}");

                _entitiesPendingDestruction.RemoveFirst();
                ReturnDestroyListNode(deleteNode);
            }

            return true;
        }

        private bool ProcessPendingDestroyImmediate(Entity destroyedEntity)
        {
            if (destroyedEntity == null) return Logger.WarnReturn(false, "ProcessPendingDestroyImmediate(): destroyedEntity == null");

            LinkedListNode<ulong> destroyNode = _entitiesPendingDestruction.Find(destroyedEntity.Id);
            if (destroyNode == null)
                return Logger.WarnReturn(false, $"ProcessPendingDestroyImmediate(): Entity {destroyedEntity} is not found in the pending destruction list");

            // Delete the entity manually and remove it from the list
            DeleteEntity(destroyedEntity);
            _entitiesPendingDestruction.Remove(destroyNode);
            ReturnDestroyListNode(destroyNode);

            return true;
        }

        private bool DeleteEntity(Entity entity)
        {
            if (entity == null) return Logger.WarnReturn(false, "DeleteEntity(): entity == null");
            _entityDict.Remove(entity.Id);
            entity.OnDeallocate();
            return true;
        }

        public void EnableAI(bool enable)
        {
            if (IsAIEnabled == enable) return;
            IsAIEnabled = enable;

            if (enable)
                foreach (var entity in SimulatedEntities.Iterate())
                    if (entity is Agent agent) agent.AIController?.SetIsEnabled(true);

            foreach (var entity in _entityDict.Values)
                if (entity is WorldEntity worldEntity && entity is not Missile && worldEntity.IsInWorld)
                    worldEntity.Locomotor?.Stop();
        }
    }
}
