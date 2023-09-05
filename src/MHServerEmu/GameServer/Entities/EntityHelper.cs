using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Entities
{
    public static class EntityHelper
    {
        public static NetMessageEntityCreate GenerateEntityCreateMessage(ulong entityId, ulong prototypeId, Vector3 position, Vector3 orientation,
            ulong replicationId, int health, int mapAreaId, int healthMaxOther, ulong mapRegionId, int mapCellId, ulong contextAreaRef, bool requiresEnterGameWorld)
        {
            byte[] baseData = (requiresEnterGameWorld == false)
                ? new EntityCreateBaseData(entityId, prototypeId, position, orientation).Encode()
                : new EntityCreateBaseData(entityId, prototypeId, null, null).Encode();

            byte[] archiveData = new WorldEntity(replicationId, position, health, mapAreaId, healthMaxOther, mapRegionId, mapCellId, contextAreaRef).Encode();

            return NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(ByteString.CopyFrom(baseData))
                .SetArchiveData(ByteString.CopyFrom(archiveData))
                .Build();
        }
        public static NetMessageEntityCreate SpawnEntityEnemy(ulong entityId, ulong prototypeId, Vector3 position, Vector3 orientation,
            ulong replicationId, int health, int mapAreaId, int healthMaxOther, ulong mapRegionId, int mapCellId, ulong contextAreaRef, bool requiresEnterGameWorld,
            int CombatLevel, int CharacterLevel)
        {
            byte[] baseData = (requiresEnterGameWorld == false)
                ? new EntityCreateBaseData(entityId, prototypeId, position, orientation).Encode()
                : new EntityCreateBaseData(entityId, prototypeId, null, null).Encode();

            WorldEntity worldEntity = new(replicationId, position, health, mapAreaId, healthMaxOther, mapRegionId, mapCellId, contextAreaRef);

            worldEntity.Properties.Append(new(PropertyEnum.CharacterLevel, CharacterLevel)); 
            worldEntity.Properties.Append(new(PropertyEnum.CombatLevel, CombatLevel)); // zero effect

            byte[] archiveData = worldEntity.Encode();

            return NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(ByteString.CopyFrom(baseData))
                .SetArchiveData(ByteString.CopyFrom(archiveData))
                .Build();
        }
    }

}

