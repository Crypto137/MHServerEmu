using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Regions;

namespace MHServerEmu.Games.Regions
{
    public partial class RegionManager
    {
        public ulong CreateEntities(Region region)
        {
            PrototypeId area;
            CellPrototype entry;            
            Vector3 areaOrigin = new();
            Vector3 entityPosition;
            PrototypeId[] connectionNodes;
            ConnectionNodeList targets;

            ulong numEntities = _entityManager.PeekNextEntityId();

            switch (region.PrototypeId)
            {
                case RegionPrototypeId.HYDRAIslandPartDeuxRegionL60:
                    connectionNodes = new PrototypeId[] {
                        (PrototypeId)14896483168893745334, // Hydra1ShotPreBossToBossNode
                        (PrototypeId)3860711599720838991, // Hydra1ShotSubToCliffNode
                        (PrototypeId)10028772098410821475, // Hydra1ShotSnowToBaseNode
                    }; 
                    targets = RegionTransition.BuildConnectionEdges((PrototypeId)region.PrototypeId);
                    _entityManager.GenerateEntities(region, targets, true, true);

                    // Temporary Fix for ON teleport
                    //_entityManager.GetEntityByPrototypeIdFromRegion((PrototypeId)9635289451621323629, region.Id).BaseData.PrototypeId = (PrototypeId)18247075406046695986; //BunkerDoorLargeOFF
                    //_entityManager.GetEntityByPrototypeIdFromRegion((PrototypeId)15325647619817083651, region.Id).BaseData.PrototypeId = (PrototypeId)16804963870366568904; //MandarinPortalOFF

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

                    Cell cell = region.StartArea.CellList.First();
                    entry = cell.CellProto;
                    _entityManager.MarkersAdd(cell);
                    
                    foreach (var marker in entry.MarkerSet.Markers)
                    {
                        if (marker is EntityMarkerPrototype npc)
                        {
                            switch (npc.EntityGuid)
                            {
                                case (PrototypeGuid)17602051469318245682:// EncounterOpenMissionSmallV10
                                case (PrototypeGuid)292473193813839029: // EncounterOpenMissionLargeV1
                                    _entityManager.CreateWorldEntityEnemy(cell, GameDatabase.GetPrototypeRefByName("Entity/Props/Throwables/ThrowablePoliceCar.prototype"),
                                        npc.Position, npc.Rotation, 100, false, 1);
                                    break;
                            }
                        }
                    }
                    break;

                case RegionPrototypeId.DrStrangeTimesSquareRegionL60:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)17939117456967478777 , // DrStrangeTimesSquareRestToStreetNode
                         (PrototypeId)6773295991889538761 , // DrStrangeTimesSquareRooftopToHotelNode
                    };
                    targets = RegionTransition.BuildConnectionEdges((PrototypeId)region.PrototypeId);
                    _entityManager.GenerateEntities(region, targets, true, true);

                    // Temporary Fix for ON teleport
                    _entityManager.GetEntityByPrototypeIdFromRegion((PrototypeId)3817919318712522311, region.Id).BaseData.PrototypeId = (PrototypeId)3361079670852883724; //OpenTransitionMedSoftOFF1
                    _entityManager.GetEntityByPrototypeIdFromRegion((PrototypeId)11621620264142837342, region.Id).BaseData.PrototypeId = (PrototypeId)16378653613730632945; //OpenTransitionSmlSoftOFF2
                    
                    break;

                case RegionPrototypeId.CosmicDoopSectorSpaceRegion:

                    area = GameDatabase.GetPrototypeRefByName("Regions/EndGame/Special/CosmicDoopSectorSpace/CosmicDoopSectorSpaceAreaA.prototype");
                    PrototypeId[] doop = new PrototypeId[]
                    {
                        (PrototypeId)8886032254367441193, // CosmicDoopRangedMinion
                        (PrototypeId)905954195879503067, // CosmicDoopMeleeMinionLargeAggro
                        (PrototypeId)11242103498987545924, // CosmicDoopRangedMinionLargeAggro
                        (PrototypeId)1173113805575694864, // CosmicDoopDoopZoneMiniBossVariantLargeAggro
                        (PrototypeId)8852879594302677942, // CosmicDoopOverlordLargeAggro
                        (PrototypeId)10884818398647164828 // CosmicDoopDoopZone
                    };

                    static Vector3[] DrawCirclePoints(float radius, int numPoints)
                    {
                        Vector3[] points = new Vector3[numPoints];

                        double angle = 2 * Math.PI / numPoints;

                        for (int i = 0; i < numPoints; i++)
                        {
                            float x = (float)(radius * Math.Cos(i * angle));
                            float y = (float)(radius * Math.Sin(i * angle));
                            float z = (float)(i * angle);
                            points[i] = new Vector3(x, y, z);
                        }

                        return points;
                    }

                    Vector3[] Doops = DrawCirclePoints(400.0f, 5);

                    void AddSmallDoop(Vector3 PosOrient, Vector3 SpawnPos)
                    {
                        Vector3 pos = new(SpawnPos.X + PosOrient.X, SpawnPos.Y + PosOrient.Y, SpawnPos.Z);
                        _entityManager.CreateWorldEntityEnemy(cell, doop[2], pos, new(PosOrient.Z, 0, 0), 608, false, 60);
                    }

                    void DrawGroupDoops(Vector3 SpawnPos)
                    {
                        for (int i = 0; i < Doops.Count(); i++)
                        {
                            AddSmallDoop(Doops[i], SpawnPos);
                        }
                    }

                    Area areaDoop = region.AreaList[0];
                    for (int j = 0; j < region.AreaList[0].CellList.Count; j++)
                    {
                        cell = areaDoop.CellList[j];
                        areaOrigin = cell.AreaPosition;

                        int num = 0;
                        foreach (var marker in cell.CellProto.MarkerSet.Markers)
                        {
                            if (marker is EntityMarkerPrototype npc)
                            {
                                Vector3 pos = new(npc.Position.X + areaOrigin.X, npc.Position.Y + areaOrigin.Y, npc.Position.Z + areaOrigin.Z);
                                switch (npc.EntityGuid)
                                {
                                    case (PrototypeGuid)2888059748704716317: // EncounterSmall
                                        num++;
                                        if (num == 1)
                                            _entityManager.CreateWorldEntityEnemy(cell, doop[3], pos, npc.Rotation, 608, false, 60);
                                        else
                                            DrawGroupDoops(pos);

                                        break;

                                    case (PrototypeGuid)13880579250584290847: // EncounterMedium
                                        WorldEntity boss = _entityManager.CreateWorldEntityEnemy(cell, doop[4], pos, npc.Rotation, 608, false, 60);
                                        boss.PropertyCollection.List.Add(new(PropertyEnum.Health, 600));

                                        break;
                                }
                            }
                        }
                    }

                    break;

                case RegionPrototypeId.TrainingRoomSHIELDRegion:

                    area = (PrototypeId)AreaPrototypeId.TrainingRoomSHIELDArea;
                    cell = region.StartArea.CellList.First();
                    entry = cell.CellProto;
                    targets = RegionTransition.BuildConnectionEdges((PrototypeId)region.PrototypeId);
                    _entityManager.GenerateEntities(region, targets, true, true);

                    foreach (var marker in entry.MarkerSet.Markers)
                    {
                        if (marker is EntityMarkerPrototype npc)
                        {
                            //Logger.Trace($"[{i}].EntityGuid = {npc.EntityGuid}");     // this is slow and causes Game tick time to go over 50 ms on loading
                            switch (npc.EntityGuid)
                            {
                                case (PrototypeGuid)9760489745388478121: // EncounterTinyV12                                    
                                    _entityManager.CreateWorldEntityEnemy(cell, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyBoss.prototype"),
                                        npc.Position, npc.Rotation, 608, false, 60);
                                    break;

                                case (PrototypeGuid)1411432581376189649: // EncounterTinyV13                                    
                                    _entityManager.CreateWorldEntityEnemy(cell, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyRaidBoss.prototype"),
                                        npc.Position, npc.Rotation, 608, false, 60);
                                    break;

                                case (PrototypeGuid)9712873838200498938: // EncounterTinyV14                                    
                                    _entityManager.CreateWorldEntityEnemy(cell, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/CowsEG/SpearCowD1.prototype"), // why not?
                                        npc.Position, npc.Rotation, //Entity/Characters/Mobs/TrainingRoom/TrainingDamageDummy.prototype
                                        608, false, 10);
                                    break;

                                case (PrototypeGuid)17473025685948150052: // EncounterTinyV15                                    
                                    _entityManager.CreateWorldEntityEnemy(cell, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummy.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, false, 10);
                                    break;

                            }
                        }
                    }

                    break;

                case RegionPrototypeId.NPEAvengersTowerHUBRegion:

                    area = (PrototypeId)AreaPrototypeId.NPEAvengersTowerHubArea;
                    cell = region.StartArea.CellList.First();
                    targets = RegionTransition.BuildConnectionEdges((PrototypeId)region.PrototypeId);
                    _entityManager.GenerateEntities(region, targets, true, true);

                    PrototypeId populationMarkerId = GameDatabase.GetPrototypeRefByName("Resource/Encounters/Discoveries/Social_BenUrich_JessicaJones.encounter");
                    EncounterResourcePrototype populationMarker = GameDatabase.GetPrototype<EncounterResourcePrototype>(populationMarkerId);
                    {
                        EntityMarkerPrototype npc = (EntityMarkerPrototype)populationMarker.MarkerSet.Markers[0]; // BenUrich
                        areaOrigin = new(-464f, 0f, 192f);
                        entityPosition = npc.Position + areaOrigin;
                        _entityManager.CreateWorldEntity(cell, GameDatabase.GetDataRefByPrototypeGuid(npc.EntityGuid),
                                entityPosition, npc.Rotation, 608, false);

                        npc = (EntityMarkerPrototype)populationMarker.MarkerSet.Markers[2]; // JessicaJones
                        entityPosition = npc.Position + areaOrigin;
                        _entityManager.CreateWorldEntity(cell, GameDatabase.GetDataRefByPrototypeGuid(npc.EntityGuid),
                                entityPosition, npc.Rotation, 608, false);
                    }

                    _entityManager.CreateWorldEntity(cell, GameDatabase.GetPrototypeRefByName("Entity/Characters/Vendors/Prototypes/Endgame/TeamSHIELDRepBuffer.prototype"),
                        new(736f, -352f, 177f), new(-2.15625f, 0f, 0f), 608, false);

                    break;

                default:
                    targets = RegionTransition.BuildConnectionEdges((PrototypeId)region.PrototypeId);
                    _entityManager.GenerateEntities(region, targets, true, true);
                    break;

            }
            return _entityManager.PeekNextEntityId() - numEntities;
        }
    }
}
