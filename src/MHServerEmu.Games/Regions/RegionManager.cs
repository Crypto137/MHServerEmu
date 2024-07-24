using System.Collections.Concurrent;
using System.Diagnostics;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.Regions
{
    public class RegionManager
    {
        public static bool GenerationAsked;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Region, 0);

        private static readonly Dictionary<RegionPrototypeId, Region> _regionDict = new();

        private ConcurrentQueue<(Region, PlayerConnection)> _generationQueue = new();
        private ConcurrentQueue<Region> _shutdownQueue = new();

        public static void ClearRegionDict() => _regionDict?.Clear();
        public IEnumerable<Region> AllRegions => _allRegions.Values;

        //----------
        private uint _cellId;
        private uint _areaId;
        private readonly Dictionary<uint, Cell> _allCells = new();
        private readonly Dictionary<ulong, Region> _allRegions = new();
        private readonly Dictionary<ulong, Region> _matches = new();
        public Game Game { get; private set; }
        private readonly object _managerLock = new();

        public RegionManager()
        {
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
                if (cell.Area.GenerateLog)
                    Logger.Trace($"Adding cell {cell} in region {cell.Region} area id={cell.Area.Id}");
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

            if (cell.Area.GenerateLog)
                Logger.Trace($"Removing cell {cell} from region {cell.Region}");

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

            if (region.MatchNumber != 0)
                _matches[region.MatchNumber] = region;

            return region;
        }

        public Region EmptyRegion(RegionPrototypeId prototype) // For test
        {
            Region region = new(Game);
            region.InitEmpty(prototype, 1210027349);
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

        public void ProcessPendingRegions()
        {
            // Process regions that need to be shut down
            while (_shutdownQueue.TryDequeue(out Region region))
            {
                TimeSpan lifetime = DateTime.Now - region.CreatedTime;
                string formattedLifetime = string.Format("{0:%m} min {0:%s} sec", lifetime);
                Logger.Info($"Shutdown region = {region}, Lifetime = {formattedLifetime}");
                region.Shutdown();
            }
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
                    ulong numEntities = Game.EntityManager.PeekNextEntityId();
                    Logger.Info($"Generating region {((PrototypeId)prototype).GetNameFormatted()}...");

                    try
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        region = GenerateRegion(prototype);
                        Logger.Info($"Generated region {prototype} in {stopwatch.ElapsedMilliseconds} ms");
                    } 
                    catch(Exception e) 
                    {
                        Logger.ErrorException(e, "Generation failed");
                    }   
                    
                    // region = EmptyRegion(prototype);
                    if (region != null)
                    {
                        RegionHelper.TEMP_InitializeHardcodedRegionData(region);
                        EntityHelper.SetUpHardcodedEntities(region);
                        ulong entities = Game.EntityManager.PeekNextEntityId() - numEntities;
                        Logger.Info($"Entities generated = {entities} [{region.EntitySpatialPartition.TotalElements}]");
                        region.CreatedTime = DateTime.Now;

                        _regionDict.Add(prototype, region);
                    }
                }

                return region;
            }
        }

        public async Task CleanUpRegionsAsync(CancellationToken cancellationToken)
        {            
            while (true)
            {                
                // NOTE: When cancellation is requested, control doesn't return from Task.Delay(),
                // effectively breaking the loop.
                await Task.Delay(Game.CustomGameOptions.RegionCleanupIntervalMS, cancellationToken);

                // Run cleanup after the delay so that it doesn't happen as soon as we first run this task.
                CleanUpRegions(false);
            }
        }

        public void CleanUpRegions(bool forceCleanAll)
        {
            lock (_managerLock)
            {
                if (_allRegions.Count == 0) return;
            }            
            var currentTime = DateTime.Now;
            Logger.Info($"Running region cleanup...");

            // Get PlayerRegions
            HashSet<RegionPrototypeId> playerRegions = new();
            foreach (var playerConnection in Game.NetworkManager)
            {
                var regionRef = (RegionPrototypeId)playerConnection.RegionDataRef; // TODO use RegionID
                playerRegions.Add(regionRef); 
            }

            // Check all regions 
            List<Region> toShutdown = new();

            if (forceCleanAll)
            {
                // Add all regions if we are forcing a cleanup of all regions (e.g. when shutting the game down)
                toShutdown.AddRange(AllRegions);
            }
            else
            {
                lock (_managerLock)
                {
                    foreach (Region region in AllRegions)
                    {
                        DateTime visitedTime;
                        lock (region.Lock)
                        {
                            visitedTime = region.VisitedTime;
                        }

                        if (playerRegions.Contains(region.OLD_RegionPrototypeId)) // TODO RegionId
                        {
                            // TODO send force exit from region to Players
                        }
                        else
                        {
                            // TODO check all active local teleport to this Region
                            if (currentTime - visitedTime >= Game.CustomGameOptions.RegionUnvisitedThreshold)
                                toShutdown.Add(region);
                        }
                    }
                }
            }

            // Queue all inactive regions for shutdown
            foreach (Region region in toShutdown)
            {
                lock (_managerLock)
                {
                    _allRegions.Remove(region.Id);
                    _regionDict.Remove(region.OLD_RegionPrototypeId);
                    _shutdownQueue.Enqueue(region);
                }              
            }
        }

        #region Hardcoded

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

