using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Entities
{
    public class EntityManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Hardcoded messages we use for loading
        private static readonly NetMessageEntityCreate PlayerMessage = NetMessageEntityCreate.ParseFrom(PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityCreatePlayer.bin")[0].Payload);
        private static readonly NetMessageEntityCreate[] AvatarMessages = PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityCreateAvatars.bin").Select(message => NetMessageEntityCreate.ParseFrom(message.Payload)).ToArray();

        private readonly Dictionary<ulong, Entity> _entityDict = new();

        private ulong _nextEntityId = 1000;
        private ulong _nextReplicationId = 50000;

        public WorldEntity Waypoint { get; }

        public EntityManager()
        {
            // Initialize a waypoint entity
            EntityBaseData waypointBaseData = new(Convert.FromHexString("200C839F01200020"));
            Waypoint = new(waypointBaseData, Convert.FromHexString("20F4C10206000000CD80018880FCFF99BF968110CCC00202CC800302CD40D58280DE868098044DA1A1A4FE0399C00183B8030000000000"));

            // minihack: force default player and avatar entity message initialization on construction
            // so that there isn't a lag when a player logs in for the first time after the server starts
            bool playerMessageIsEmpty = PlayerMessage == null;
            bool avatarMessagesIsEmpty = AvatarMessages == null;
        }

        public WorldEntity CreateWorldEntity(ulong regionId, ulong prototypeId, Vector3 position, Vector3 orientation,
            int health, int mapAreaId, int healthMaxOther, ulong mapRegionId, int mapCellId, ulong contextAreaRef, bool requiresEnterGameWorld)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(_nextEntityId++, prototypeId, position, orientation)
                : new EntityBaseData(_nextEntityId++, prototypeId, null, null);

            WorldEntity worldEntity = new(baseData, _nextReplicationId++, position, health, mapAreaId, healthMaxOther, mapRegionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);
            return worldEntity;
        }

        public WorldEntity CreateWorldEntityEnemy(ulong regionId, ulong prototypeId, Vector3 position, Vector3 orientation,
            int health, int mapAreaId, int healthMaxOther, ulong mapRegionId, int mapCellId, ulong contextAreaRef, bool requiresEnterGameWorld,
            int CombatLevel, int CharacterLevel)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(_nextEntityId++, prototypeId, position, orientation)
                : new EntityBaseData(_nextEntityId++, prototypeId, null, null);

            WorldEntity worldEntity = new(baseData, _nextReplicationId++, position, health, mapAreaId, healthMaxOther, mapRegionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);

            worldEntity.PropertyCollection.List.Add(new(PropertyEnum.CharacterLevel, CharacterLevel));
            worldEntity.PropertyCollection.List.Add(new(PropertyEnum.CombatLevel, CombatLevel)); // zero effect

            return worldEntity;
        }

        public WorldEntity CreateWorldEntityEmpty(ulong regionId, ulong prototypeId, Vector3 position, Vector3 orientation)
        {
            EntityBaseData baseData = new EntityBaseData(_nextEntityId++, prototypeId, position, orientation);
            WorldEntity worldEntity = new(baseData, 1, _nextReplicationId++);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);
            return worldEntity;
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
            EntityBaseData baseData = new(PlayerMessage.BaseData.ToByteArray());
            return new(baseData, PlayerMessage.ArchiveData.ToByteArray());
        }

        public Avatar[] GetDefaultAvatarEntities()
        {
            Avatar[] avatars = new Avatar[AvatarMessages.Length];

            for (int i = 0; i < avatars.Length; i++)
            {
                EntityBaseData baseData = new(AvatarMessages[i].BaseData.ToByteArray());
                avatars[i] = new(baseData, AvatarMessages[i].ArchiveData.ToByteArray());
            }

            return avatars;
        }
    }
}
