using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.Games.Entities
{
    public class EntityManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Hardcoded messages we use for loading
        private static readonly NetMessageEntityCreate PlayerMessage = PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityCreatePlayer.bin")[0]
            .Deserialize<NetMessageEntityCreate>();
        private static readonly NetMessageEntityCreate[] AvatarMessages = PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityCreateAvatars.bin")
            .Select(message => message.Deserialize<NetMessageEntityCreate>()).ToArray();

        private readonly Game _game;
        private readonly Dictionary<ulong, Entity> _entityDict = new();

        private ulong _nextEntityId = 1000;
        private ulong GetNextEntityId() { return _nextEntityId++; }
        public ulong PeekNextEntityId() { return _nextEntityId; }

        public EntityManager(Game game)
        {
            _game = game;

            // minihack: force default player and avatar entity message initialization on construction
            // so that there isn't a lag when a player logs in for the first time after the server starts
            bool playerMessageIsEmpty = PlayerMessage == null;
            bool avatarMessagesIsEmpty = AvatarMessages == null;
        }

        public WorldEntity CreateWorldEntity(Cell cell, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            int health, bool requiresEnterGameWorld, bool OverrideSnapToFloor = false)
        {
            if (cell == null) return default;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            ulong regionId = cell.GetRegion().Id;
            int healthMaxOther = health;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            WorldEntity worldEntity = new(baseData, _game.CurrentRepId, position, health, mapAreaId, healthMaxOther, regionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            worldEntity.EnterWorld(cell, position, orientation);
            _entityDict.Add(baseData.EntityId, worldEntity);
            return worldEntity;
        }

        public WorldEntity CreateWorldEntityEnemy(Cell cell, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            int health, bool requiresEnterGameWorld, int characterLevel)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            ulong regionId = cell.GetRegion().Id;
            int healthMaxOther = health;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            WorldEntity worldEntity = new(baseData, _game.CurrentRepId, position, health, mapAreaId, healthMaxOther, regionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);
            worldEntity.EnterWorld(cell, position, orientation);
            int combatLevel = characterLevel;
            worldEntity.Properties[PropertyEnum.CharacterLevel] = characterLevel;
            worldEntity.Properties[PropertyEnum.CombatLevel] = combatLevel;         // zero effect

            return worldEntity;
        }

        public WorldEntity CreateWorldEntityEmpty(ulong regionId, PrototypeId prototypeId, Vector3 position, Vector3 orientation)
        {
            EntityBaseData baseData = new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation);
            WorldEntity worldEntity = new(baseData, AoiNetworkPolicyValues.AoiChannel0, _game.CurrentRepId);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);
            return worldEntity;
        }

        public Item CreateInvItem(PrototypeId itemProto, InventoryLocation invLoc, PrototypeId rarity, int itemLevel, float itemVariation, int seed, AffixSpec[] affixSpec, bool isNewItem) {

            EntityBaseData baseData = new()
            {
                ReplicationPolicy = AoiNetworkPolicyValues.AoiChannel2,
                EntityId = GetNextEntityId(),
                PrototypeId = itemProto,
                FieldFlags = EntityCreateMessageFlags.HasInterestPolicies | EntityCreateMessageFlags.HasInvLoc,
                InterestPolicies = AoiNetworkPolicyValues.AoiChannel2,
                LocoFieldFlags = LocomotionMessageFlags.None,
                LocomotionState = new(0f),
                InvLoc = invLoc
            };

            if (isNewItem)
            {
                baseData.FieldFlags |= EntityCreateMessageFlags.HasInvLocPrev;
                baseData.InvLocPrev = new(0, PrototypeId.Invalid, 0xFFFFFFFF); // -1
            }                

            var defRank = (PrototypeId)15168672998566398820; // Popcorn           
            ItemSpec itemSpec = new(itemProto, rarity, itemLevel, 0, affixSpec, seed, 0);
            Item item = new(baseData, _game.CurrentRepId, defRank, itemLevel, rarity, itemVariation, itemSpec);
            _entityDict.Add(baseData.EntityId, item);
            return item;
        }

        public Transition SpawnTransitionMarker(Cell cell, TransitionPrototype transitionProto, Vector3 position, Vector3 orientation,
            bool requiresEnterGameWorld, bool OverrideSnapToFloor)
        {
            if (cell == null) return default;
            ulong regionId = cell.GetRegion().Id;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, null, null);

            Transition transition = new(baseData, _game.CurrentRepId, regionId, mapAreaId, mapCellId, contextAreaRef, position, null);
            transition.RegionId = regionId;
            transition.EnterWorld(cell, position, orientation);
            _entityDict.Add(baseData.EntityId, transition);

            return transition;
        }

        public Transition SpawnTargetTeleport(Cell cell, TransitionPrototype transitionProto, Vector3 position, Vector3 orientation,
            bool requiresEnterGameWorld, PrototypeId targetRef, bool OverrideSnapToFloor)
        {            
            if (cell == null) return default;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, null, null);

            var regionConnectionTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetRef);

            var cellAssetId = regionConnectionTarget.Cell;
            var cellPrototypeId = cellAssetId != AssetId.Invalid ? GameDatabase.GetDataRefByAsset(cellAssetId) : PrototypeId.Invalid;

            var targetRegionRef = regionConnectionTarget.Region;        

            Region region = cell.GetRegion();
            var targetRegion = GameDatabase.GetPrototype<RegionPrototype>(targetRegionRef);
            if (RegionPrototype.Equivalent(targetRegion, region.RegionPrototype)) targetRegionRef = (PrototypeId)region.PrototypeId;

            Destination destination = new()
            {
                Type = transitionProto.Type,
                Region = targetRegionRef,
                Area = regionConnectionTarget.Area,
                Cell = cellPrototypeId,
                Entity = regionConnectionTarget.Entity,
                NameId = regionConnectionTarget.Name,
                Target = targetRef
            };

            ulong regionId = region.Id;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            Transition transition = new(baseData, _game.CurrentRepId, regionId, mapAreaId, mapCellId, contextAreaRef, position, destination);
            transition.RegionId = regionId;
            transition.EnterWorld(cell, position, orientation);
            _entityDict.Add(baseData.EntityId, transition);

            return transition;
        }

        public bool DestroyEntity(ulong entityId)
        {
            if (_entityDict.TryGetValue(entityId, out _) == false)
            {
                Logger.Warn($"Failed to remove entity id {entityId}: entity does not exist");
                return false;
            }

            _entityDict.Remove(entityId);
            return true;
        }

        public Entity GetEntityById(ulong entityId) => _entityDict[entityId];
        public Entity GetEntityByPrototypeId(PrototypeId prototype) => _entityDict.Values.FirstOrDefault(entity => entity.BaseData.PrototypeId == prototype);
        public Entity GetEntityByPrototypeIdFromRegion(PrototypeId prototype, ulong regionId)
        {
            return _entityDict.Values.FirstOrDefault(entity => entity.BaseData.PrototypeId == prototype && entity.RegionId == regionId);
        }
        public Entity FindEntityByDestination(Destination destination, ulong regionId)
        {
            foreach (KeyValuePair<ulong, Entity> entity in _entityDict)
            {
                if (entity.Value.BaseData.PrototypeId == destination.Entity && entity.Value.RegionId == regionId)
                {
                    if (destination.Area == 0) return entity.Value;
                    PrototypeId area = entity.Value.Properties[PropertyEnum.ContextAreaRef];
                    if (area == destination.Area)
                        return entity.Value;
                }                
            }
            return null;
        }
        public Transition GetTransitionInRegion(Destination destination, ulong regionId)
        {
            PrototypeId areaRef = destination.Area;
            PrototypeId cellRef = destination.Cell;
            PrototypeId entityRef = destination.Entity;
            foreach (var entity in _entityDict.Values)
                if (entity.RegionId == regionId)
                {
                    if (entity is not Transition transition) continue;
                    if (areaRef != 0 && areaRef != (PrototypeId)transition.Location.Area.PrototypeId) continue;
                    if (cellRef != 0 && cellRef != transition.Location.Cell.PrototypeId) continue;
                    if (transition.BaseData.PrototypeId == entityRef)
                        return transition;
                }

            return default;
        }

        public bool TryGetEntityById(ulong entityId, out Entity entity) => _entityDict.TryGetValue(entityId, out entity);
        public ulong GetPropertyCollectionReplicationId(ulong entityId) => _entityDict[entityId].Properties.ReplicationId;
        public bool TryGetPropertyCollectionReplicationId(ulong entityId, out ulong replicationId)
        {
            if (_entityDict.TryGetValue(entityId, out Entity entity))
            {
                replicationId = entity.Properties.ReplicationId;
                return true;
            }

            replicationId = 0;
            return false;
        }

        public Player GetDefaultPlayerEntity()
        {
            EntityBaseData baseData = new(PlayerMessage.BaseData);
            return new(baseData, PlayerMessage.ArchiveData);
        }

        public Avatar[] GetDefaultAvatarEntities()
        {
            Avatar[] avatars = new Avatar[AvatarMessages.Length];

            for (int i = 0; i < avatars.Length; i++)
            {
                EntityBaseData baseData = new(AvatarMessages[i].BaseData);
                avatars[i] = new(baseData, AvatarMessages[i].ArchiveData);
            }

            return avatars;
        }

        public IEnumerable<Entity> GetEntities()
        {
            foreach (var entity in _entityDict.Values)
                yield return entity;
        }

        public IEnumerable<Entity> GetEntities(Cell cell)
        {
            foreach (var entity in _entityDict.Values)
                if (entity is WorldEntity worldEntity && worldEntity.Cell == cell)
                    yield return entity;
        }

        public IEnumerable<Entity> GetEntities(Region region)
        {
            foreach (var entity in _entityDict.Values)
                if (entity is WorldEntity worldEntity && worldEntity.Region == region)
                    yield return entity;
        }

        // TODO: CreateEntity -> finalizeEntity -> worldEntity.EnterWorld -> _location.SetRegion( region )

        #region OldSpawnSystem
 
        public void AddEntityMarker(Cell cell, EntityMarkerPrototype entityMarker)
        {
            CellPrototype cellProto = cell.CellProto;

            Vector3 entityPosition = cell.CalcMarkerPosition(entityMarker.Position);

            PrototypeId protoRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
            var entity = GameDatabase.GetPrototype<WorldEntityPrototype>(protoRef);

            bool? snapToFloor = SpawnSpec.SnapToFloorConvert(entityMarker.OverrideSnapToFloor, entityMarker.OverrideSnapToFloorValue);
            snapToFloor ??= entity.SnapToFloorOnSpawn;       
            bool overrideSnap = snapToFloor != entity.SnapToFloorOnSpawn;
            if (snapToFloor == true) // Fix Boxes in Axis Raid
            {
                float projectHeight = cell.RegionBounds.Center.Z + RegionLocation.ProjectToFloor(cellProto, entityMarker.Position);
                if (entityPosition.Z > projectHeight) 
                    entityPosition.Z = projectHeight;
            }
            entityPosition.Z += entity.Bounds.GetBoundHalfHeight(); 
                  
            CreateWorldEntity(cell, protoRef, entityPosition, entityMarker.Rotation, 608, false, overrideSnap);
        }

        public void AddTeleports(Cell cell, Area entryArea, ConnectionNodeList targets)
        {
            PrototypeId area = (PrototypeId)entryArea.PrototypeId;

            foreach (var marker in cell.CellProto.InitializeSet.Markers)
            {
                if (marker is EntityMarkerPrototype portal)
                {  
                    PrototypeId protoId = GameDatabase.GetDataRefByPrototypeGuid(portal.EntityGuid);
                    Prototype entity = GameDatabase.GetPrototype<Prototype>(protoId);
                    bool snap = portal.OverrideSnapToFloor;
                    if (entity is TransitionPrototype transition)
                    {
                        Vector3 position = cell.CalcMarkerPosition(portal.Position);
                        position.Z += transition.Bounds.GetBoundHalfHeight();

                        //Logger.Debug($"[{transition.Type}] {portal.LastKnownEntityName} [{protoId}]");
                        if (transition.Waypoint != 0)
                        {
                            var waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(transition.Waypoint);
                            SpawnTargetTeleport(cell, transition, position, portal.Rotation, false, waypointProto.Destination, snap);
                        }
                        else
                        {
                            TargetObject node = RegionTransition.GetTargetNode(targets, area, cell.PrototypeId, portal.EntityGuid);
                            if (node != null)
                                SpawnTargetTeleport(cell, transition, position, portal.Rotation, false, node.TargetId, snap);
                            else
                                SpawnTransitionMarker(cell, transition, position, portal.Rotation, false, snap);   
                        }      
                    }

                }
            }
        }

        #endregion
    }
}
