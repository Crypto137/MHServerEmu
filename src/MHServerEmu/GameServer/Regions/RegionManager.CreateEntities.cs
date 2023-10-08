using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.GameData.Prototypes.Markers;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Regions
{
    using ConnectionNodeDict = Dictionary<ulong, Dictionary<ulong, ulong>>;

    public partial class RegionManager
    {
        public float ProjectToFloor(CellPrototype cell, Vector3 areaOrigin, Vector3 position)
        {
            Vector3 cellPos = position - cell.Boundbox.Min;
            cellPos.X /= cell.Boundbox.Width;
            cellPos.Y /= cell.Boundbox.Length;
            int mapX = (int)cell.HeightMap.HeightMapSize.X;
            int mapY = (int)cell.HeightMap.HeightMapSize.Y;
            int x = Math.Clamp(mapX - 1 - (int)(cellPos.X * mapX), 0, mapX - 1);
            int y = Math.Clamp(mapY - 1 - (int)(cellPos.Y * mapY), 0, mapY - 1);
            short height = (short)cell.HeightMap.HeightMapData[y * mapX + x];
            //Logger.Warn($"Height = [{height}]");           
            return height + areaOrigin.Z;
        }

        public float GetEntityFloor(ulong prototypeId)
        {
            //Logger.Warn($"prototype = [{prototypeId}] {GameDatabase.GetPrototypePath(prototypeId)}");
            PrototypeEntry TestWorldEntity = prototypeId.GetPrototype().GetEntry(BlueprintId.WorldEntity);
            if (TestWorldEntity == null)
            {
                Logger.Debug($"[GetEntityFloor] WorldEntity not found [{prototypeId}] {GameDatabase.GetPrototypePath(prototypeId)}");
                return 0f;
            }
            PrototypeEntryElement TestBounds = TestWorldEntity.GetField(FieldId.Bounds);
            if (TestBounds == null) {
                Logger.Trace($"[GetEntityFloor] Bounds not found [{prototypeId}] {GameDatabase.GetPrototypePath(prototypeId)}");
                return 50f; }

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

        public ConnectionNodeDict BuildConnectionEdges(ulong[] connectionNode)
        {
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
                    Area = entryTarget.GetFieldDef(FieldId.Area),
                    Entity = GameDatabase.GetPrototypeGuid((ulong)entryTarget.GetField(FieldId.Entity).Value),
                    TargetId = origin
                });
                nodes.Add(new TargetObject
                {
                    Area = entryOrigin.GetFieldDef(FieldId.Area),
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
                        EntityMarkerPrototype npc = (EntityMarkerPrototype)entry.MarkerSet[i];
                        string marker = npc.LastKnownEntityName;

                        if (marker.Contains("GambitMTXStore")) continue; // Invisible
                        if (marker.Contains("CosmicEventVendor")) continue; // Invisible

                        if (marker.Contains("Entity/Characters/") || (addProp && marker.Contains("Entity/Props/")))
                        {
                            entityPosition = npc.Position + areaOrigin;
                            bool snap = npc.OverrideSnapToFloor == 1;
                            if (marker.Contains("Entity/Characters/"))
                            {
                                snap = false;
                                if (marker.Contains("Magik")) snap = true;
                                if (marker.Contains("BloodRoseBarVendor"))
                                {
                                    snap = true;
                                    entityPosition.Z += 50f;
                                }
                            }
                            if (snap == false)
                            {
                                entityPosition.Z = ProjectToFloor(entry, areaOrigin, npc.Position) +
                                    GetEntityFloor(GameDatabase.GetPrototypeId(npc.EntityGuid));
                            }

                            _entityManager.CreateWorldEntity(
                                region.Id, GameDatabase.GetPrototypeId(npc.EntityGuid),
                                entityPosition, npc.Rotation,
                                608, areaid, 608, cellId, area, false, snap);
                        }
                    }
                }
            }

            void MarkersAddDistrict(string path, bool addProp = false)
            {
                District district = GameDatabase.Resource.DistrictDict[path];
                for (cellid = 0; cellid < district.CellMarkerSet.Length; cellid++)
                    MarkersAdd(GameDatabase.Resource.CellDict[district.CellMarkerSet[cellid].Resource], cellid + 1, addProp);
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
                                       (ulong)region.Prototype, GameDatabase.GetPrototypeId(door.EntityGuid),
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
                        entry = GameDatabase.Resource.CellDict[GameDatabase.GetPrototypePath(entryArea.CellList[c].PrototypeId)];
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

                case RegionPrototype.BrooklynPatrolRegionL60:

                    areaid = 2;
                    areaOrigin = new(1152.0f, 0.0f, 0.0f);
                    area = GameDatabase.GetPrototypeId("Regions/EndGame/TierX/PatrolBrooklyn/Areas/DocksPatrolBridgeTransitionNS.prototype");
                    entry = GameDatabase.Resource.CellDict["Resource/Cells/EndGame/BrooklynDocksPatrol/DocksPatrol_BridgeA_Center_A.cell"];
                    MarkersAdd(entry, 18, true);

                    break;

                case RegionPrototype.UpperMadripoorRegionL60:
                case RegionPrototype.UpperMadripoorRegionL60Cosmic:
                case RegionPrototype.XManhattanRegion1to60:
                case RegionPrototype.XManhattanRegion60Cosmic:
                case RegionPrototype.UltronRaidRegionGreen:
                case RegionPrototype.CH0105NightclubRegion:
                case RegionPrototype.CH0904SiegePCZRegion:
                    GenerateEntities(region, null, true, true);

                    break;

                case RegionPrototype.DailyGSinisterLabRegionL60:
                    // connectionNodes = new ulong[] { 6937597958432367477 };
                    targets = new ConnectionNodeDict
                    {
                        [11176346407598236282] = new Dictionary<ulong, ulong> {
                        [7414834759211230489] = 8059231424571188000 , // OpenTransitionMedSoftFlat
                        [10015913612618892855] = 1605486679378305818 } // OpenTransitionMedSoft2
                    };

                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototype.CH0101HellsKitchenRegion:
                    connectionNodes = new ulong[] { 14443352045617489679 };
                    targets = BuildConnectionEdges(connectionNodes);
                    GenerateEntities(region, targets, true, true);
                    break;

                case RegionPrototype.HelicarrierRegion:

                    area = (ulong)AreaPrototype.HelicarrierArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Helicarrier/Helicarrier_HUB.cell"], cellid);

                    break;

                case RegionPrototype.HoloSimARegion1to60:

                    area = GameDatabase.GetPrototypeId("Regions/EndGame/TierX/HoloSim/HoloSimAArea.prototype");
                    entry = GameDatabase.Resource.CellDict["Resource/Cells/EndGame/DR_Survival_A.cell"];
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
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeId("Entity/Props/Throwables/ThrowablePoliceCar.prototype"),
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

                case RegionPrototype.CosmicDoopSectorSpaceRegion:

                    area = GameDatabase.GetPrototypeId("Regions/EndGame/Special/CosmicDoopSectorSpace/CosmicDoopSectorSpaceAreaA.prototype");
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
                        CellPrototype Cell = GameDatabase.Resource.CellDict[GameDatabase.GetPrototypePath(areaDoop.CellList[j].PrototypeId)];
                        int num = 0;
                        for (int i = 0; i < Cell.MarkerSet.Length; i++)
                        {
                            if (Cell.MarkerSet[i] is EntityMarkerPrototype)
                            {
                                npc = (EntityMarkerPrototype)Cell.MarkerSet[i];
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
                    entry = GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Training_Rooms/TrainingRoom_SHIELD_B.cell"];
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
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeId("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 60, 60);
                                    break;

                                case 1411432581376189649: // EncounterTinyV13                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeId("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummyRaidBoss.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 60, 60);
                                    break;

                                case 9712873838200498938: // EncounterTinyV14                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeId("Entity/Characters/Mobs/CowsEG/SpearCowD1.prototype"), // why not?
                                        npc.Position, npc.Rotation, //Entity/Characters/Mobs/TrainingRoom/TrainingDamageDummy.prototype
                                        608, areaid, 608, cellid, area, false, 10, 10);
                                    break;

                                case 17473025685948150052: // EncounterTinyV15                                    
                                    _entityManager.CreateWorldEntityEnemy(region.Id, GameDatabase.GetPrototypeId("Entity/Characters/Mobs/TrainingRoom/TrainingHPDummy.prototype"),
                                        npc.Position, npc.Rotation,
                                        608, areaid, 608, cellid, area, false, 10, 10);
                                    break;

                            }
                        }
                    }
                    /* zero effects 
                    messageList.Add(new(NetMessageAIToggleState.CreateBuilder()
                        .SetState(true)
                        .Build())
                        );

                    messageList.Add(new(NetMessageDamageToggleState.CreateBuilder()
                        .SetState(false)
                        .Build())
                        );
                    */
                    break;

                case RegionPrototype.DangerRoomHubRegion:

                    area = (ulong)AreaPrototype.DangerRoomHubArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/EndGame/EndlessDungeon/DangerRoom_LaunchTerminal.cell"], cellid);

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

                    area = GameDatabase.GetPrototypeId("Regions/StoryRevamp/CH07SavageLand/Areas/DinoJungle/DinoJungleArea.prototype");

                    Area areaL = region.AreaList[0];
                    for (int i = 11; i < 14; i++)
                    {
                        cellid = (int)areaL.CellList[i].Id;
                        areaOrigin = areaL.CellList[i].PositionInArea;
                        MarkersAdd(GameDatabase.Resource.CellDict[GameDatabase.GetPrototypePath(areaL.CellList[i].PrototypeId)], cellid, true);
                    }

                    break;

                case RegionPrototype.AvengersTowerHUBRegion:

                    area = (ulong)AreaPrototype.AvengersTowerHubArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Avengers_Tower/AvengersTower_HUB.cell"], cellid);

                    break;

                case RegionPrototype.NPEAvengersTowerHUBRegion:

                    area = (ulong)AreaPrototype.NPEAvengersTowerHubArea;
                    MarkersAdd(GameDatabase.Resource.CellDict["Resource/Cells/DistrictCells/Avengers_Tower/AvengersTowerNPE_HUB.cell"], cellid);

                    Encounter PopulationMarker = GameDatabase.Resource.EncounterDict["Resource/Encounters/Discoveries/Social_BenUrich_JessicaJones.encounter"];
                    npc = (EntityMarkerPrototype)PopulationMarker.MarkerSet[0]; // BenUrich
                    areaOrigin = new(-464f, 0f, 192f);
                    entityPosition = npc.Position + areaOrigin;
                    _entityManager.CreateWorldEntity(
                            region.Id, GameDatabase.GetPrototypeId(npc.EntityGuid),
                            entityPosition, npc.Rotation,
                            608, areaid, 608, cellid, area, false);

                    npc = (EntityMarkerPrototype)PopulationMarker.MarkerSet[2]; // JessicaJones
                    entityPosition = npc.Position + areaOrigin;
                    _entityManager.CreateWorldEntity(
                            region.Id, GameDatabase.GetPrototypeId(npc.EntityGuid),
                            entityPosition, npc.Rotation,
                            608, areaid, 608, cellid, area, false);

                    _entityManager.CreateWorldEntity(region.Id, GameDatabase.GetPrototypeId("Entity/Characters/Vendors/Prototypes/Endgame/TeamSHIELDRepBuffer.prototype"),
                        new(736f, -352f, 177f), new(-2.15625f, 0f, 0f),
                        608, areaid, 608, cellid, area, false);

                    break;

            }
            return _entityManager.GetLastEntityId() - numEntities;
        }
    }
}
