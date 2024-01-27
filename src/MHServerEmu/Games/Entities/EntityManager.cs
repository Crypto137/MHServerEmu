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

        public Transition Waypoint { get; }

        public EntityManager(Game game)
        {
            _game = game;

            // Initialize a waypoint entity from hardcoded data
            ByteString waypointBaseData = "200C839F01200020".ToByteString();
            ByteString waypointArchiveData = "20F4C10206000000CD80018880FCFF99BF968110CCC00202CC800302CD40D58280DE868098044DA1A1A4FE0399C00183B8030000000000".ToByteString();
            Waypoint = new(new(waypointBaseData), waypointArchiveData);

            // minihack: force default player and avatar entity message initialization on construction
            // so that there isn't a lag when a player logs in for the first time after the server starts
            bool playerMessageIsEmpty = PlayerMessage == null;
            bool avatarMessagesIsEmpty = AvatarMessages == null;
        }

        public WorldEntity CreateWorldEntity(ulong regionId, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            int health, int mapAreaId, int healthMaxOther, int mapCellId, PrototypeId contextAreaRef, bool requiresEnterGameWorld, bool OverrideSnapToFloor = false)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            WorldEntity worldEntity = new(baseData, _game.CurrentRepId, position, health, mapAreaId, healthMaxOther, regionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);
            return worldEntity;
        }

        public WorldEntity CreateWorldEntityEnemy(ulong regionId, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            int health, int mapAreaId, int healthMaxOther, int mapCellId, PrototypeId contextAreaRef, bool requiresEnterGameWorld,
            int CombatLevel, int CharacterLevel)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            WorldEntity worldEntity = new(baseData, _game.CurrentRepId, position, health, mapAreaId, healthMaxOther, regionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);

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

        public Transition SpawnDirectTeleport(PrototypeId regionPrototype, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            int mapAreaId, ulong regionId, int mapCellId, PrototypeId contextAreaRef, bool requiresEnterGameWorld,
            PrototypeId targetPrototype, bool OverrideSnapToFloor)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            var regionConnectionTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetPrototype);

            var cellAssetId = regionConnectionTarget.Cell;
            var cellPrototypeId = cellAssetId != AssetId.Invalid ? GameDatabase.GetPrototypeRefByName(GameDatabase.GetAssetName(cellAssetId)) : PrototypeId.Invalid;

            var targetRegion = regionConnectionTarget.Region;
            // Logger.Debug($"SpawnDirectTeleport {targetRegion}");
            if (targetRegion == 0) { // get Parent value
                var parentTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetPrototype);
                if (parentTarget != null) targetRegion = parentTarget.Region;
            }

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

            Transition transition = new(baseData, _game.CurrentRepId, regionId, mapAreaId, mapCellId, contextAreaRef, position, destination);
            transition.RegionId = regionId;
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
        // TODO: CreateEntity -> finalizeEntity -> worldEntity.EnterWorld -> _location.SetRegion( region )

        public static float GetEntityFloor(PrototypeId prototypeId)
        {
            var entity = GameDatabase.GetPrototype<WorldEntityPrototype>(prototypeId);
            if (entity.ParentDataRef == (PrototypeId)HardcodedBlueprintId.NPCTemplateHub)
                return 46f; // AgentUntargetableInvulnerable.WorldEntity.Bounds.CapsuleBounds.HeightFromCenter

            if (entity.Bounds == null)
                return GetEntityFloor(entity.ParentDataRef);

            var bounds = entity.Bounds;
            float height = 0f;
            if (bounds.ParentDataRef == (PrototypeId)HardcodedBlueprintId.BoxBounds || bounds.ParentDataRef == (PrototypeId)HardcodedBlueprintId.ObjectSmall)
                height = ((BoxBoundsPrototype)bounds).Height;
            else if (bounds.ParentDataRef == (PrototypeId)HardcodedBlueprintId.SphereBounds)
                height = ((SphereBoundsPrototype)bounds).Radius;
            else if (bounds.ParentDataRef == (PrototypeId)HardcodedBlueprintId.CapsuleBounds)
                height = ((CapsuleBoundsPrototype)bounds).HeightFromCenter * 2f;
            else Logger.Warn($"ReferenceType = {bounds.ParentDataRef}");

            return height / 2;
        }

        public static bool GetSnapToFloorOnSpawn(PrototypeId prototypeId)
        {
            if (prototypeId == (PrototypeId)HardcodedBlueprintId.ThrowableProp) return true;
            if (prototypeId == (PrototypeId)HardcodedBlueprintId.DestructibleProp) return true;
            var entity = GameDatabase.GetPrototype<WorldEntityPrototype>(prototypeId);
            return entity.SnapToFloorOnSpawn;
        }

        public void AddEntityMarker(CellPrototype cell, EntityMarkerPrototype entityMarker, Vector3 areaOrigin, Region region, PrototypeId area, int areaid, int cellId)
        {
            Vector3 entityPosition = entityMarker.Position + areaOrigin;

            PrototypeId proto = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
            bool entitySnapToFloor = GetSnapToFloorOnSpawn(proto);

            bool snapToFloor = (entityMarker.OverrideSnapToFloor == 1) ? (entityMarker.OverrideSnapToFloorValue == 1) : entitySnapToFloor;

            if (snapToFloor)
            {
                float projectHeight = RegionLocation.ProjectToFloor(cell, areaOrigin, entityMarker.Position);
                if (entityPosition.Z > projectHeight)
                    entityPosition.Z = projectHeight;
            }

            entityPosition.Z += GetEntityFloor(proto);

            CreateWorldEntity(
                region.Id, proto,
                entityPosition, entityMarker.Rotation,
                608, areaid, 608, cellId, area, false, snapToFloor != entitySnapToFloor);
        }
    }
}
