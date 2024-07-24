using System.Collections.Concurrent;
using System.Diagnostics;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions
{
    public class RegionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly IdGenerator _idGenerator = new(IdType.Region, 0);

        private readonly ConcurrentQueue<Region> _shutdownQueue = new();
        private readonly object _managerLock = new();

        private readonly Dictionary<uint, Cell> _allCells = new();
        private readonly Dictionary<ulong, Region> _allRegions = new();
        private readonly Dictionary<ulong, Region> _matches = new();

        private readonly Dictionary<PrototypeId, Region> _regionByRefDict = new();    // TODO: multiple instances of the same region

        private uint _areaId = 1;
        private uint _cellId = 1;

        public Game Game { get; private set; }
        public IEnumerable<Region> AllRegions { get => _allRegions.Values; }

        public RegionManager()
        {
        }

        public bool Initialize(Game game)
        {
            Game = game;
            return true;
        }

        public uint AllocateCellId()
        {
            return _cellId++;
        }

        public uint AllocateAreaId()
        {
            return _areaId++;
        }

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

        public Region GenerateRegion(PrototypeId regionProtoRef) 
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
                RegionDataRef = regionProtoRef
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
                Logger.Error($"GenerateRegion failed after {10 - tries} attempts | regionId: {regionProtoRef.GetNameFormatted()} | Last Seed: {settings.Seed}");

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
        public Region GetRegionByRef(PrototypeId regionProtoRef)
        {
            if (DataDirectory.Instance.PrototypeIsA<RegionPrototype>(regionProtoRef) == false)
                return Logger.WarnReturn<Region>(null, $"GetRegion(): {regionProtoRef} is not a valid region prototype ref");

            //prototype = RegionPrototypeId.NPEAvengersTowerHUBRegion;
            lock (_managerLock)
            {
                if (_regionByRefDict.TryGetValue(regionProtoRef, out Region region) == false)
                {
                    // Generate the region and create entities for it if needed
                    ulong numEntities = Game.EntityManager.PeekNextEntityId();
                    Logger.Info($"Generating region {((PrototypeId)regionProtoRef).GetNameFormatted()}...");

                    try
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        region = GenerateRegion(regionProtoRef);
                        Logger.Info($"Generated region {regionProtoRef} in {stopwatch.ElapsedMilliseconds} ms");
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

                        _regionByRefDict.Add(regionProtoRef, region);
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
            HashSet<PrototypeId> regionsWithPlayers = new();
            foreach (var playerConnection in Game.NetworkManager)
                regionsWithPlayers.Add(playerConnection.RegionDataRef); // TODO use RegionID

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

                        if (regionsWithPlayers.Contains(region.PrototypeDataRef)) // TODO RegionId
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
                    _regionByRefDict.Remove(region.PrototypeDataRef);
                    _shutdownQueue.Enqueue(region);
                }              
            }
        }
    }
}

