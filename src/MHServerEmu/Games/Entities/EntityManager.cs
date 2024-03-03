using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Generators.Regions;
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

        private readonly Game _game;
        private readonly Dictionary<ulong, Entity> _entityDict = new();

        private ulong _nextEntityId = 1000;
        private ulong GetNextEntityId() { return _nextEntityId++; }
        public ulong PeekNextEntityId() { return _nextEntityId; }

        public EntityManager(Game game)
        {
            _game = game;

            // minihack: force default player entity message initialization on construction
            // so that there isn't a lag when a player logs in for the first time after the server starts
            bool playerMessageIsEmpty = PlayerMessage == null;
        }

        public WorldEntity CreateWorldEntity(Cell cell, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            int health, bool requiresEnterGameWorld, bool OverrideSnapToFloor = false)
        {
            if (cell == null) return default;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            ulong regionId = cell.GetRegion().Id;
            int healthMaxOther = health;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            WorldEntity worldEntity = new(baseData, _game.CurrentRepId, position, health, mapAreaId, healthMaxOther, regionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            worldEntity.EnterWorld(cell, position, orientation);
            _entityDict.Add(baseData.EntityId, worldEntity);
            return worldEntity;
        }

        public WorldEntity CreateWorldEntityEnemy(Cell cell, PrototypeId prototypeId, Vector3 position, Vector3 orientation,
            int health, bool requiresEnterGameWorld, int characterLevel)
        {
            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation)
                : new EntityBaseData(GetNextEntityId(), prototypeId, null, null);

            ulong regionId = cell.GetRegion().Id;
            int healthMaxOther = health;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            WorldEntity worldEntity = new(baseData, _game.CurrentRepId, position, health, mapAreaId, healthMaxOther, regionId, mapCellId, contextAreaRef);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);
            worldEntity.EnterWorld(cell, position, orientation);
            int combatLevel = characterLevel;
            worldEntity.Properties[PropertyEnum.CharacterLevel] = characterLevel;
            worldEntity.Properties[PropertyEnum.CombatLevel] = combatLevel;         // zero effect

            return worldEntity;
        }

        public WorldEntity CreateWorldEntityEmpty(ulong regionId, PrototypeId prototypeId, Vector3 position, Vector3 orientation)
        {
            EntityBaseData baseData = new EntityBaseData(GetNextEntityId(), prototypeId, position, orientation);
            WorldEntity worldEntity = new(baseData, AOINetworkPolicyValues.AOIChannelProximity, _game.CurrentRepId);
            worldEntity.RegionId = regionId;
            _entityDict.Add(baseData.EntityId, worldEntity);
            return worldEntity;
        }

        public Item CreateInvItem(PrototypeId itemProto, InventoryLocation invLoc, PrototypeId rarity, int itemLevel, float itemVariation, int seed, AffixSpec[] affixSpec, bool isNewItem) {

            EntityBaseData baseData = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.AOIChannelOwner,
                EntityId = GetNextEntityId(),
                PrototypeId = itemProto,
                FieldFlags = EntityCreateMessageFlags.HasInterestPolicies | EntityCreateMessageFlags.HasInvLoc,
                InterestPolicies = AOINetworkPolicyValues.AOIChannelOwner,
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

        public Transition SpawnTransitionMarker(Cell cell, TransitionPrototype transitionProto, Vector3 position, Vector3 orientation,
            bool requiresEnterGameWorld, bool OverrideSnapToFloor)
        {
            if (cell == null) return default;
            ulong regionId = cell.GetRegion().Id;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, null, null);

            Transition transition = new(baseData, _game.CurrentRepId, regionId, mapAreaId, mapCellId, contextAreaRef, position, null);
            transition.RegionId = regionId;
            transition.EnterWorld(cell, position, orientation);
            _entityDict.Add(baseData.EntityId, transition);

            return transition;
        }

        public Transition SpawnTargetTeleport(Cell cell, TransitionPrototype transitionProto, Vector3 position, Vector3 orientation,
            bool requiresEnterGameWorld, PrototypeId targetRef, bool OverrideSnapToFloor)
        {
            if (cell == null) return default;

            EntityBaseData baseData = (requiresEnterGameWorld == false)
                ? new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, position, orientation, OverrideSnapToFloor)
                : new EntityBaseData(GetNextEntityId(), transitionProto.DataRef, null, null);

            var regionConnectionTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetRef);

            var cellAssetId = regionConnectionTarget.Cell;
            var cellPrototypeId = cellAssetId != AssetId.Invalid ? GameDatabase.GetDataRefByAsset(cellAssetId) : PrototypeId.Invalid;

            var targetRegionRef = regionConnectionTarget.Region;

            Region region = cell.GetRegion();
            var targetRegion = GameDatabase.GetPrototype<RegionPrototype>(targetRegionRef);
            if (RegionPrototype.Equivalent(targetRegion, region.RegionPrototype)) targetRegionRef = (PrototypeId)region.PrototypeId;

            Destination destination = new()
            {
                Type = transitionProto.Type,
                Region = targetRegionRef,
                Area = regionConnectionTarget.Area,
                Cell = cellPrototypeId,
                Entity = regionConnectionTarget.Entity,
                NameId = regionConnectionTarget.Name,
                Target = targetRef
            };

            ulong regionId = region.Id;
            int mapAreaId = (int)cell.Area.Id;
            int mapCellId = (int)cell.Id;
            PrototypeId contextAreaRef = (PrototypeId)cell.Area.PrototypeId;

            Transition transition = new(baseData, _game.CurrentRepId, regionId, mapAreaId, mapCellId, contextAreaRef, position, destination);
            transition.RegionId = regionId;
            transition.EnterWorld(cell, position, orientation);
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
                    PrototypeId area = entity.Value.Properties[PropertyEnum.ContextAreaRef];
                    if (area == destination.Area)
                        return entity.Value;
                }
            }
            return null;
        }
        public Transition GetTransitionInRegion(Destination destination, ulong regionId)
        {
            PrototypeId areaRef = destination.Area;
            PrototypeId cellRef = destination.Cell;
            PrototypeId entityRef = destination.Entity;
            foreach (var entity in _entityDict.Values)
                if (entity.RegionId == regionId)
                {
                    if (entity is not Transition transition) continue;
                    if (areaRef != 0 && areaRef != (PrototypeId)transition.Location.Area.PrototypeId) continue;
                    if (cellRef != 0 && cellRef != transition.Location.Cell.PrototypeId) continue;
                    if (transition.BaseData.PrototypeId == entityRef)
                        return transition;
                }

            return default;
        }

        public bool TryGetEntityById(ulong entityId, out Entity entity) => _entityDict.TryGetValue(entityId, out entity);
        public ulong GetPropertyCollectionReplicationId(ulong entityId) => _entityDict[entityId].Properties.ReplicationId;
        public bool TryGetPropertyCollectionReplicationId(ulong entityId, out ulong replicationId)
        {
            if (_entityDict.TryGetValue(entityId, out Entity entity))
            {
                replicationId = entity.Properties.ReplicationId;
                return true;
            }

            replicationId = 0;
            return false;
        }

        public Player GetDefaultPlayerEntity()
        {
            EntityBaseData baseData = new(PlayerMessage.BaseData);
            return new(baseData, PlayerMessage.ArchiveData);
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

        #region OldSpawnSystem

        public void AddEntityMarker(Cell cell, EntityMarkerPrototype entityMarker)
        {
            CellPrototype cellProto = cell.CellProto;

            Vector3 entityPosition = cell.CalcMarkerPosition(entityMarker.Position);

            PrototypeId protoRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
            var entity = GameDatabase.GetPrototype<WorldEntityPrototype>(protoRef);

            bool? snapToFloor = SpawnSpec.SnapToFloorConvert(entityMarker.OverrideSnapToFloor, entityMarker.OverrideSnapToFloorValue);
            snapToFloor ??= entity.SnapToFloorOnSpawn;
            bool overrideSnap = snapToFloor != entity.SnapToFloorOnSpawn;
            if (snapToFloor == true) // Fix Boxes in Axis Raid
            {
                float projectHeight = cell.RegionBounds.Center.Z + RegionLocation.ProjectToFloor(cellProto, entityMarker.Position);
                if (entityPosition.Z > projectHeight)
                    entityPosition.Z = projectHeight;
            }
            entityPosition.Z += entity.Bounds.GetBoundHalfHeight();
            int health = GetRankHealth(entity);
            WorldEntity worldEntity = CreateWorldEntity(cell, protoRef, entityPosition, entityMarker.Rotation, health, false, overrideSnap);
            if (worldEntity.WorldEntityPrototype is AgentPrototype)
                worldEntity.AppendOnStartActions(cell.GetRegion().PrototypeDataRef);
        }

        public static int GetRankHealth(WorldEntityPrototype entity)
        {
            if (entity is PropPrototype)
            {
                return 200;
            } 
            else
            {
                switch ((RankPrototypeId)entity.Rank)
                {
                    case RankPrototypeId.Popcorn: return 600;
                    case RankPrototypeId.Champion: return 800;
                    case RankPrototypeId.Elite: return 1000;
                    case RankPrototypeId.MiniBoss: return 1500;
                    case RankPrototypeId.Boss: return 2000;
                    default: return 1000;
                }

            }
        }

        public enum RankPrototypeId : ulong
        {
            Popcorn = 15168672998566398820,
            Champion = 3048000484526787506,
            Elite = 17308931952834644598,
            EliteMinion = 7470660573381266688,
            EliteNamed = 11012647903754259579,
            MiniBoss = 18093345044982008775,
            Boss = 9550003146522364442,
        } 

        public void AddTeleports(Cell cell, Area entryArea, ConnectionNodeList targets)
        {
            PrototypeId area = (PrototypeId)entryArea.PrototypeId;

            foreach (var marker in cell.CellProto.InitializeSet.Markers)
            {
                if (marker is EntityMarkerPrototype portal)
                {
                    PrototypeId protoId = GameDatabase.GetDataRefByPrototypeGuid(portal.EntityGuid);
                    Prototype entity = GameDatabase.GetPrototype<Prototype>(protoId);
                    bool snap = portal.OverrideSnapToFloor;
                    if (entity is TransitionPrototype transition)
                    {
                        Vector3 position = cell.CalcMarkerPosition(portal.Position);
                        position.Z += transition.Bounds.GetBoundHalfHeight();

                        //Logger.Debug($"[{transition.Type}] {portal.LastKnownEntityName} [{protoId}]");
                        if (transition.Waypoint != 0)
                        {
                            var waypointProto = GameDatabase.GetPrototype<WaypointPrototype>(transition.Waypoint);
                            SpawnTargetTeleport(cell, transition, position, portal.Rotation, false, waypointProto.Destination, snap);
                        }
                        else
                        {
                            TargetObject node = RegionTransition.GetTargetNode(targets, area, cell.PrototypeId, portal.EntityGuid);
                            if (node != null)
                                SpawnTargetTeleport(cell, transition, position, portal.Rotation, false, node.TargetId, snap);
                            else
                                SpawnTransitionMarker(cell, transition, position, portal.Rotation, false, snap);
                        }
                    }

                }
            }
        }

        #endregion

        #region Hardcode
        public void HardcodedEntities(Region region)
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
                                    CreateWorldEntityEnemy(cell, GameDatabase.GetPrototypeRefByName("Entity/Props/Throwables/ThrowablePoliceCar.prototype"),
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

            // Hack mode for Off Teleports / Blocker
            List<WorldEntity> blockers = new();
            EntityRegionSPContext context = new() { Flags = EntityRegionSPContextFlags.ActivePartition | EntityRegionSPContextFlags.StaticPartition };
            foreach (var entity in region.IterateEntitiesInVolume(region.Bound, context))
            {
                if (entity is Transition teleport)
                {
                    if (teleport.Destinations.Length > 0 && teleport.Destinations[0].Type == RegionTransitionType.Transition)
                    {
                        var teleportProto = teleport.TransitionPrototype;
                        if (teleportProto.VisibleByDefault == false) // To fix
                        {
                            // Logger.Debug($"[{teleport.Location.GetPosition().ToStringFloat()}][InvT]{GameDatabase.GetFormattedPrototypeName(teleport.Destinations[0].Target)} = {teleport.Destinations[0].Target},");
                            if (LockedTargets.Contains((InvTarget)teleport.Destinations[0].Target) == false) continue;
                            PrototypeId visibleParent = GetVisibleParentRef(teleportProto.ParentDataRef);
                            entity.BaseData.PrototypeId = visibleParent;
                            continue;
                        }
                        // Logger.Debug($"[T]{GameDatabase.GetFormattedPrototypeName(teleport.Destinations[0].Target)} = {teleport.Destinations[0].Target},");
                    }
                }
                else if (Blockers.Contains((BlockerEntity)entity.BaseData.PrototypeId))
                {
                    blockers.Add(entity);              
                }

            }
            foreach (var entity in blockers) entity.ExitWorld();

        }

        private static readonly InvTarget[] LockedTargets = new InvTarget[]
        {
            InvTarget.ResearchCorridorEntryTarget,
            InvTarget.CH0202HoodContainerInteriorTarget,
            InvTarget.CH0205TaskmasterTapeInteriorTarget,
            InvTarget.LokiBossPhaseTwoTarget,
            InvTarget.NPEAvengersTowerHubEntry,
            InvTarget.XMansionBodySliderTarget,
            InvTarget.BroodCavesBossINT,
            InvTarget.BroodCavesEXTEntryTarget,
            InvTarget.SauronCavesEXTEntryTarget,
            InvTarget.GeneModEXITPortalTarget,
            InvTarget.XMansionBlackbirdWaypoint,
            InvTarget.AIMWeapFacToMODOKTarget,
            InvTarget.HelicarrierEntryTarget,
            InvTarget.CanalEntryTarget1,
            InvTarget.AsgardHUBToSiegePCZTarget,
            InvTarget.AsgardHUBToLokiBossTarget,
            InvTarget.Ch10PagodaFloorBEntryTarget,
            InvTarget.ToolshedHUBEntryTarget,
            InvTarget.Ch10PagodaFloorCEntryTarget,
            InvTarget.Ch10PagodaFloorDEntryTarget,
            InvTarget.Ch10PagodaFloorEEntryTarget,
            InvTarget.Ch10PagodaTopFloorEntryTarget,
            InvTarget.NYCRooftopInvYardUpTarget,
            // TR targets
            InvTarget.TRShantyRooftopsTargetAccess,
            InvTarget.CH05RecCenterExtTarget,   
            InvTarget.ObjectiveAOutsideTarget,
            InvTarget.ObjectiveBOutsideTarget,
            InvTarget.ObjectiveCOutsideTarget,
            InvTarget.TRCarParkTargetAccess,
            InvTarget.TRGameCenterTargetAccess,
            // DailyG
            InvTarget.DailyGTimesSquareHotelDestinationTarget,
            InvTarget.DailyGTimesSquareStreetDestinationTarget,
            InvTarget.DailyGHighTownInvasionHotelDestTarget,
            InvTarget.DrStrangeTimesSquareHotelDestinationTarget,
            InvTarget.DrStrangeTimesSquareStreetDestinationTarget,
            // OneShot
            InvTarget.ZooEmployeeTargetAccess,
            InvTarget.TRSeaWorldTargetAccess,
            InvTarget.TRZooAquariumTargetAccess,
            InvTarget.HydeBossExitTarget,
            InvTarget.ZooJungleInstanceEntryTarget,
            InvTarget.Hydra1ShotBaseEntryTarget,
            InvTarget.Hydra1ShotBossEntryTarget2,
            InvTarget.WakandaP1InRegionEndTarget,
            InvTarget.WakandaP1BossEndTarget,
            // Challange
            InvTarget.UltronBossTargetG,
            InvTarget.AsgardPvPRewardTarget,
            InvTarget.RampToCalderaArrivalTarget,            
            InvTarget.SurturOneWayArrivalTarget,
            InvTarget.SlagOuterExitTarget,
            InvTarget.MonoEntryTarget,
            InvTarget.MoMRightExitArrivalTarget,
            InvTarget.MoMLeftExitArrivalTarget,
            InvTarget.AxisRaidNullifiersEntryTarget,
            InvTarget.BossEntryTarget,
        };

        private static readonly InvTarget[] UnLockedTargets = new InvTarget[]
        {
            InvTarget.XManhattanEntryTarget1to60,
            InvTarget.LokiBossEntryTarget,
            InvTarget.SiegePCZEntryTarget,
            InvTarget.CH0204AIMBaseEntryInteriorTarget,
            InvTarget.CH0207TaskmasterBaseEntryInteriorTarget,
            InvTarget.CH0208BrooklynCanneryStartTarget,
            InvTarget.CH0403MGHStorageFrontInteriorTarget,
            InvTarget.CH0404MGHGarageInteriorFrontTarget,
            InvTarget.CH0404MGHFactoryInteriorFrontTarget,
            InvTarget.CH0404MGHFactoryBossInteriorTarget,
            InvTarget.CH0404MGHFactoryExteriorRearTarget,
            InvTarget.CH0410FiskElevatorAFloor2Target,
            InvTarget.SewersEntryTarget,
            InvTarget.AIMLabToTrainingCampTarget,
            InvTarget.ShieldOutpostEXTTarget,
            InvTarget.NorwayDarkForestTarget,
            InvTarget.AsgardiaInstanceEntryTarget,
            InvTarget.AsgardiaBridgeTarget,
            InvTarget.Ch10PagodaFloorAEntryTarget,
            InvTarget.SovereignHotelRoofEntryTarget,  
            // TR
            InvTarget.CH0205TaskmasterTapeExteriorTarget,
            InvTarget.CH05MutantWarehouseExtEntry,
            // Invisible Exit
            InvTarget.HydeBossEntryTarget,
            InvTarget.MoMCenterEntryTarget,
        };

        private static readonly BlockerEntity[] Blockers = new BlockerEntity[]
        {
            BlockerEntity.GateBlockerRaftLivingLaser,
            BlockerEntity.DestructibleExitDoors,
            BlockerEntity.SurturRaidGateBlockerEntityMONO,
            BlockerEntity.SurturRaidGateBlockerEntityMOM,
            BlockerEntity.SurturRaidGateBlockerEntitySLAG,
            BlockerEntity.SurturRaidGateBlockerEntitySURT
        };

        public enum BlockerEntity : ulong
        {
            GateBlockerRaftLivingLaser = 12353403066566515268,
            DestructibleExitDoors = 15556708167322245112,
            SurturRaidGateBlockerEntityMONO = 14264436868519894710,
            SurturRaidGateBlockerEntityMOM = 7506253403374886470,
            SurturRaidGateBlockerEntitySLAG = 2107982419118661284,
            SurturRaidGateBlockerEntitySURT = 7080009510741745355,
            BeastNorway = 1946034969610490335, // remove second 
        }

        public enum InvTarget : ulong
        {
            // NPEAvengersTowerHUBRegion
            BazaarFromAvengersTowerHubTarget = 15895543318574475572,
            XManhattanEntryTarget1to60 = 2635481312889807924,
            SubterraL5EntryTarget = 17508088629154751698,
            UESvsDinosEntryTarget = 11375015409704837543,
            XMansionEntry = 2908608236307814449,
            RaftHelipadEntryTarget = 1419217567326872169,
            MadripoorMainEntryTarget = 5578214614276448404,
            CH0201ShippingyardEntryInteriorTarget = 14988590400532456514,
            CH0401Respawn01Target = 13122305741551771460,
            CH01HKSouthRooftopPlayerStart = 11237793595509253006,
            // CH0106KPWarehouseRegion
            WarehouseBossExteriorTarget = 10254792218958897947,
            // CH0201ShippingYardRegion
            CH0202HoodContainerInteriorTarget = 9608365637530952300,
            CH0204AIMBaseEntryInteriorTarget = 4355410365228982789,            
            // CH0205ConstructionRegion
            CH0205TaskmasterTapeInteriorTarget = 11803462739553362667,
            CH0207TaskmasterBaseEntryInteriorTarget = 10108529194301596944,
            // CH0206TaskmasterVHSTapeConstructionRegion
            CH0205TaskmasterTapeExteriorTarget = 17617241741145481969,
            // CH0207TaskmasterRegion
            CH0208BrooklynCanneryStartTarget = 17210189397093720615,
            // CH0209HoodsHideoutRegion
            NPEAvengersTowerHubEntry = 11334277059865941394,
            // CH0401LowerEastRegion
            CH0403MGHStorageFrontInteriorTarget = 15735845837126443530,
            CH0404MGHGarageInteriorFrontTarget = 7726391931552539005,
            // CH0403MGHStorageRegion
            CH0403MGHStorageRearExteriorTarget = 4325468468211556753,
            // CH0404MGHFactoryRegion
            CH0404MGHFactoryInteriorFrontTarget = 11511146138818388494,
            CH0404MGHFactoryBossInteriorTarget = 12796241511874961820,
            CH0404MGHFactoryExteriorRearTarget = 11806303983103713685,
            // CH0402UpperEastRegion
            CH0408MobRearInteriorTarget = 11895422811237195421, // Exit from Bistro
            // CH0408MaggiaRestaurantRegion
            CH0408MobRearExteriorTarget = 17484766862257757859,
            // CH0410FiskTowerRegion
            CH0410FiskElevatorAFloor2Target = 12440915086139596806,
            // CH0501MutantTownRegion
            SewersEntryTarget = 14627094356933614733,
            // CH0502MutantWarehouseRegion
            CH05MutantWarehouseExtEntry = 241887477377605722,
            // CH0503SupervillainRecCenterRegion
            CH05RecCenterExtTarget = 10769034612749049342,
            // CH0504PurifierChurchRegion
            XMansionBodySliderTarget = 10156365377106549943,
            // XaviersMansionRegion
            FortStrykerEntryTarget = 3997829106751906038,
            SlumsEntryTarget = 8492855896331000872,
            JungleEntryTarget = 9647739674975804916,
            HelicarrierEntryTarget = 1423746992940784646,
            // CH0604AIMWeaponsLabRegion
            AIMLabToTrainingCampTarget = 4821103317906694715,
            // CH0702SauronCavesRegion
            SauronCavesEXTEntryTarget = 8070077825000284522,
            // CH0703BroodCavesRegion
            BroodCavesBossINT = 4234011923218898400,
            BroodCavesBossEXT = 4854258685993491942,
            BroodCavesEXTEntryTarget = 685085247747793128,
            // CH0704SHIELDScienceStationRegion
            ShieldOutpostEXTTarget = 15087385534041563173,
            // CH0706MutateCavesRegion
            GeneModBossEXT = 12972244072243797149,
            // CH0707SinisterLabRegion
            GeneModEXITPortalTarget = 1472763862505496680,
            XMansionBlackbirdWaypoint = 8769531952491273496,
            // HelicarrierRegion
            NorwayPCZEntryTarget = 15602868991888858554,
            AIMWeaponFacExteriorEntryTarget = 725811344567509511,
            HYDRAIslandLVL1Entry = 5599790498739985717,
            LatveriaPCZEntryTarget = 14533918910337458007, 
            ResearchCorridorEntryTarget = 6216551702482332010,
            // CH0801AIMWeaponFacilityRegion
            AIMWeapFacToMODOKTarget = 10175636343744765571,
            // CH0805LatveriaPCZObjectiveARegion
            ObjectiveAOutsideTarget = 18226452563201630105,
            // CH0806LatveriaPCZObjectiveBRegion
            ObjectiveBOutsideTarget = 11884989666270388122,
            // CH0807LatveriaPCZObjectiveCRegion
            ObjectiveCOutsideTarget = 2723149482678296475,
            // CH0901NorwayPCZRegion
            NorwayDarkForestTarget = 11767373299566321264,
            AsgardiaInstanceEntryTarget = 15288093230286381150,
            // CH0903AsgardiaInstanceRegion
            AsgardiaBridgeTarget = 3437800305839709322,
            // AsgardiaRegion
            LokiBossEntryTarget = 11180315199281962291,
            SiegePCZEntryTarget = 12537192004833254695,
            // CH0904SiegePCZRegion
            CanalEntryTarget1 = 8738154874827447293,
            // CH0905CanalRegion
            AsgardHUBToSiegePCZTarget = 14934666025878298319,
            // CH0906LokiBossRegion
            LokiBossPhaseTwoTarget = 15469485961670041196,
            AsgardHUBToLokiBossTarget = 6343094346979221211,
            // MadripoorInvasionRegion
            Ch10PagodaFloorAEntryTarget = 589822349659285856,
            // HandDojoRegion
            Ch10PagodaFloorBEntryTarget = 3555544683987675489,
            ToolshedHUBEntryTarget = 17221238477572612404,
            Ch10PagodaFloorCEntryTarget = 2774131390855457122,
            Ch10PagodaFloorEEntryTarget = 6093066063172282724,
            Ch10PagodaTopFloorEntryTarget = 12955753604043975250,
            Ch10PagodaFloorDEntryTarget = 4874741483775273315,
            // NYCRooftopInvRegion
            NYCRooftopInvYardUpTarget = 4811369732915800844,
            // UpperMadripoorRegionL60
            SovereignHotelRoofEntryTarget = 14195072671160937196,
            // TRShantyRooftopsRegion
            TRShantyRooftopsTargetAccess = 15095574082967449674,
            // TRCarParkRegion
            TRCarParkTargetAccess = 6385558882715249947,
            // TRGameCenterRegion
            TRGameCenterTargetAccess = 9921007488688860754,
            // DailyGTimesSquareRegionL60
            DailyGTimesSquareHotelDestinationTarget = 7040225978500524304,
            DailyGTimesSquareStreetDestinationTarget = 4857786726277785995,
            // DailyGHighTownInvasionRegionL60
            DailyGHighTownInvasionHotelDestTarget = 12087150614603311702,
            // DrStrangeTimesSquareRegionL60
            DrStrangeTimesSquareHotelDestinationTarget = 17730529484168572441,
            DrStrangeTimesSquareStreetDestinationTarget = 5991922233338179220,
            // BronxZooRegionL60
            TRSeaWorldTargetAccess = 3132193544495836603,
            HydeBossExitTarget = 8006010142029130269,
            HydeBossEntryTarget = 10506910663675357845,
            ZooJungleInstanceEntryTarget = 18422336131966250598,
            TRZooAquariumTargetAccess = 3981908949025697563,
            ZooEmployeeTargetAccess = 5081698796864680364,
            // HYDRAIslandPartDeuxRegionL60
            Hydra1ShotBaseEntryTarget = 4042445796194922837,
            Hydra1ShotBossEntryTarget2 = 9308897719218876785,
            // WakandaP1RegionL60
            WakandaP1InRegionEndTarget = 1487192153901117002,
            WakandaP1BossEndTarget = 10099148393087708326,
            // UltronRaidRegionGreen
            UltronBossTargetG = 7879625668095978348,
            // PvPDefenderTier5Region
            AsgardPvPRewardTarget = 3428180377327967290,
            // SurturRaidRegionGreen
            RampToCalderaArrivalTarget = 8879645719174783216,
            MoMRightExitArrivalTarget = 12919759216768590914,
            SurturOneWayArrivalTarget = 3891105581744136409,
            SlagOuterExitTarget = 7068172508433490462,
            MonoEntryTarget = 2747204676526481547,
            MoMCenterEntryTarget = 1963663328097935916,
            MoMLeftExitArrivalTarget = 8318676114763097039,
            // AxisRaidRegionGreen
            AxisRaidNullifiersEntryTarget = 8684857023547056096,
            BossEntryTarget = 6193630514385067431,
        };

        private PrototypeId GetVisibleParentRef(PrototypeId invisibleId)
        {
            WorldEntityPrototype invisibleProto = GameDatabase.GetPrototype<WorldEntityPrototype>(invisibleId);
            if (invisibleProto.VisibleByDefault == false) return GetVisibleParentRef(invisibleProto.ParentDataRef);
            return invisibleId;
        }
        #endregion
    }
}
