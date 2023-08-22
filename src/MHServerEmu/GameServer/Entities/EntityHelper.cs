using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.GameServer.Common;

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
    }
}
