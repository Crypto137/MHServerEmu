using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.GameData.Prototypes.Markers;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.GameData.Prototypes;
using MHServerEmu.GameServer.GameData.Calligraphy;

namespace MHServerEmu.GameServer.Regions
{
    using ConnectionNodeDict = Dictionary<ulong, Dictionary<ulong, ulong>>;

    public struct TargetObject
    {
        public ulong Entity { get; set; }
        public ulong Area { get; set; }
        public ulong TargetId { get; set; }
    }

    public partial class RegionManager
    {
        public static float ProjectToFloor(CellPrototype cell, Vector3 areaOrigin, Vector3 position)
        {
            Vector3 cellPos = position - cell.Boundbox.Min;
            cellPos.X /= cell.Boundbox.Width;
            cellPos.Y /= cell.Boundbox.Length;
            int mapX = (int)cell.HeightMap.HeightMapSize.X;
            int mapY = (int)cell.HeightMap.HeightMapSize.Y;
            int x = Math.Clamp((int)(cellPos.X * mapX), 0, mapX - 1);
            int y = Math.Clamp((int)(cellPos.Y * mapY), 0, mapY - 1);
            short height = cell.HeightMap.HeightMapData[y * mapX + x];
            return height + areaOrigin.Z;
        }

        public static float GetEntityFloor(ulong prototypeId)
        {
            Prototype entity = prototypeId.GetPrototype();
            if (entity.ParentId == (ulong)BlueprintId.NPCTemplateHub)
                return 46f; // AgentUntargetableInvulnerable.WorldEntity.Bounds.CapsuleBounds.HeightFromCenter
            PrototypeEntry TestWorldEntity = entity.GetEntry(BlueprintId.WorldEntity);
            if (TestWorldEntity == null)
                return GetEntityFloor(entity.ParentId);

            PrototypeEntryElement TestBounds = TestWorldEntity.GetField(FieldId.Bounds);
            if (TestBounds == null) 
                return GetEntityFloor(entity.ParentId);

            Prototype bounds = (Prototype)TestBounds.Value;
            float height = 0f;
            if (bounds.ParentId == (ulong)BlueprintId.BoxBounds || bounds.ParentId == (ulong)BlueprintId.ObjectSmall)
                height = (float)(double)bounds.GetEntry(BlueprintId.BoxBounds).GetField(FieldId.Height).Value;
            else if (bounds.ParentId == (ulong)BlueprintId.SphereBounds)
                height = (float)(double)bounds.GetEntry(BlueprintId.SphereBounds).GetField(FieldId.Radius).Value;
            else if (bounds.ParentId == (ulong)BlueprintId.CapsuleBounds)
                height = (float)(double)bounds.GetEntry(BlueprintId.CapsuleBounds).GetField(FieldId.HeightFromCenter).Value * 2f;
            else Logger.Warn($"ParentId = {bounds.ParentId}");

            return height / 2;
        }

        public static bool GetSnapToFloorOnSpawn(ulong prototypeId)
        {
            if (prototypeId == (ulong)BlueprintId.ThrowableProp) return false;
            if (prototypeId == (ulong)BlueprintId.DestructibleProp) return true;
            Prototype entity = prototypeId.GetPrototype();
            PrototypeEntry TestWorldEntity = entity.GetEntry(BlueprintId.WorldEntity);
            if (TestWorldEntity == null) 
                return GetSnapToFloorOnSpawn(entity.ParentId);
            PrototypeEntryElement TestSnapToFloorOnSpawn = TestWorldEntity.GetField(FieldId.SnapToFloorOnSpawn);
            if (TestSnapToFloorOnSpawn == null) 
                return GetSnapToFloorOnSpawn(entity.ParentId);
            return (bool)TestSnapToFloorOnSpawn.Value;
        }

        public static ConnectionNodeDict BuildConnectionEdges(ulong[] connectionNode)
        {
            ulong FindArea(ulong target)
            {
                if (target == (ulong)BlueprintId.RegionConnectionTarget) return 0;
                ulong area = target.GetPrototype().GetEntry(BlueprintId.RegionConnectionTarget).GetFieldDef(FieldId.Area);
                if (area == 0) return FindArea(target.GetPrototype().ParentId);
                return area;
            }

            var items = new ConnectionNodeDict();
            var nodes = new List<TargetObject>();

            foreach (ulong connection in connectionNode)
            {
                ulong target = (ulong)connection.GetPrototype().GetEntry(BlueprintId.RegionConnectionNode).GetField(FieldId.Target).Value;
                PrototypeEntry entryTarget = target.GetPrototype().GetEntry(BlueprintId.RegionConnectionTarget);
                ulong origin = (ulong)connection.GetPrototype().GetEntry(BlueprintId.RegionConnectionNode).GetField(FieldId.Origin).Value;
                PrototypeEntry entryOrigin = origin.GetPrototype().GetEntry(BlueprintId.RegionConnectionTarget);
                nodes.Add(new TargetObject
                {
                    Area = FindArea(target),
                    Entity = GameDatabase.GetPrototypeGuid((ulong)entryTarget.GetField(FieldId.Entity).Value),
                    TargetId = origin
                });
                nodes.Add(new TargetObject
                {
                    Area = FindArea(origin),
                    Entity = GameDatabase.GetPrototypeGuid((ulong)entryOrigin.GetField(FieldId.Entity).Value),
                    TargetId = target
                });
            }
            //foreach (var node in nodes) Logger.Warn($"{node.area}, {node.entity}, {node.targetId}"); 

            var groupedNodes = nodes.GroupBy(node => node.Area);
            foreach (var group in groupedNodes)
            {
                var groupItems = new Dictionary<ulong, ulong>();

                foreach (var node in group)
                    groupItems[node.Entity] = node.TargetId;

                items[group.Key] = groupItems;
            }

            return items;
        }
        public ulong CreateEntities(Region region)
        {
            ulong area;
            ulong entryPrototypeId;
            CellPrototype entry;
            int cellid = 1;
            int areaid = 1;
            Vector3 areaOrigin = new();
            Vector3 entityPosition;
            ulong[] connectionNodes;
            ConnectionNodeDict targets;
            EntityMarkerPrototype npc;

            void MarkersAdd(CellPrototype entry, int cellId, bool addProp = false)
            {
                for (int i = 0; i < entry.MarkerSet.Length; i++)
                {
                    if (entry.MarkerSet[i] is EntityMarkerPrototype)
                    {
                        EntityMarkerPrototype entityMarker = (EntityMarkerPrototype)entry.MarkerSet[i];
                        string marker = entityMarker.LastKnownEntityName;

                        if (marker.Contains("GambitMTXStore")) continue; // Invisible
                        if (marker.Contains("CosmicEventVendor")) continue; // Invisible

                        if (marker.Contains("Entity/Characters/") || (addProp && marker.Contains("Entity/Props/")))
                        {
                            entityPosition = entityMarker.Position + areaOrigin;                            

                            ulong proto = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                            bool entitySnapToFloor = GetSnapToFloorOnSpawn(proto);

                            bool snapToFloor = (entityMarker.OverrideSnapToFloor == 1) ? (entityMarker.OverrideSnapToFloorValue == 1) : entitySnapToFloor;

                            if (snapToFloor)
                            {
                                float projectHeight = ProjectToFloor(entry, areaOrigin, entityMarker.Position);
                                if (entityPosition.Z > projectHeight)
                                    entityPosition.Z = projectHeight;  
                            }

                            entityPosition.Z += GetEntityFloor(proto);

                            _entityManager.CreateWorldEntity(
                                region.Id, proto,
                                entityPosition, entityMarker.Rotation,
                                608, areaid, 608, cellId, area, false, snapToFloor != entitySnapToFloor);
                        }
                    }
                }
            }

            void MarkersAddDistrict(string path, bool addProp = false)
            {
                ulong districtPrototypeId = GameDatabase.GetPrototypeRefByName(path);
                DistrictPrototype district = GameDatabase.GetPrototype<DistrictPrototype>(districtPrototypeId);
                for (cellid = 0; cellid < district.CellMarkerSet.Length; cellid++)
                {
                    ulong cellPrototypeId = GameDatabase.GetPrototypeRefByName(district.CellMarkerSet[cellid].Resource);
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(cellPrototypeId), cellid + 1, addProp);
                }
                    
            }

            void AddTeleports(CellPrototype entry, Area entryArea, ConnectionNodeDict targets, int cellId)
            {
                for (int i = 0; i < entry.InitializeSet.Length; i++)
                {
                    if (entry.InitializeSet[i] is EntityMarkerPrototype)
                    {
                        EntityMarkerPrototype door = (EntityMarkerPrototype)entry.InitializeSet[i];
                        if (targets.ContainsKey(area))
                            if (targets[area].ContainsKey(door.EntityGuid))
                            {
                                //Logger.Warn($"EntityGuid = {door.EntityGuid}");
                                Vector3 position = door.Position + areaOrigin;
                                float dz = 60f;
                                if (door.EntityGuid == 14397992695795297083) dz = 0f;
                                position.Z += dz;
                                _entityManager.SpawnDirectTeleport(
                                       (ulong)region.Prototype, GameDatabase.GetDataRefByPrototypeGuid(door.EntityGuid),
                                       position, door.Rotation,
                                       (int)entryArea.Id, region.Id, cellid, area, false,
                                       targets[area][door.EntityGuid],
                                       door.OverrideSnapToFloor > 0);
                            }
                    }
                }
            }

            void GenerateEntities(Region region, ConnectionNodeDict targets, bool addMarkers, bool addProp)
            {
                for (int a = 0; a < region.AreaList.Count; a++)
                {
                    Area entryArea = region.AreaList[a];
                    area = (ulong)entryArea.Prototype;
                    for (int c = 0; c < entryArea.CellList.Count; c++)
                    {
                        cellid = (int)entryArea.CellList[c].Id;
                        areaid = (int)entryArea.Id;
                        entry = GameDatabase.GetPrototype<CellPrototype>(entryArea.CellList[c].PrototypeId);
                        areaOrigin = entryArea.CellList[c].PositionInArea;
                        if (addMarkers)
                            MarkersAdd(entry, cellid, addProp);
                        if (targets != null && targets.Count > 0)
                            AddTeleports(entry, entryArea, targets, cellid);
                    }
                }
            }

            ulong numEntities = _entityManager.GetLastEntityId();

            switch (region.Prototype)
            {
                case RegionPrototype.AsgardiaRegion:

                    area = (ulong)AreaPrototype.AsgardiaArea;
                    MarkersAddDistrict("Resource/Districts/AsgardHubDistrict.district");

                    break;

                case RegionPrototype.BronxZooRegionL60:
                case RegionPrototype.BrooklynPatrolRegionL60:
                case RegionPrototype.XManhattanRegion1to60:
                case RegionPrototype.XManhattanRegion60Cosmic:
                case RegionPrototype.UltronRaidRegionGreen:
                case RegionPrototype.CH0105NightclubRegion:
                case RegionPrototype.CH0301MadripoorRegion:
                case RegionPrototype.CH0904SiegePCZRegion:
                    GenerateEntities(region, null, true, true);

                    break;

                case RegionPrototype.UpperMadripoorRegionL60:
                case RegionPrototype.UpperMadripoorRegionL60Cosmic:
                    connectionNodes = new ulong[] { 
                        18086542742377411223, // UpperMadripoorSewerToCentralNode
                        10669111645315543387, // UpperMadripoorSewerToEastNode
                        9296679134777520601, // UpperMadripoorSewerToNorthNode
                        9655619833865515489, // UpperMadripoorSewerToSouthNode
                        14731094987216007537, // UpperMadripoorSewerToWestNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototype.DailyGSinisterLabRegionL60:
                    connectionNodes = new ulong[] { 6937597958432367477 };
                    targets = BuildConnectionEdges(connectionNodes);
                    targets[11176346407598236282] = targets[0];
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototype.CH0101HellsKitchenRegion:
                    connectionNodes = new ulong[] { 14443352045617489679 };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                    
                case RegionPrototype.HYDRAIslandPartDeuxRegionL60:
                    connectionNodes = new ulong[] { 
                        14896483168893745334, // Hydra1ShotPreBossToBossNode
                        3860711599720838991, // Hydra1ShotSubToCliffNode
                        10028772098410821475, // Hydra1ShotSnowToBaseNode
                    }; 
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    // Temporary Fix for ON teleport
                    _entityManager.GetEntityByPrototypeId(9635289451621323629).BaseData.PrototypeId = 18247075406046695986; //BunkerDoorLargeOFF
                    _entityManager.GetEntityByPrototypeId(15325647619817083651).BaseData.PrototypeId = 16804963870366568904; //MandarinPortalOFF

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

                case RegionPrototype.HelicarrierRegion:

                    area = (ulong)AreaPrototype.HelicarrierArea;
                    ulong cellPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Helicarrier/Helicarrier_HUB.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(cellPrototypeId), cellid);

                    break;

                case RegionPrototype.HoloSimARegion1to60:

                    area = GameDatabase.GetPrototypeRefByName("Regions/EndGame/TierX/HoloSim/HoloSimAArea.prototype");
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/EndGame/DR_Survival_A.cell");
                    entry = GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId);
                    MarkersAdd(entry, cellid);

                    cellid = 1;
                    for (int i = 0; i < entry.MarkerSet.Length; i++)
                    {
                        if (entry.MarkerSet[i] is EntityMarkerPrototype)
                        {
                            npc = (EntityMarkerPrototype)entry.MarkerSet[i];

                            switch (npc.EntityGuid)
                            {
                                case 17602051469318245682:// EncounterOpenMissionSmallV10
                                case 292473193813839029: // EncounterOpenMissionLargeV1
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Props/Throwables/ThrowablePoliceCar.prototype"),
                                        npc.Position, npc.Rotation,
                                        100, areaid, 100, cellid, area, false, 1, 1);
                                    break;
                            }
                        }
                    }
                    break;

                case RegionPrototype.OpDailyBugleRegionL11To60:
                    connectionNodes = new ulong[] 
                    {
                         14492921398354848340 , // DailyBugleLobbyToBasementNode
                         6115167504424512401 , // DailyBugleBasementToArchivesNode
                         10151865075287206574 , // DailyBugleArchivesToOfficeNode
                         1078484290838276706 , // DailyBugleOfficeToRooftopNode
                         4075900166737242541 , // DailyBugleRooftopToBossNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototype.DailyGTimesSquareRegionL60:
                    connectionNodes = new ulong[]
                    {
                         8167419542698075817 , // DailyGTimesSquareRestToStreetNode
                         12623062565289993766 , // DailyGTimesSquareRoofToHotelNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototype.DrStrangeTimesSquareRegionL60:
                    connectionNodes = new ulong[]
                    {
                         17939117456967478777 , // DrStrangeTimesSquareRestToStreetNode
                         6773295991889538761 , // DrStrangeTimesSquareRooftopToHotelNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    // Temporary Fix for ON teleport
                    _entityManager.GetEntityByPrototypeId(3817919318712522311).BaseData.PrototypeId = 3361079670852883724; //OpenTransitionMedSoftOFF1
                    _entityManager.GetEntityByPrototypeId(11621620264142837342).BaseData.PrototypeId = 16378653613730632945; //OpenTransitionSmlSoftOFF2
                    
                    break;

                case RegionPrototype.CosmicDoopSectorSpaceRegion:

                    area = GameDatabase.GetPrototypeRefByName("Regions/EndGame/Special/CosmicDoopSectorSpace/CosmicDoopSectorSpaceAreaA.prototype");
                    ulong[] doop = new ulong[]
                    {
                        8886032254367441193, // CosmicDoopRangedMinion
                        905954195879503067, // CosmicDoopMeleeMinionLargeAggro
                        11242103498987545924, // CosmicDoopRangedMinionLargeAggro
                        1173113805575694864, // CosmicDoopDoopZoneMiniBossVariantLargeAggro
                        8852879594302677942, // CosmicDoopOverlordLargeAggro
                        10884818398647164828 // CosmicDoopDoopZone
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
                        _entityManager.CreateWorldEntityEnemy(region.Id, doop[2],
                                            pos, new(PosOrient.Z, 0, 0),
                                            608, areaid, 608, cellid, area, false, 60, 60);
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
                        cellid = (int)areaDoop.CellList[j].Id;
                        areaOrigin = areaDoop.CellList[j].PositionInArea;
                        CellPrototype cell = GameDatabase.GetPrototype<CellPrototype>(areaDoop.CellList[j].PrototypeId);
                        int num = 0;
                        for (int i = 0; i < cell.MarkerSet.Length; i++)
                        {
                            if (cell.MarkerSet[i] is EntityMarkerPrototype)
                            {
                                npc = (EntityMarkerPrototype)cell.MarkerSet[i];
                                Vector3 pos = new(npc.Position.X + areaOrigin.X, npc.Position.Y + areaOrigin.Y, npc.Position.Z + areaOrigin.Z);
                                switch (npc.EntityGuid)
                                {
                                    case 2888059748704716317: // EncounterSmall
                                        num++;
                                        if (num == 1)
                                            _entityManager.CreateWorldEntityEnemy(region.Id, doop[3],
                                                pos, npc.Rotation,
                                                608, areaid, 608, cellid, area, false, 60, 60);
                                        else
                                            DrawGroupDoops(pos);

                                        break;

                                    case 13880579250584290847: // EncounterMedium
                                        WorldEntity boss = _entityManager.CreateWorldEntityEnemy(region.Id, doop[4],
                                            pos, npc.Rotation,
                                            608, areaid, 608, cellid, area, false, 60, 60);
                                        boss.PropertyCollection.List.Add(new(PropertyEnum.Health, 600));

                                        break;
                                }
                            }
                        }
                    }

                    break;

                case RegionPrototype.TrainingRoomSHIELDRegion:

                    area = (ulong)AreaPrototype.TrainingRoomSHIELDArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Training_Rooms/TrainingRoom_SHIELD_B.cell");
                    entry = GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId);
                    MarkersAdd(entry, cellid, true);

                    cellid = 1;
                    for (int i = 0; i < entry.MarkerSet.Length; i++)
                    {
                        if (entry.MarkerSet[i] is EntityMarkerPrototype)
                        {
                            npc = (EntityMarkerPrototype)entry.MarkerSet[i];
                            //Logger.Trace($"[{i}].EntityGuid = {npc.EntityGuid}");     // this is slow and causes Game tick time to go over 50 ms on loading
                            switch (npc.EntityGuid)
                            {
                                case 9760489745388478121: // EncounterTinyV12                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 60, 60);
                                    break;

                                case 1411432581376189649: // EncounterTinyV13                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyRaidBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 60, 60);
                                    break;

                                case 9712873838200498938: // EncounterTinyV14                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/CowsEG/SpearCowD1.prototype"), // why not?
                                        npc.Position, npc.Rotation, //Entity/Characters/Mobs/TrainingRoom/TrainingDamageDummy.prototype
                                        608, areaid, 608, cellid, area, false, 10, 10);
                                    break;

                                case 17473025685948150052: // EncounterTinyV15                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummy.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 10, 10);
                                    break;

                            }
                        }
                    }

                    break;

                case RegionPrototype.DangerRoomHubRegion:

                    area = (ulong)AreaPrototype.DangerRoomHubArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/EndGame/EndlessDungeon/DangerRoom_LaunchTerminal.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId), cellid);

                    break;

                case RegionPrototype.GenoshaHUBRegion:

                    connectionNodes = new ulong[] { 7252811901575568920 };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototype.XaviersMansionRegion:

                    area = (ulong)AreaPrototype.XaviersMansionArea;
                    MarkersAddDistrict("Resource/Districts/XaviersMansion.district");

                    break;

                case RegionPrototype.CH0701SavagelandRegion:

                    area = GameDatabase.GetPrototypeRefByName("Regions/StoryRevamp/CH07SavageLand/Areas/DinoJungle/DinoJungleArea.prototype");

                    Area areaL = region.AreaList[0];
                    for (int i = 11; i < 14; i++)
                    {
                        cellid = (int)areaL.CellList[i].Id;
                        areaOrigin = areaL.CellList[i].PositionInArea;
                        MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(areaL.CellList[i].PrototypeId), cellid, true);
                    }

                    break;

                case RegionPrototype.AvengersTowerHUBRegion:

                    area = (ulong)AreaPrototype.AvengersTowerHubArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Avengers_Tower/AvengersTower_HUB.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId), cellid);

                    break;

                case RegionPrototype.NPEAvengersTowerHUBRegion:

                    area = (ulong)AreaPrototype.NPEAvengersTowerHubArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Avengers_Tower/AvengersTowerNPE_HUB.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId), cellid);

                    ulong populationMarkerId = GameDatabase.GetPrototypeRefByName("Resource/Encounters/Discoveries/Social_BenUrich_JessicaJones.encounter");
                    EncounterPrototype populationMarker = GameDatabase.GetPrototype<EncounterPrototype>(populationMarkerId);
                    npc = (EntityMarkerPrototype)populationMarker.MarkerSet[0]; // BenUrich
                    areaOrigin = new(-464f, 0f, 192f);
                    entityPosition = npc.Position + areaOrigin;
                    _entityManager.CreateWorldEntity(
                            region.Id, GameDatabase.GetDataRefByPrototypeGuid(npc.EntityGuid),
                            entityPosition, npc.Rotation,
                            608, areaid, 608, cellid, area, false);

                    npc = (EntityMarkerPrototype)populationMarker.MarkerSet[2]; // JessicaJones
                    entityPosition = npc.Position + areaOrigin;
                    _entityManager.CreateWorldEntity(
                            region.Id, GameDatabase.GetDataRefByPrototypeGuid(npc.EntityGuid),
                            entityPosition, npc.Rotation,
                            608, areaid, 608, cellid, area, false);

                    _entityManager.CreateWorldEntity(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Vendors/Prototypes/Endgame/TeamSHIELDRepBuffer.prototype"),
                        new(736f, -352f, 177f), new(-2.15625f, 0f, 0f),
                        608, areaid, 608, cellid, area, false);

                    break;

            }
            return _entityManager.GetLastEntityId() - numEntities;
        }
    }
}
