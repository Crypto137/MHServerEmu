using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.UI;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.Regions
{
    public class RegionManager
    {
        public static bool GenerationAsked;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Region, 0);

        private readonly EntityManager _entityManager;
        private static readonly Dictionary<RegionPrototypeId, Region> _regionDict = new();

        public static void ClearRegionDict() => _regionDict?.Clear();

        //----------
        private uint _cellId;
        private uint _areaId;
        private readonly Dictionary<uint, Cell> _allCells = new();
        private readonly Dictionary<ulong, Region> _allRegions = new();
        private readonly Dictionary<ulong, Region> _matches = new();
        public Game Game { get; private set; }
        private readonly object _managerLock = new();
        public RegionManager(EntityManager entityManager)
        {
            _entityManager = entityManager;
            _areaId = 1;
            _cellId = 1;
        }

        public bool Initialize(Game game)
        {
            Game = game;
            return true;
        }

        public uint AllocateCellId() => _cellId++;
        public uint AllocateAreaId() => _areaId++;

        public bool AddCell(Cell cell)
        {
            if (cell != null && _allCells.ContainsKey(cell.Id) == false)
            {
                _allCells[cell.Id] = cell;
                if (cell.Area.Log) Logger.Trace($"Adding cell {cell} in region {cell.GetRegion()} area id={cell.Area.Id}");
                return true;
            }
            return false;
        }

        public Cell GetCell(uint cellId)
        {
            if (_allCells.TryGetValue(cellId, out var cell)) return cell;
            return null;
        }

        public bool RemoveCell(Cell cell)
        {
            if (cell == null) return false;
            if (cell.Area.Log) Logger.Trace($"Removing cell {cell} from region {cell.GetRegion()}");

            if (_allCells.ContainsKey(cell.Id))
            {
                _allCells.Remove(cell.Id);
                return true;
            }
            return false;
        }

        public Region CreateRegion(RegionSettings settings)
        {
            if (settings.RegionDataRef == 0) return null;

            ulong instanceAddress = settings.InstanceAddress;
            if (instanceAddress == 0 || GetRegion(instanceAddress) != null) return null;

            Region region = new(Game);
            if (region == null) return null;

            _allRegions[instanceAddress] = region;

            RegionSettings initSettings = settings; // clone?
            initSettings.InstanceAddress = instanceAddress;

            if (region.Initialize(initSettings) == false)
            {
                _allRegions.Remove(instanceAddress);
                region.Shutdown();
                return null;
            }

            if (region.GetMatchNumber() != 0)
                _matches[region.GetMatchNumber()] = region;

            return region;
        }

        public Region EmptyRegion(RegionPrototypeId prototype)
        {
            Region region = new(prototype, 1210027349,
             Array.Empty<byte>(),
             new(10, DifficultyTier.Normal));
            region.Bound = Aabb.Zero;
            return region;
        }

        public Region GenerateRegion(RegionPrototypeId prototype) 
        {
            RegionSettings settings = new()
            {
                Seed = Game.Random.Next(),
                DifficultyTierRef = (PrototypeId)DifficultyTier.Normal,
                InstanceAddress = _idGenerator.Generate(),
                Level = 10,
                Bound = Aabb.Zero,
                GenerateAreas = true,
                GenerateEntities = true,
                GenerateLog = false,
                Affixes = new List<PrototypeId>(),
                RegionDataRef = (PrototypeId)prototype
            };
            // settings.Seed = 1210027349;
            // GRandom random = new(settings.Seed);//Game.Random.Next()
            int tries = 10;
            Region region = null;
            while (region == null && (--tries > 0))
            {
                if (tries < 9) settings.Seed = Game.Random.Next(); // random.Next(); 
                region = CreateRegion(settings);
            }

            if (region == null)
                Logger.Error($"GenerateRegion failed after {10 - tries} attempts | regionId: {prototype} | Last Seed: {settings.Seed}");

            return region;
        }

        // NEW
        public Region GetRegion(ulong id)
        {
            if (id == 0) return null;
            lock (_managerLock)
            {
                if (_allRegions.TryGetValue(id, out Region region))
                    return region;
            }
            return null;
        }

        public static Region GetRegion(Game game, ulong id)
        {
            if (game == null) return null;
            RegionManager regionManager = game.RegionManager;
            if (regionManager == null) return null;
            return regionManager.GetRegion(id);
        }

        // OLD
        public Region GetRegion(RegionPrototypeId prototype)
        {
            //prototype = RegionPrototypeId.NPEAvengersTowerHUBRegion;
            lock (_managerLock)
            {
                if (_regionDict.TryGetValue(prototype, out Region region) == false)
                {
                    // Generate the region and create entities for it if needed
                    ulong numEntities = _entityManager.PeekNextEntityId();
                    Logger.Debug($"GenerateRegion {GameDatabase.GetFormattedPrototypeName((PrototypeId)prototype)}");
                    try
                    {
                        region = GenerateRegion(prototype);
                    } 
                    catch(Exception e) 
                    {
                        Logger.ErrorException(e, "Generation failed");
                    }                    
                    // region = EmptyRegion(prototype);
                    if (region != null)
                    {
                        region.ArchiveData = GetArchiveData(prototype);
                        _entityManager.HardcodedEntities(region);
                        ulong entities = _entityManager.PeekNextEntityId() - numEntities;
                        Logger.Debug($"Entities generated = {entities} [{region.EntitySpatialPartition.TotalElements}]");
                        region.CreatedTime = DateTime.Now;

                        _regionDict.Add(prototype, region);
                    }
                }

                return region;
            }
        }

        private const int CleanUpTime = 60 * 1000 * 5; // 5 minutes
        private const int UnVisitedTime = 5; // 5 minutes

        public async Task CleanUpRegionsAsync()
        {            
            while (true)
            {
                CleanUpRegions();
                await Task.Delay(CleanUpTime); 
            }
        }

        private void CleanUpRegions()
        {
            lock (_managerLock)
            {
                if (_allRegions.Count == 0) return;
            }            
            var currentTime = DateTime.Now;
            Logger.Debug($"CleanUp");

            // Get PlayerRegions
            HashSet<RegionPrototypeId> playerRegions = new();
            foreach (var playerConnection in Game.NetworkManager.TempRemoveMeIterateConnections())
            {
                var regionRef = (RegionPrototypeId)playerConnection.RegionDataRef; // TODO use RegionID
                playerRegions.Add(regionRef); 
            }

            // Check all regions 
            List<Region> toShutdown = new();
            lock (_managerLock)
            {
                foreach (Region region in _allRegions.Values)
                {
                    DateTime visitedTime;
                    lock (region.Lock)
                    {
                        visitedTime = region.VisitedTime;
                    }
                    TimeSpan timeDifference = currentTime - visitedTime;

                    if (playerRegions.Contains(region.PrototypeId)) // TODO RegionId
                    {
                        // TODO send force exit from region to Players
                    }
                    else
                    {
                        // TODO check all active local teleport to this Region
                        if (timeDifference.TotalMinutes > UnVisitedTime)
                            toShutdown.Add(region);
                    }
                }
            }

            // ShoutDown all unactived regions
            foreach (Region region in toShutdown)
            {
                lock (_managerLock)
                {
                    _allRegions.Remove(region.Id);
                    _regionDict.Remove(region.PrototypeId);
                }
                TimeSpan lifetime = DateTime.Now - region.CreatedTime;
                string formattedLifetime = string.Format("{0:%m} min {0:%s} sec", lifetime);
                Logger.Warn($"Shutdown region = {region}, Lifetime = {formattedLifetime}");
                region.Shutdown();                
            }

        }

        #region Hardcoded

        private static byte[] GetArchiveData(RegionPrototypeId prototype)
        {
            // Dumped repIds: NPEAvengersTowerHUBRegion == 41192, DangerRoomHubRegion == 36264, XManhattanRegion60Cosmic == 18383
            RegionArchive regionArchive = new(9000000);

            switch (prototype)
            {
                // dumped
                case RegionPrototypeId.NPEAvengersTowerHUBRegion:
                    regionArchive.Properties[PropertyEnum.BonusItemFindBonusDifficultyMult] = 6;
                    regionArchive.Properties[PropertyEnum.DifficultyTier] = (PrototypeId)DifficultyTier.Normal;   // Difficulty/Tiers/Tier1Normal.prototype

                    // Missions/Prototypes/BonusMissions/WeeklyEventMissions/CosmicWeekMissionController.prototype
                    regionArchive.MissionManager.InsertMission(new((PrototypeId)9033745879814514175, 0x64A2F98F));

                    // Missions/Prototypes/Hubs/AvengersTower/Discoveries/BenUrichAndJessicaJones.prototype
                    regionArchive.MissionManager.InsertMission(new((PrototypeId)12163941636897120859, 0x7094021E));

                    // Missions/Prototypes/BonusMissions/WeeklyEventMissions/OmegaMissionVendorController.prototype
                    regionArchive.MissionManager.InsertMission(new((PrototypeId)15923097160974411372, 0x5535A91E));

                    // Missions/Prototypes/BonusMissions/WeeklyEventMissions/ARMORWeekMissionController.prototype
                    regionArchive.MissionManager.InsertMission(new((PrototypeId)16864913917856392610, 0x7297046B));

                    break;

                case RegionPrototypeId.XaviersMansionRegion:
                    /*
                    area.CellList[17].AddEncounter(15374827165380448803, 4, true);
                    area.CellList[15].AddEncounter(8642336607468261979, 7, true);
                    area.CellList[23].AddEncounter(4065272706848002543, 3, true);
                    area.CellList[10].AddEncounter(12198525011368022752, 1, true);
                    */
                    break;

                // dumped
                case RegionPrototypeId.DangerRoomHubRegion:
                    regionArchive.Properties[PropertyEnum.BonusItemFindBonusDifficultyMult] = 9;
                    regionArchive.Properties[PropertyEnum.DamageRegionMobToPlayer] = 4f;
                    regionArchive.Properties[PropertyEnum.DamageRegionPlayerToMob] = 0.3f;
                    regionArchive.Properties[PropertyEnum.DifficultyTier] = (PrototypeId)DifficultyTier.Heroic;   // Difficulty/Tiers/Tier2Heroic.prototype
                    regionArchive.Properties[PropertyEnum.ExperienceBonusPct] = 0.9f;
                    regionArchive.Properties[PropertyEnum.LootBonusCreditsPct] = 0.9f;
                    regionArchive.Properties[PropertyEnum.LootBonusXPPct] = 0.9f;

                    break;

                // dumped
                case RegionPrototypeId.XManhattanRegion60Cosmic:
                    regionArchive.Properties[PropertyEnum.BonusItemFindBonusDifficultyMult] = 13;
                    regionArchive.Properties[PropertyEnum.DamageRegionMobToPlayer] = 8f;
                    regionArchive.Properties[PropertyEnum.DamageRegionPlayerToMob] = 0.13f;
                    regionArchive.Properties[PropertyEnum.DifficultyTier] = (PrototypeId)DifficultyTier.Superheroic;  // Difficulty/Tiers/Tier3Superheroic.prototype
                    regionArchive.Properties[PropertyEnum.ExperienceBonusPct] = 2.4f;
                    regionArchive.Properties[PropertyEnum.LootBonusCreditsPct] = 2.4f;
                    regionArchive.Properties[PropertyEnum.LootBonusXPPct] = 2.4f;

                    #region Missions

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)3667304362440335589,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventNinjas.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518100500000),
                        (PrototypeId)5751045088227960741,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownPlayingBaseball.prototype
                        0x7912D38B,
                        new MissionObjective[] { new(0x0, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518149500000),
                        (PrototypeId)9358696252006540917,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownCalisthenics.prototype
                        0x6A7A309C,
                        new MissionObjective[] { new(0x0, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)3672161157557464473,   // Missions/Prototypes/PVEEndgame/StaticScenarios/ControllerChecks/CivilWarDeathCheckOpenWorldCrossbones.prototype
                        0x62300577,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 535777, 561178, 572318, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)5826455758384603593,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventDoombots.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x5, 0x5, 0x0, 0x0),
                            new(0x1, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)9898965535468037097,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventFrightfulFour.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)15008190206338409193,  // Missions/Prototypes/Achievements/Challenges/AchievementPunisherMidtownTimed.prototype
                        0x5957A7E7,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x14, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Failed,
                        TimeSpan.Zero,
                        (PrototypeId)4425023419088315138,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventBrotherhood.prototype
                        0x48F91EAB,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x1, 0x0, 0x0),
                            new(0x5, MissionObjectiveState.Failed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518186500000),
                        (PrototypeId)7753181439701952032,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownTrafficStop.prototype
                        0x460C3BFD,
                        new MissionObjective[] { new(0x0, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Failed,
                        new(1613518556500000),
                        (PrototypeId)8164724054894847468,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownCarAccident.prototype
                        0x75A7CBA2,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, 0x0, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 594073, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)8733075101353582604,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventHood.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)15924448993393518092,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownBankRobbery.prototype
                        0x743466DB,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x3, 0x3, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)8492342449826966212,   // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownArsonistsAgainstPolice.prototype
                        0x187EEF19,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)10149215495678468529,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMalekith.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)10315340690132511222,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownGroceryThugsStandoff.prototype
                        0xF48EB45,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)10490887443555427166,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMegaSentinel.prototype
                        0x39D7DF60,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x2, 0x4, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Active, new(1613519345000000), Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)10686139501369173095,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMisterSinister.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)15354703907328566055,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventSinisterSix.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x4, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x5, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x6, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Inactive,
                        TimeSpan.Zero,
                        (PrototypeId)11060757254749691901,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventBase.prototype
                        0x0,
                        Array.Empty<MissionObjective>(),
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)12166721969729972671,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownMaggiaShakedownStand.prototype
                        0x140643F5,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Available, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        Array.Empty<ulong>(),
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Active,
                        TimeSpan.Zero,
                        (PrototypeId)15669697143252068358,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Discoveries/Midtown/MidtownMaggiaKidnapping.prototype
                        0x33D3263B,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Skipped, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Skipped, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 572318, 601433, },
                        false));

                    regionArchive.MissionManager.InsertMission(new(MissionState.Completed,
                        TimeSpan.Zero,
                        (PrototypeId)17742095572202693358,  // Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventGreenGoblin.prototype
                        0x0,
                        new MissionObjective[] { new(0x0, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x1, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x2, MissionObjectiveState.Completed, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x1, 0x1, 0x0, 0x0),
                            new(0x3, MissionObjectiveState.Invalid, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0) },
                        new ulong[] { 491876, 516140, 535777, 545031, 547169, 561178, 572318, 573260, 579997, 594073, 596593, 600031, 601433, 609423, },
                        false));

                    #endregion

                    // Widget: UI/MetaGame/MissionName.prototype
                    // Context: Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMegaSentinel.prototype
                    var missionTextWidget = regionArchive.UIDataProvider.GetWidget<UIWidgetMissionText>((PrototypeId)7164846210465729875, (PrototypeId)10490887443555427166);
                    missionTextWidget.SetText((LocaleStringId)8188822000559654203, LocaleStringId.Invalid);

                    // Widget: UI/MetaGame/TimeRemainingStoryMode2.prototype
                    // Context: Missions/Prototypes/PVEEndgame/PatrolMidtown/Events/MidtownEventMegaSentinel.prototype
                    var genericFractionWidget = regionArchive.UIDataProvider.GetWidget<UIWidgetGenericFraction>((PrototypeId)11932510257277768241, (PrototypeId)10490887443555427166);
                    genericFractionWidget.SetCount(1, 1);
                    genericFractionWidget.SetTimeRemaining(251550);

                    break;

                // custom
                case RegionPrototypeId.SurturRaidRegionGreen:
                    var deathsWidget = regionArchive.UIDataProvider.GetWidget<UIWidgetGenericFraction>((PrototypeId)11858833950498362027, PrototypeId.Invalid);
                    deathsWidget.SetCount(30, 30);
                    regionArchive.UIDataProvider.GetWidget<UIWidgetEntityIconsSyncData>((PrototypeId)1133155547537679647, PrototypeId.Invalid);
                    regionArchive.UIDataProvider.GetWidget<UIWidgetEntityIconsSyncData>((PrototypeId)478583290767352422, PrototypeId.Invalid);

                    break;
            }

            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)regionArchive.ReplicationPolicy))
            {
                regionArchive.Serialize(archive);
                return archive.AccessAutoBuffer().ToArray();
            }
        }

        public static RegionPrototypeId[] PatrolRegions = new RegionPrototypeId[]
        {
            RegionPrototypeId.XManhattanRegion1to60,
            RegionPrototypeId.XManhattanRegion60Cosmic,
            RegionPrototypeId.BrooklynPatrolRegionL60,
            RegionPrototypeId.BrooklynPatrolRegionL60Cosmic,
            RegionPrototypeId.UpperMadripoorRegionL60,
            RegionPrototypeId.UpperMadripoorRegionL60Cosmic,
        };
        #endregion
    }
}

