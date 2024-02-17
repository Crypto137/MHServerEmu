using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Regions
{
    using ConnectionNodeDict = Dictionary<PrototypeId, Dictionary<PrototypeGuid, PrototypeId>>;

    public struct TargetObject
    {
        public PrototypeGuid Entity { get; set; }
        public PrototypeId Area { get; set; }
        public PrototypeId TargetId { get; set; }
    }

    public partial class RegionManager
    {
        public static float ProjectToFloor(CellPrototype cell, Vector3 areaOrigin, Vector3 position)
        {
            Vector3 cellPos = position - cell.BoundingBox.Min;
            cellPos.X /= cell.BoundingBox.Width;
            cellPos.Y /= cell.BoundingBox.Length;
            int mapX = (int)cell.HeightMap.HeightMapSize.X;
            int mapY = (int)cell.HeightMap.HeightMapSize.Y;
            int x = Math.Clamp((int)(cellPos.X * mapX), 0, mapX - 1);
            int y = Math.Clamp((int)(cellPos.Y * mapY), 0, mapY - 1);
            short height = cell.HeightMap.HeightMapData[y * mapX + x];
            return height + areaOrigin.Z;
        }

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

        public static ConnectionNodeDict BuildConnectionEdges(PrototypeId[] connectionNode)
        {
            PrototypeId FindArea(PrototypeId target)
            {
                if (target == (PrototypeId)HardcodedBlueprintId.RegionConnectionTarget) return 0;

                var area = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(target);
                if ((PrototypeId)area.Area == PrototypeId.Invalid) return FindArea(area.ParentDataRef);
                return (PrototypeId)area.Area;
            }

            var items = new ConnectionNodeDict();
            var nodes = new List<TargetObject>();

            foreach (PrototypeId connection in connectionNode)
            {
                var proto = GameDatabase.GetPrototype<RegionConnectionNodePrototype>(connection);
                var target = (PrototypeId)proto.Target;
                var origin = (PrototypeId)proto.Origin;
                var entryTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(target);
                var entryOrigin = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(origin);

                nodes.Add(new TargetObject
                {
                    Area = FindArea(target),
                    Entity = GameDatabase.GetPrototypeGuid((PrototypeId)entryTarget.Entity),
                    TargetId = origin
                });
                nodes.Add(new TargetObject
                {
                    Area = FindArea(origin),
                    Entity = GameDatabase.GetPrototypeGuid((PrototypeId)entryOrigin.Entity),
                    TargetId = target
                });
            }
            //foreach (var node in nodes) Logger.Warn($"{node.area}, {node.entity}, {node.targetId}"); 

            var groupedNodes = nodes.GroupBy(node => node.Area);
            foreach (var group in groupedNodes)
            {
                var groupItems = new Dictionary<PrototypeGuid, PrototypeId>();

                foreach (var node in group)
                    groupItems[node.Entity] = node.TargetId;

                items[group.Key] = groupItems;
            }

            return items;
        }
        public ulong CreateEntities(Region region)
        {
            PrototypeId area;
            PrototypeId entryPrototypeId;
            CellPrototype entry;
            int cellid = 1;
            int areaid = 1;
            Vector3 areaOrigin = new();
            Vector3 entityPosition;
            PrototypeId[] connectionNodes;
            ConnectionNodeDict targets;

            void MarkersAdd(CellPrototype entry, int cellId, bool addProp = false)
            {
                foreach (var markerProto in entry.MarkerSet.Markers)
                {
                    if (markerProto is EntityMarkerPrototype entityMarker)
                    {
                        string marker = entityMarker.LastKnownEntityName;

                        if (marker.Contains("GambitMTXStore")) continue; // Invisible
                        if (marker.Contains("CosmicEventVendor")) continue; // Invisible

                        if (marker.Contains("Entity/Characters/") || (addProp && marker.Contains("Entity/Props/")))
                        {
                            entityPosition = entityMarker.Position + areaOrigin;                            

                            var proto = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
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
                var districtPrototypeId = GameDatabase.GetPrototypeRefByName(path);
                DistrictPrototype district = GameDatabase.GetPrototype<DistrictPrototype>(districtPrototypeId);
                cellid = 1;
                foreach (ResourceMarkerPrototype cell in district.CellMarkerSet.Markers)
                {
                    var cellPrototypeId = GameDatabase.GetPrototypeRefByName(cell.Resource);
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(cellPrototypeId), cellid++, addProp);
                }
                    
            }

            void AddTeleports(CellPrototype entry, Area entryArea, ConnectionNodeDict targets, int cellId)
            {
                foreach (var marker in entry.InitializeSet.Markers)
                {
                    if (marker is EntityMarkerPrototype door)
                    {
                        if (targets.ContainsKey(area))
                            if (targets[area].ContainsKey(door.EntityGuid))
                            {
                                //Logger.Warn($"EntityGuid = {door.EntityGuid}");
                                Vector3 position = door.Position + areaOrigin;
                                float dz = 60f;
                                if (door.EntityGuid == (PrototypeGuid)14397992695795297083) dz = 0f;
                                position.Z += dz;
                                _entityManager.SpawnDirectTeleport(
                                       (PrototypeId)region.Prototype, GameDatabase.GetDataRefByPrototypeGuid(door.EntityGuid),
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
                    area = (PrototypeId)entryArea.Prototype;
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

            ulong numEntities = _entityManager.PeekNextEntityId();

            switch (region.Prototype)
            {
                case RegionPrototypeId.AsgardiaRegion:

                    area = (PrototypeId)AreaPrototypeId.AsgardiaArea;
                    MarkersAddDistrict("Resource/Districts/AsgardHubDistrict.district");

                    break;

                case RegionPrototypeId.BronxZooRegionL60:
                case RegionPrototypeId.BrooklynPatrolRegionL60:
                case RegionPrototypeId.XManhattanRegion1to60:
                case RegionPrototypeId.XManhattanRegion60Cosmic:
                case RegionPrototypeId.UltronRaidRegionGreen:
                case RegionPrototypeId.NPERaftRegion:
                case RegionPrototypeId.CH0105NightclubRegion:
                case RegionPrototypeId.CH0201ShippingYardRegion:
                case RegionPrototypeId.CH0701SavagelandRegion:
                case RegionPrototypeId.CH0901NorwayPCZRegion:
                case RegionPrototypeId.CH0904SiegePCZRegion:
                    GenerateEntities(region, null, true, true);

                    break;

                case RegionPrototypeId.CH0804LatveriaPCZRegion:
                case RegionPrototypeId.CH0808DoomCastleRegion:
                    connectionNodes = new PrototypeId[] {
                        (PrototypeId)8784254912487368435, // LatveriaPCZtoInstanceNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototypeId.TRGameCenterRegion:
                    connectionNodes = new PrototypeId[] {
                        (PrototypeId)7639542123192857492, // TRGameCenterNodeEnter
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototypeId.UpperMadripoorRegionL60:
                case RegionPrototypeId.UpperMadripoorRegionL60Cosmic:
                    connectionNodes = new PrototypeId[] {
                        (PrototypeId)7533474634124963840, // UpperMadripoorSafeZoneNode
                        (PrototypeId)7639542123192857492, // TRGameCenterNodeEnter
                        (PrototypeId)18086542742377411223, // UpperMadripoorSewerToCentralNode
                        (PrototypeId)10669111645315543387, // UpperMadripoorSewerToEastNode
                        (PrototypeId)9296679134777520601, // UpperMadripoorSewerToNorthNode
                        (PrototypeId)9655619833865515489, // UpperMadripoorSewerToSouthNode
                        (PrototypeId)14731094987216007537, // UpperMadripoorSewerToWestNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototypeId.DailyGSinisterLabRegionL60:
                    connectionNodes = new PrototypeId[] { (PrototypeId)6937597958432367477 };
                    targets = BuildConnectionEdges(connectionNodes);
                    targets[(PrototypeId)11176346407598236282] = targets[0];
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototypeId.CH0101HellsKitchenRegion:
                    connectionNodes = new PrototypeId[] { (PrototypeId)14443352045617489679 };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.CH0301MadripoorRegion:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)15391000012663825577 , // LowTownToHandTower
                         (PrototypeId)9754323402190562962 , // BeachToHydraOutpostNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.CH0307HandTowerRegion:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)15391000012663825577 , // LowTownToHandTower
                         (PrototypeId)5944206842741730203 , // HandTowerLobbyToFloor2Node
                         (PrototypeId)5285936687927405528 , // HandTowerFloor2ToFloor3Node
                         (PrototypeId)5271901688289307610 , // HandTowerFloor3ToFloor4Node
                         (PrototypeId)10586598960311315260 , // HandTowerFloor4ToBossNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.CH0401LowerEastRegion:
                case RegionPrototypeId.CH0405WaxMuseumRegion:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)7969222097651113506 , // WaxMuseumNodeEnter
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.CH0408MaggiaRestaurantRegion:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)4892594607208668810 , // RestaurantFrontNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.CH0402UpperEastRegion:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)4892594607208668810 , // RestaurantFrontNode
                         (PrototypeId)3426134486181749300 , // FiskTowerEntryNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.CH0410FiskTowerRegion:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)3426134486181749300 , // FiskTowerEntryNode
                         (PrototypeId)10936066410997621140 , // FiskElevatorANode
                         (PrototypeId)9708032418080301461 , // FiskElevatorBNode
                         (PrototypeId)4558214450896052630 , // FiskElevatorCNode
                         (PrototypeId)11836314134885245335 , // FiskElevatorDNode
                         (PrototypeId)2141693716469129624 , // FiskElevatorENode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.HYDRAIslandPartDeuxRegionL60:
                    connectionNodes = new PrototypeId[] {
                        (PrototypeId)14896483168893745334, // Hydra1ShotPreBossToBossNode
                        (PrototypeId)3860711599720838991, // Hydra1ShotSubToCliffNode
                        (PrototypeId)10028772098410821475, // Hydra1ShotSnowToBaseNode
                    }; 
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    // Temporary Fix for ON teleport
                    _entityManager.GetEntityByPrototypeIdFromRegion((PrototypeId)9635289451621323629, region.Id).BaseData.PrototypeId = (PrototypeId)18247075406046695986; //BunkerDoorLargeOFF
                    _entityManager.GetEntityByPrototypeIdFromRegion((PrototypeId)15325647619817083651, region.Id).BaseData.PrototypeId = (PrototypeId)16804963870366568904; //MandarinPortalOFF

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

                case RegionPrototypeId.HelicarrierRegion:

                    area = (PrototypeId)AreaPrototypeId.HelicarrierArea;
                    var cellPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Helicarrier/Helicarrier_HUB.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(cellPrototypeId), cellid);

                    break;

                case RegionPrototypeId.HoloSimARegion1to60:

                    area = GameDatabase.GetPrototypeRefByName("Regions/EndGame/TierX/HoloSim/HoloSimAArea.prototype");
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/EndGame/DR_Survival_A.cell");
                    entry = GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId);
                    MarkersAdd(entry, cellid);

                    cellid = 1;
                    foreach (var marker in entry.MarkerSet.Markers)
                    {
                        if (marker is EntityMarkerPrototype npc)
                        {
                            switch (npc.EntityGuid)
                            {
                                case (PrototypeGuid)17602051469318245682:// EncounterOpenMissionSmallV10
                                case (PrototypeGuid)292473193813839029: // EncounterOpenMissionLargeV1
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Props/Throwables/ThrowablePoliceCar.prototype"),
                                        npc.Position, npc.Rotation,
                                        100, areaid, 100, cellid, area, false, 1, 1);
                                    break;
                            }
                        }
                    }
                    break;

                case RegionPrototypeId.OpDailyBugleRegionL11To60:
                    connectionNodes = new PrototypeId[] 
                    {
                         (PrototypeId)14492921398354848340 , // DailyBugleLobbyToBasementNode
                         (PrototypeId)6115167504424512401 , // DailyBugleBasementToArchivesNode
                         (PrototypeId)10151865075287206574 , // DailyBugleArchivesToOfficeNode
                         (PrototypeId)1078484290838276706 , // DailyBugleOfficeToRooftopNode
                         (PrototypeId)4075900166737242541 , // DailyBugleRooftopToBossNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.DailyGFiskTowerRegionL60:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)4458370674568667845 , // DailyGFiskTowerDToBossNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.DailyGTimesSquareRegionL60:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)8167419542698075817 , // DailyGTimesSquareRestToStreetNode
                         (PrototypeId)12623062565289993766 , // DailyGTimesSquareRoofToHotelNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototypeId.DrStrangeTimesSquareRegionL60:
                    connectionNodes = new PrototypeId[]
                    {
                         (PrototypeId)17939117456967478777 , // DrStrangeTimesSquareRestToStreetNode
                         (PrototypeId)6773295991889538761 , // DrStrangeTimesSquareRooftopToHotelNode
                    };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

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
                        foreach (var marker in cell.MarkerSet.Markers)
                        {
                            if (marker is EntityMarkerPrototype npc)
                            {
                                Vector3 pos = new(npc.Position.X + areaOrigin.X, npc.Position.Y + areaOrigin.Y, npc.Position.Z + areaOrigin.Z);
                                switch (npc.EntityGuid)
                                {
                                    case (PrototypeGuid)2888059748704716317: // EncounterSmall
                                        num++;
                                        if (num == 1)
                                            _entityManager.CreateWorldEntityEnemy(region.Id, doop[3],
                                                pos, npc.Rotation,
                                                608, areaid, 608, cellid, area, false, 60, 60);
                                        else
                                            DrawGroupDoops(pos);

                                        break;

                                    case (PrototypeGuid)13880579250584290847: // EncounterMedium
                                        WorldEntity boss = _entityManager.CreateWorldEntityEnemy(region.Id, doop[4],
                                            pos, npc.Rotation,
                                            608, areaid, 608, cellid, area, false, 60, 60);
                                        boss.PropertyCollection[PropertyEnum.Health] = Property.ToValue(600);

                                        break;
                                }
                            }
                        }
                    }

                    break;

                case RegionPrototypeId.TrainingRoomSHIELDRegion:

                    area = (PrototypeId)AreaPrototypeId.TrainingRoomSHIELDArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Training_Rooms/TrainingRoom_SHIELD_B.cell");
                    entry = GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId);
                    MarkersAdd(entry, cellid, true);

                    cellid = 1;
                    foreach (var marker in entry.MarkerSet.Markers)
                    {
                        if (marker is EntityMarkerPrototype npc)
                        {
                            //Logger.Trace($"[{i}].EntityGuid = {npc.EntityGuid}");     // this is slow and causes Game tick time to go over 50 ms on loading
                            switch (npc.EntityGuid)
                            {
                                case (PrototypeGuid)9760489745388478121: // EncounterTinyV12                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 60, 60);
                                    break;

                                case (PrototypeGuid)1411432581376189649: // EncounterTinyV13                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyRaidBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 60, 60);
                                    break;

                                case (PrototypeGuid)9712873838200498938: // EncounterTinyV14                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/CowsEG/SpearCowD1.prototype"), // why not?
                                        npc.Position, npc.Rotation, //Entity/Characters/Mobs/TrainingRoom/TrainingDamageDummy.prototype
                                        608, areaid, 608, cellid, area, false, 10, 10);
                                    break;

                                case (PrototypeGuid)17473025685948150052: // EncounterTinyV15                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummy.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 10, 10);
                                    break;

                            }
                        }
                    }

                    break;

                case RegionPrototypeId.DangerRoomHubRegion:

                    area = (PrototypeId)AreaPrototypeId.DangerRoomHubArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/EndGame/EndlessDungeon/DangerRoom_LaunchTerminal.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId), cellid);

                    break;

                case RegionPrototypeId.GenoshaHUBRegion:

                    connectionNodes = new PrototypeId[] { (PrototypeId)7252811901575568920 };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);

                    break;

                case RegionPrototypeId.XaviersMansionRegion:

                    area = (PrototypeId)AreaPrototypeId.XaviersMansionArea;
                    MarkersAddDistrict("Resource/Districts/XaviersMansion.district");

                    break;

                case RegionPrototypeId.AvengersTowerHUBRegion:

                    area = (PrototypeId)AreaPrototypeId.AvengersTowerHubArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Avengers_Tower/AvengersTower_HUB.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId), cellid);

                    break;

                case RegionPrototypeId.NPEAvengersTowerHUBRegion:

                    area = (PrototypeId)AreaPrototypeId.NPEAvengersTowerHubArea;
                    entryPrototypeId = GameDatabase.GetPrototypeRefByName("Resource/Cells/DistrictCells/Avengers_Tower/AvengersTowerNPE_HUB.cell");
                    MarkersAdd(GameDatabase.GetPrototype<CellPrototype>(entryPrototypeId), cellid);

                    PrototypeId populationMarkerId = GameDatabase.GetPrototypeRefByName("Resource/Encounters/Discoveries/Social_BenUrich_JessicaJones.encounter");
                    EncounterResourcePrototype populationMarker = GameDatabase.GetPrototype<EncounterResourcePrototype>(populationMarkerId);
                    {
                        EntityMarkerPrototype npc = (EntityMarkerPrototype)populationMarker.MarkerSet.Markers[0]; // BenUrich
                        areaOrigin = new(-464f, 0f, 192f);
                        entityPosition = npc.Position + areaOrigin;
                        _entityManager.CreateWorldEntity(
                                region.Id, GameDatabase.GetDataRefByPrototypeGuid(npc.EntityGuid),
                                entityPosition, npc.Rotation,
                                608, areaid, 608, cellid, area, false);

                        npc = (EntityMarkerPrototype)populationMarker.MarkerSet.Markers[2]; // JessicaJones
                        entityPosition = npc.Position + areaOrigin;
                        _entityManager.CreateWorldEntity(
                                region.Id, GameDatabase.GetDataRefByPrototypeGuid(npc.EntityGuid),
                                entityPosition, npc.Rotation,
                                608, areaid, 608, cellid, area, false);
                    }

                    _entityManager.CreateWorldEntity(region.Id, GameDatabase.GetPrototypeRefByName("Entity/Characters/Vendors/Prototypes/Endgame/TeamSHIELDRepBuffer.prototype"),
                        new(736f, -352f, 177f), new(-2.15625f, 0f, 0f),
                        608, areaid, 608, cellid, area, false);

                    break;

            }
            return _entityManager.PeekNextEntityId() - numEntities;
        }
    }
}
