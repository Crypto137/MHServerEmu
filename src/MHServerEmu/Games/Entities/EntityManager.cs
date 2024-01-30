using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
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
            int health, bool requiresEnterGameWorld, int CharacterLevel)
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
            int CombatLevel = CharacterLevel;
            worldEntity.PropertyCollection.List.Add(new(PropertyEnum.CharacterLevel, CharacterLevel));
            worldEntity.PropertyCollection.List.Add(new(PropertyEnum.CombatLevel, CombatLevel)); // zero effect

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

        public Transition SpawnWaypoint(Cell cell, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            bool requiresEnterGameWorld, bool OverrideSnapToFloor)
        {
            if (cell == null) return default;
            ulong regionId = cell.GetRegion().Id;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            Transition transition = new(baseData, _game.CurrentRepId, regionId, mapAreaId, mapCellId, contextAreaRef, position, null);
            transition.RegionId = regionId;
            transition.EnterWorld(cell, position, orientation);
            _entityDict.Add(baseData.EntityId, transition);

            return transition;
        }

        public Transition SpawnDirectTeleport(Cell cell, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            bool requiresEnterGameWorld, PrototypeId targetPrototype, bool OverrideSnapToFloor)
        {            
            if (cell == null) return default;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            var regionConnectionTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetPrototype);

            var cellAssetId = regionConnectionTarget.Cell;
            var cellPrototypeId = cellAssetId != AssetId.Invalid ? GameDatabase.GetDataRefByAsset(cellAssetId) : PrototypeId.Invalid;

            var targetRegion = regionConnectionTarget.Region;
            // Logger.Debug($"SpawnDirectTeleport {targetRegion}");
            if (targetRegion == 0) { // get Parent value
                var parentTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetPrototype);
                if (parentTarget != null) targetRegion = parentTarget.Region;
            }

            Region region = cell.GetRegion();
            PrototypeId regionPrototype = (PrototypeId)region.PrototypeId;

            if (RegionManager.IsRegionAvailable((RegionPrototypeId)targetRegion) == false) // TODO: change region test
                targetRegion = regionPrototype;

            int type = 1; // default teleport
            if (targetRegion != regionPrototype) type = 2; // region teleport

            Destination destination = new()
            {
                Type = type,
                Region = targetRegion,
                Area = regionConnectionTarget.Area,
                Cell = cellPrototypeId,
                Entity = regionConnectionTarget.Entity,
                Name = "",
                NameId = regionConnectionTarget.Name,
                Target = targetPrototype,
                Position = new()
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
                    Property property = entity.Value.PropertyCollection.GetPropertyByEnum(PropertyEnum.ContextAreaRef);
                    var area = (PrototypeId)property.Value.Get();
                    if (area == destination.Area)
                        return entity.Value;
                }                
            }
            return null;
        }

        public bool TryGetEntityById(ulong entityId, out Entity entity) => _entityDict.TryGetValue(entityId, out entity);
        public ulong GetPropertyCollectionReplicationId(ulong entityId) => _entityDict[entityId].PropertyCollection.ReplicationId;
        public bool TryGetPropertyCollectionReplicationId(ulong entityId, out ulong replicationId)
        {
            if (_entityDict.TryGetValue(entityId, out Entity entity))
            {
                replicationId = entity.PropertyCollection.ReplicationId;
                return true;
            }

            replicationId = 0;
            return false;
        }

        public WorldEntity[] GetWorldEntitiesForRegion(ulong regionId)
        {
            IEnumerable<Entity> entities = _entityDict.Values.Where(entity => entity.RegionId == regionId).ToArray();
            return entities.Select(entity => (WorldEntity)entity).ToArray();
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

        public Transition GetTransitionInRegion(PrototypeId targetRef, Region region, PrototypeId areaRef, PrototypeId cellRef)
        {
            ulong regionId = region.Id;
            foreach (var entity in _entityDict.Values)
                if (entity.RegionId == regionId)
                {
                    if (entity is not Transition transition) continue;
                    if (areaRef != 0 && areaRef != (PrototypeId)transition.Location.Area.PrototypeId) continue;
                    if (cellRef != 0 && cellRef != transition.Location.Cell.PrototypeId) continue;
                    if (transition.BaseData.PrototypeId == targetRef) return transition;
                    if (transition.TransitionPrototype.Waypoint == targetRef) return transition;
                }
            return default;
        }

        // TODO: CreateEntity -> finalizeEntity -> worldEntity.EnterWorld -> _location.SetRegion( region )

        #region OldSpawnSystem

        public static float GetEntityFloor(PrototypeId prototypeId)
        {
            var entity = GameDatabase.GetPrototype<WorldEntityPrototype>(prototypeId);
            return entity.Bounds.GetBoundHalfHeight();
        }

        public static bool GetSnapToFloorOnSpawn(PrototypeId prototypeId)
        {
            if (prototypeId == (PrototypeId)HardcodedBlueprintId.ThrowableProp) return true;
            if (prototypeId == (PrototypeId)HardcodedBlueprintId.DestructibleProp) return true;
            var entity = GameDatabase.GetPrototype<WorldEntityPrototype>(prototypeId);
            return entity.SnapToFloorOnSpawn;
        }
 
        public void AddEntityMarker(Cell cell, EntityMarkerPrototype entityMarker)
        {
            CellPrototype cellProto = cell.CellProto;

            Vector3 entityPosition = cell.CalcMarkerPosition(entityMarker.Position);

            PrototypeId proto = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
            bool entitySnapToFloor = GetSnapToFloorOnSpawn(proto);

            bool snapToFloor = (entityMarker.OverrideSnapToFloor == 1) ? (entityMarker.OverrideSnapToFloorValue == 1) : entitySnapToFloor;

            if (snapToFloor)
            {
                float projectHeight = cell.RegionBounds.Center.Z + RegionLocation.ProjectToFloor(cellProto, entityMarker.Position);
                if (entityPosition.Z > projectHeight)
                    entityPosition.Z = projectHeight;
            }

            entityPosition.Z += GetEntityFloor(proto);
            CreateWorldEntity(cell, proto, entityPosition, entityMarker.Rotation, 608, false, snapToFloor != entitySnapToFloor);
        }



        public void MarkersAdd(Cell cell, bool addProp = false)
        {            
            foreach (var markerProto in cell.CellProto.MarkerSet.Markers)
            {
                if (markerProto is EntityMarkerPrototype entityMarker)
                {
                    string marker = entityMarker.LastKnownEntityName;

                    if (marker.Contains("GambitMTXStore")) continue; // Invisible
                    if (marker.Contains("CosmicEventVendor")) continue; // Invisible

                    if (marker.Contains("Entity/Characters/") || (addProp && marker.Contains("Entity/Props/")))
                    {
                        AddEntityMarker(cell, entityMarker);
                    }
                }
            }
        }

        public void AddTeleports(Cell cell, Area entryArea, ConnectionNodeList targets)
        {
            PrototypeId area = (PrototypeId)entryArea.PrototypeId;

            TargetObject GetTargetNode(PrototypeId area, PrototypeId cell, PrototypeGuid entity)
            {
                foreach (var targetNode in targets)
                {
                    if (targetNode.Area == area && targetNode.Entity == entity)
                    {
                        if (targetNode.Cell == 0) return targetNode;
                        else if (targetNode.Cell == cell) return targetNode;
                    }
                }
                return null;
            }

            foreach (var marker in cell.CellProto.InitializeSet.Markers)
            {
                if (marker is EntityMarkerPrototype portal)
                {  
                    PrototypeId protoId = GameDatabase.GetDataRefByPrototypeGuid(portal.EntityGuid);
                    TargetObject node = GetTargetNode(area, cell.PrototypeId, portal.EntityGuid);
                    if (node != null)
                    {
                        Vector3 position = cell.CalcMarkerPosition(portal.Position);
                        position.Z += GetEntityFloor(protoId);

                        SpawnDirectTeleport( cell, protoId, position, portal.Rotation, false, node.TargetId, portal.OverrideSnapToFloor > 0);
                    }
                    
                    if (portal.LastKnownEntityName.Contains("Waypoints/"))
                    {
                       // Logger.Debug($"[TP] {portal.LastKnownEntityName} [{protoId}]");
                        Vector3 position = cell.CalcMarkerPosition(portal.Position);
                        SpawnWaypoint(cell, protoId, position, portal.Rotation, false, portal.OverrideSnapToFloor > 0);
                    }
                }
            }
        }

        public void GenerateEntities(Region region, ConnectionNodeList targets, bool addMarkers, bool addProp)
        {
            foreach (Area entryArea in region.AreaList)
            {                
                foreach (var cell in entryArea.CellList)
                {
                    if (addMarkers)
                        MarkersAdd(cell, addProp);
                        AddTeleports(cell, entryArea, targets);
                }
            }
        }

        #endregion
    }
}
