using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions
{
    public partial class RegionManager
    {
        public void HardcodedEntities(Region region, bool hackTeleport)
        {
            CellPrototype entry; 
           // Vector3 entityPosition;            
            
            switch (region.PrototypeId)
            {
                case RegionPrototypeId.HYDRAIslandPartDeuxRegionL60:

                    /* TODO: OSRSkullJWooDecryptionController
                    
                    Area entryArea = region.AreaList[0];
                    area = (ulong)entryArea.Prototype;
                    entry = GameDatabase.Resource.CellDict[GameDatabase.GetPrototypePath(entryArea.CellList[1].PrototypeId)];
                    npc = (EntityMarkerPrototype)entry.MarkerSet[0]; // SpawnMarkers/Types/MissionTinyV1.prototype
                    areaOrigin = entryArea.CellList[1].PositionInArea;

                    WorldEntity agent = _entityManager.CreateWorldEntity(region.Id, 
                    GameDatabase.GetPrototypeId("Entity/Characters/Mobs/SHIELD/OneShots/SHIELDNamedJimmyWooVulnerable.prototype"),
                    npc.Position + areaOrigin, npc.Rotation,
                    608, (int)entryArea.Id, 608, (int)entryArea.CellList[1].Id, area, false, false);

                    ulong controller = GameDatabase.GetPrototypeId("Missions/Prototypes/PVEEndgame/OneShots/RedSkull/Controllers/OSRSkullJWooDecryptionController.prototype");
                    agent.PropertyCollection.List.Add(new(PropertyEnum.CharacterLevel, 57));
                    agent.PropertyCollection.List.Add(new(PropertyEnum.CombatLevel, 57));
                    agent.PropertyCollection.List.Add(new(PropertyEnum.MissionPrototype, controller));
                    agent.TrackingContextMap = new EntityTrackingContextMap[]{
                        new EntityTrackingContextMap (controller, 15) 
                    };*/

                    break;

                case RegionPrototypeId.HoloSimARegion1to60:

                    Cell cell = region.StartArea.Cells.First().Value;
                    entry = cell.CellProto;
                    
                    foreach (var marker in entry.MarkerSet.Markers)
                    {
                        if (marker is EntityMarkerPrototype npc)
                        {
                            switch (npc.EntityGuid)
                            {
                                case (PrototypeGuid)17602051469318245682:// EncounterOpenMissionSmallV10
                                case (PrototypeGuid)292473193813839029: // EncounterOpenMissionLargeV1
                                    _entityManager.CreateWorldEntityEnemy(cell, GameDatabase.GetPrototypeRefByName("Entity/Props/Throwables/ThrowablePoliceCar.prototype"),
                                        cell.CalcMarkerPosition(npc.Position), npc.Rotation, 100, false, 1);
                                    break;
                            }
                        }
                    }
                    break;
                /*
                case RegionPrototypeId.NPEAvengersTowerHUBRegion:
                    cell = region.StartArea.CellList.First();

                    _entityManager.CreateWorldEntity(cell, GameDatabase.GetPrototypeRefByName("Entity/Characters/Vendors/Prototypes/Endgame/TeamSHIELDRepBuffer.prototype"),
                        new(736f, -352f, 177f), new(-2.15625f, 0f, 0f), 608, false);

                    break;     */           

            }

            // Hack mode for Off Teleports
            if (hackTeleport)
                foreach( var entity in _entityManager.GetEntities(region))
                {
                    if (entity is Transition teleport)
                    { 
                        if (teleport.Destinations.Length > 0 && teleport.Destinations[0].Type == RegionTransitionType.Transition)
                        {
                            var teleportProto = teleport.TransitionPrototype;
                            if (teleportProto.VisibleByDefault == false) // To fix
                            {
                                PrototypeId visibleParent = GetVisibleParentRef(teleportProto.ParentDataRef);
                                entity.BaseData.PrototypeId = visibleParent;
                            }
                        }
                    }
                }

        }

        private PrototypeId GetVisibleParentRef(PrototypeId invisibleId)
        {
            WorldEntityPrototype invisibleProto = GameDatabase.GetPrototype<WorldEntityPrototype>(invisibleId);
            if (invisibleProto.VisibleByDefault == false) return GetVisibleParentRef(invisibleProto.ParentDataRef);
            return invisibleId;
        }
    }
}
