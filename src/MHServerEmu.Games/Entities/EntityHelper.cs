using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    /// <summary>
    /// A helper class for managing hardcoded entities. TODO: Gradually get rid of stuff in here.
    /// </summary>
    public static class EntityHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        

        public enum TestOrb : ulong
        {
            Red = 925659119519994384, // HealOrbItem = 925659119519994384, 
            BigRed = 18107188791044543532, // LimboBuffOrbItem = 18107188791044543532,
            Greeen = 16852724980331648695, // ExperienceOrbSmallItem = 16852724980331648695,
            BigGreen = 3442167663578518146, // LegendaryOrb = 3442167663578518146,
            Blue = 9607833165236212779, // EnduranceOrbItem = 9607833165236212779
            Orange = 8905675869072986929, // TestOnlyXPOrb = 8905675869072986929,
            XRay = 5358798066155328438, // Radioactive31Orb = 5358798066155328438,
            Pink = 14631580738344719410, // ManhattanOrbItem = 14631580738344719410,
            Hyde = 1644714682932532551, // Art252HydeFormulaOrb = 1644714682932532551,
            Violet = 18337403507337860830, // MagnetoMetalOrb = 18337403507337860830,
        }

        public static Agent CrateOrb(TestOrb orbProto, Vector3 position, Region region)
        {
            var settings = new EntitySettings
            {
                EntityRef = (PrototypeId)orbProto,
                Position = position,
                Orientation = new(3.14f, 0.0f, 0.0f),
                RegionId = region.Id,
                Lifespan = TimeSpan.FromSeconds(3),
                Properties = new()
                {
                    [PropertyEnum.AIStartsEnabled] = false
                }
            };
            var game = region.Game;
            Agent orb = (Agent)game.EntityManager.CreateEntity(settings);
            return orb;
        }

        public static void SummonEntityFromPowerPrototype(Avatar avatar, SummonPowerPrototype summonPowerProto)
        {
            AssetId creatorAsset = avatar.GetEntityWorldAsset();
            PrototypeId allianceRef = avatar.Alliance.DataRef;

            if (summonPowerProto.SummonEntityContexts.IsNullOrEmpty()) return;
            PrototypeId summonerRef = summonPowerProto.SummonEntityContexts[0].SummonEntity;
            var summonerProto = GameDatabase.GetPrototype<AgentPrototype>(summonerRef);

            var settings = new EntitySettings
            {
                EntityRef = summonerRef,
                Properties = new PropertyCollection
                {
                    [PropertyEnum.NoMissileCollide] = true, // EvalOnCreate
                    [PropertyEnum.CreatorEntityAssetRefBase] = creatorAsset,
                    [PropertyEnum.CreatorEntityAssetRefCurrent] = creatorAsset,
                    [PropertyEnum.CreatorPowerPrototype] = summonPowerProto.DataRef,
                    [PropertyEnum.SummonedByPower] = true,
                    [PropertyEnum.AllianceOverride] = allianceRef,
                    [PropertyEnum.Rank] = summonerProto.Rank,
                }
            };

            Agent summoner = (Agent)avatar.Game.EntityManager.CreateEntity(settings);
            EntitySettings setting = new() { OptionFlags = EntitySettingsOptionFlags.IsNewOnServer};
            summoner.EnterWorld(avatar.Region, summoner.GetPositionNearAvatar(avatar), avatar.RegionLocation.Orientation, setting);

            if (summonPowerProto.ActionsTriggeredOnPowerEvent.HasValue())
                summoner.AIController.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = avatar.Id;
            summoner.Properties[PropertyEnum.PowerUserOverrideID] = avatar.Id;

            Inventory summonedInventory = avatar.GetInventory(InventoryConvenienceLabel.Summoned);
            summoner.ChangeInventoryLocation(summonedInventory);
        }

        public static void DestroySummonerFromPowerPrototype(Avatar avatar, SummonPowerPrototype summonPowerProto)
        {
            var summonerProto = summonPowerProto.GetSummonEntity(0, avatar.GetOriginalWorldAsset());
            Inventory summonedInventory = avatar.GetInventory(InventoryConvenienceLabel.Summoned);
            Agent summoner = summonedInventory.GetMatchingEntity(summonerProto.DataRef) as Agent;
            summoner?.Destroy();
        }

        public static void SetUpHardcodedEntities(Region region)
        {
            //CellPrototype entry;
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

            }

            // Hack mode for Off Teleports / Blocker
            List<WorldEntity> blockers = new();
            foreach (var entity in region.IterateEntitiesInVolume(region.Bound, new()))
            {
                if (entity is Transition teleport)
                {
                    if (teleport.DestinationList.Count > 0 && teleport.DestinationList[0].Type == RegionTransitionType.Transition)
                    {
                        var teleportProto = teleport.TransitionPrototype;
                        if (teleportProto.VisibleByDefault == false) // To fix
                        {
                            // Logger.Debug($"[{teleport.Location.GetPosition()}][InvT]{GameDatabase.GetFormattedPrototypeName(teleport.Destinations[0].Target)} = {teleport.Destinations[0].Target},");
                            if (LockedTargets.Contains((InvTarget)teleport.DestinationList[0].TargetRef) == false) continue;
                            if ((InvTarget)teleport.DestinationList[0].TargetRef == InvTarget.NPEAvengersTowerHubEntry && region.PrototypeId == RegionPrototypeId.NPERaftRegion) continue;
                            PrototypeId visibleParent = GetVisibleParentRef(teleportProto.ParentDataRef);
                            entity.TEMP_ReplacePrototype(visibleParent);
                            continue;
                        }
                        // Logger.Debug($"[T]{GameDatabase.GetFormattedPrototypeName(teleport.Destinations[0].Target)} = {teleport.Destinations[0].Target},");
                    }
                }
                else if (Blockers.Contains((BlockerEntity)entity.PrototypeDataRef))
                {
                    blockers.Add(entity);
                }

            }
            foreach (var entity in blockers) entity.ExitWorld();

        }

        #region HardCodeRank

        public static float GetHealthForWorldEntity(WorldEntity worldEntity)
        {
            WorldEntityPrototype worldEntityProto = worldEntity.WorldEntityPrototype;

            if (worldEntityProto is PropPrototype)
                return 200f;

            if (worldEntityProto is SpawnerPrototype || worldEntityProto is HotspotPrototype)
                return 0f;

            switch (worldEntity.GetRankPrototype().Rank)
            {
                case Rank.Popcorn:  return 600f;
                case Rank.Champion: return 800f;
                case Rank.Elite:    return 1000f;
                case Rank.MiniBoss: return 1500f;
                case Rank.Boss:     return 2000f;
                default:            return 1000f;
            }
        }

        #endregion

        private static PrototypeId GetVisibleParentRef(PrototypeId invisibleId)
        {
            WorldEntityPrototype invisibleProto = GameDatabase.GetPrototype<WorldEntityPrototype>(invisibleId);
            if (invisibleProto.VisibleByDefault == false) return GetVisibleParentRef(invisibleProto.ParentDataRef);
            return invisibleId;
        }

        public static readonly InvSpawner[] InvSpawners = new InvSpawner[]
        {
            InvSpawner.OperationsBountyChestSpawnerA100,
            InvSpawner.OperationsBountyChestSpawnerB225,
            InvSpawner.OperationsBountyChestSpawnerC350,
            InvSpawner.DrDoomPhase2StarryExpanseSpawner,
            InvSpawner.DrDoomPhase3StarryExpanseSpawner,
            InvSpawner.DrDoomPhase2SpawnerEGc,
            InvSpawner.DrDoomPhase2SpawnerEGr,
            InvSpawner.DrDoomPhase3SpawnerEGc,
            InvSpawner.DrDoomPhase3SpawnerEGr,
        };

        public enum InvSpawner : ulong
        {
            OperationsBountyChestSpawnerA100 = 18164666176037329599,
            OperationsBountyChestSpawnerB225 = 11588501183449012936,
            OperationsBountyChestSpawnerC350 = 16538587082423278280,
            DrDoomPhase2SpawnerEGc = 4693116097212523005,
            DrDoomPhase2SpawnerEGr = 3339796473761636876,
            DrDoomPhase3SpawnerEGc = 16067664414561149438,
            DrDoomPhase3SpawnerEGr = 12791301204692902413,
            DrDoomPhase2StarryExpanseSpawner = 8902365322555041383,
            DrDoomPhase3StarryExpanseSpawner = 8461250813795575400,
        }


        private static readonly InvTarget[] LockedTargets = new InvTarget[]
        {
            InvTarget.NPETrainingRoomEntry,
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
            BlockerEntity.SurturRaidGateBlockerEntitySURT,
            BlockerEntity.SurturRaidGateBlockerEntitySURT2,
            BlockerEntity.OperationsBountyChestA,
            BlockerEntity.OperationsBountyChestB,
            BlockerEntity.OperationsBountyChestC,
        };

        public enum BlockerEntity : ulong
        {
            GateBlockerRaftLivingLaser = 12353403066566515268,
            DestructibleExitDoors = 15556708167322245112,
            SurturRaidGateBlockerEntityMONO = 14264436868519894710,
            SurturRaidGateBlockerEntityMOM = 7506253403374886470,
            SurturRaidGateBlockerEntitySLAG = 2107982419118661284,
            SurturRaidGateBlockerEntitySURT = 7080009510741745355,
            SurturRaidGateBlockerEntitySURT2 = 17385248028568526589,
            // Off BounntyChest
            OperationsBountyChestA = 8947265512402064759,
            OperationsBountyChestB = 16557893689139991928,
            OperationsBountyChestC = 2614246491109856633,
        }

        public enum InvTarget : ulong
        {
            // NPERaftRegion
            NPETrainingRoomEntry = 7210609263143097312,
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
    }
}
