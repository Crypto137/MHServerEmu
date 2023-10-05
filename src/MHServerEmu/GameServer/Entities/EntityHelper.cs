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
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(entityId, prototypeId, position, orientation)
                : new EntityBaseData(entityId, prototypeId, null, null);

            WorldEntity worldEntity = new WorldEntity(baseData, replicationId, position, health, mapAreaId, healthMaxOther, mapRegionId, mapCellId, contextAreaRef);
            return worldEntity.ToNetMessageEntityCreate();
        }

        public static NetMessageEntityCreate SpawnEntityEnemy(ulong entityId, ulong prototypeId, Vector3 position, Vector3 orientation,
            ulong replicationId, int health, int mapAreaId, int healthMaxOther, ulong mapRegionId, int mapCellId, ulong contextAreaRef, bool requiresEnterGameWorld,
            int CombatLevel, int CharacterLevel)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(entityId, prototypeId, position, orientation)
                : new EntityBaseData(entityId, prototypeId, null, null);

            WorldEntity worldEntity = new(baseData, replicationId, position, health, mapAreaId, healthMaxOther, mapRegionId, mapCellId, contextAreaRef);

            worldEntity.PropertyCollection.List.Add(new(PropertyEnum.CharacterLevel, CharacterLevel)); 
            worldEntity.PropertyCollection.List.Add(new(PropertyEnum.CombatLevel, CombatLevel)); // zero effect

            return worldEntity.ToNetMessageEntityCreate();
        }

        public static NetMessageEntityCreate SpawnEmptyEntity(ulong entityId, ulong prototypeId, Vector3 position, Vector3 orientation,
            ulong replicationId)
        {
            EntityBaseData baseData = new EntityBaseData(entityId, prototypeId, position, orientation);
            WorldEntity worldEntity = new(baseData, 1, replicationId);
            return worldEntity.ToNetMessageEntityCreate();
        }
    }
}

