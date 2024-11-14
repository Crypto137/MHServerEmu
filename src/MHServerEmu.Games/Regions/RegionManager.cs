using System.Diagnostics;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions
{
    public class RegionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly IdGenerator _idGenerator = new(IdType.Region, 0);

        private readonly Dictionary<uint, Cell> _allCells = new();
        private readonly Dictionary<ulong, Region> _allRegions = new();
        private readonly Dictionary<ulong, Region> _matches = new();

        private readonly Dictionary<PrototypeId, Region> _publicRegionDict = new();

        private uint _areaId = 1;
        private uint _cellId = 1;

        private TimeSpan _lastCleanupTime;
        private readonly Stack<ulong> _regionsToDestroy = new();
        private readonly HashSet<ulong> _regionsPendingShutdown = new();

        public Game Game { get; private set; }

        public RegionManager()
        {
        }

        public bool Initialize(Game game)
        {
            Game = game;
            _lastCleanupTime = Clock.UnixTime;
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

        public bool DestroyRegion(ulong regionId)
        {
            // NOTE: This is dangerous to call from outside the RegionManager.
            // Use RegionManager.RequestRegionShutdown() or Region.RequestShutdown() when possible.

            // NOTE: We merged Region::DestroyRegion() with Region::destroyRegionFromIterator() from the client
            // because we don't use C++ style iterators here.

            if (_allRegions.TryGetValue(regionId, out Region region) == false)
                return Logger.WarnReturn(false, $"DestroyRegion(): Failed to retrieve region for id 0x{regionId:X}");

            if (region.MatchNumber != 0)
                _matches.Remove(regionId);

            if (region.IsPublic)
                _publicRegionDict.Remove(region.PrototypeDataRef);

            TimeSpan lifetime = Clock.UnixTime - region.CreatedTime;
            Logger.Info($"Shutdown: Region = {region}, Lifetime = {(int)lifetime.TotalMinutes} min {lifetime:ss} sec");
            region.Shutdown();

            _allRegions.Remove(regionId);
            _regionsPendingShutdown.Remove(regionId);

            return true;
        }

        public bool RequestRegionShutdown(ulong regionId)
        {
            return _regionsPendingShutdown.Add(regionId);
        }

        public void DestroyAllRegions()
        {
            foreach (var kvp in _allRegions)
                DestroyRegion(kvp.Key);
        }

        public Region GetRegion(ulong id)
        {
            if (id == 0) return null;

            if (_allRegions.TryGetValue(id, out Region region))
                return region;

            return null;
        }

        public Region GetOrGenerateRegionForPlayer(RegionContext regionContext, PlayerConnection playerConnection)
        {
            PrototypeId regionProtoRef = regionContext.RegionDataRef;

            RegionPrototype regionProto = GameDatabase.GetPrototype<RegionPrototype>(regionProtoRef);
            if (regionProto == null)
                return Logger.WarnReturn<Region>(null, $"GetRegion(): {regionProtoRef} is not a valid region prototype ref");

            if (regionProto.HasEndless() && regionContext.EndlessLevel == 0)
                return Logger.WarnReturn<Region>(null, $"GetRegion(): DangerRoom {regionProtoRef} with EndlessLevel = 0");

            //prototype = RegionPrototypeId.NPEAvengersTowerHUBRegion;

            Region region = null;

            if (regionProto.IsPublic)
            {
                // Currently we have only one instance of each public region, and they all use the lowest available difficulty.
                regionContext.DifficultyTierRef = regionProto.AccessDifficulties.HasValue()
                    ? regionProto.AccessDifficulties[0]
                    : GameDatabase.GlobalsPrototype.DifficultyTierDefault;

                if (_publicRegionDict.TryGetValue(regionProtoRef, out region) == false)
                    region = GenerateAndInitRegion(regionContext);
            }
            else
            {
                // There can be multiple instances of private regions, one for each world view.
                // Currently each player connection has a world view, and in the future they
                // will also be sharable for party members by party leaders.
                ulong regionId = playerConnection.WorldView.GetRegionInstanceId(regionProtoRef);

                // Use preferred difficulty for private instances
                regionContext.DifficultyTierRef = playerConnection.Player.GetDifficultyTierPreference();

                if (regionId == 0 
                    || _allRegions.TryGetValue(regionId, out region) == false 
                    || region.DifficultyTierRef != regionContext.DifficultyTierRef 
                    || region.Settings.EndlessLevel != regionContext.EndlessLevel) // Danger Room next level
                {
                    // MetaStateShutdown will shutdown old region
                    if (region != null && region.Settings.EndlessLevel == regionContext.EndlessLevel)
                    {
                        // Destroy existing private instance if it does not match the player's difficulty preference 
                        playerConnection.WorldView.RemoveRegion(region.PrototypeDataRef);
                        DestroyRegion(regionId);
                    }

                    region = GenerateAndInitRegion(regionContext);
                    playerConnection.WorldView.AddRegion(regionProtoRef, region.Id);
                }
            }

            return region;
        }

        public void Update()
        {
            TimeSpan now = Clock.UnixTime;
            TimeSpan timeSinceLastCleanup = now - _lastCleanupTime;

            if (_regionsPendingShutdown.Count == 0)
            {
                if (timeSinceLastCleanup < Game.CustomGameOptions.RegionCleanupInterval)
                    return;

                Logger.Trace($"Running region cleanup...");
            }
            else
            {
                // Do more frequent silent cleanups when we have pending shutdowns.
                if (timeSinceLastCleanup < TimeSpan.FromMilliseconds(1000))
                    return;
            }

            _lastCleanupTime = now;

            foreach (Region region in _allRegions.Values)
            {
                // TODO: force remove players from the region?
                if (region.PlayerCount > 0)
                    continue;

                // TODO: check world views of players this region is relevant to
                if (_regionsPendingShutdown.Contains(region.Id) || now - region.LastVisitedTime >= Game.CustomGameOptions.RegionUnvisitedThreshold)
                    _regionsToDestroy.Push(region.Id);
            }

            while (_regionsToDestroy.Count > 0)
            {
                ulong regionId = _regionsToDestroy.Pop();
                DestroyRegion(regionId);
            }
        }

        public Dictionary<ulong, Region>.ValueCollection.Enumerator GetEnumerator()
        {
            return _allRegions.Values.GetEnumerator();
        }

        private Region GenerateAndInitRegion(RegionContext regionContext)
        {
            PrototypeId regionProtoRef = regionContext.RegionDataRef;

            Region region = null;

            // Generate the region and create entities for it if needed
            ulong numEntities = Game.EntityManager.PeekNextEntityId();
            Logger.Info($"Generating region {regionProtoRef.GetNameFormatted()}...");

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                if (regionContext.Seed == 0) regionContext.Seed = Game.Random.Next();

                RegionSettings settings = new(regionContext)
                {
                    InstanceAddress = _idGenerator.Generate(),
                    GenerateAreas = true,
                    GenerateEntities = true,
                    GenerateLog = false
                };

                // clear Endless context
                regionContext.Reset();

                int tries = 10;

                while (region == null && (--tries > 0))
                {
                    if (tries < 9) settings.Seed = Game.Random.Next(); // random.Next(); 
                    region = CreateRegion(settings);
                }

                if (region == null)
                    Logger.Error($"GenerateRegion failed after {10 - tries} attempts | regionId: {regionProtoRef.GetNameFormatted()} | Last Seed: {settings.Seed}");

                Logger.Info($"Generated region {regionProtoRef.GetNameFormatted()} in {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, "Generation failed");
            }

            // region = EmptyRegion(prototype);
            if (region != null)
            {
                // Off Region Mission info // RegionHelper.TEMP_InitializeHardcodedRegionData(region);
                // EntityHelper.SetUpHardcodedEntities(region);
                ulong entities = Game.EntityManager.PeekNextEntityId() - numEntities;
                Logger.Info($"Entities generated = {entities} [{region.EntitySpatialPartition.TotalElements}]");

                if (region.IsPublic)
                    _publicRegionDict.Add(regionProtoRef, region);
            }

            return region;
        }
    }
}

