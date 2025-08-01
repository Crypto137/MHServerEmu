using System.Diagnostics;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
    public class RegionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<uint, Cell> _allCells = new();
        private readonly Dictionary<ulong, Region> _allRegions = new();
        private readonly Dictionary<ulong, Region> _matches = new();

        private uint _areaId = 1;
        private uint _cellId = 1;

        public Game Game { get; private set; }

        public RegionManager()
        {
        }

        public bool Initialize(Game game)
        {
            Game = game;
            return true;
        }

        public Dictionary<ulong, Region>.ValueCollection.Enumerator GetEnumerator()
        {
            return _allRegions.Values.GetEnumerator();
        }

        public Region GetRegion(ulong id)
        {
            if (id == 0)
                return null;

            if (_allRegions.TryGetValue(id, out Region region) == false)
                return null;

            return region;
        }

        public Region GenerateRegion(ulong regionId, PrototypeId regionProtoRef, NetStructCreateRegionParams createParams)
        {
            const int NumTries = 10;

            RegionSettings settings = new(createParams)
            {
                InstanceAddress = regionId,
                RegionDataRef = regionProtoRef,
                GenerateAreas = true,
                GenerateEntities = true,
                GenerateLog = false
            };

            if (settings.Seed == 0)
                settings.Seed = Game.Random.Next();

            Logger.Info($"Generating region {regionProtoRef.GetName()}...");

            Region region = null;
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < NumTries && region == null; i++)
            {
                // Get a new seed for retries
                if (i > 0)
                    settings.Seed = Game.Random.Next();

                region = CreateRegion(settings);
            }

            stopwatch.Stop();

            if (region != null)
                Logger.Info($"Generated region {regionProtoRef.GetNameFormatted()} in {stopwatch.Elapsed.TotalMilliseconds} ms");
            else
                Logger.Error($"GenerateRegion failed after {NumTries} attempts | regionId: {regionProtoRef.GetNameFormatted()} | Last Seed: {settings.Seed}");

            return region;
        }

        public Region CreateRegion(RegionSettings settings)
        {
            if (settings.RegionDataRef == 0) return Logger.WarnReturn<Region>(null, "CreateRegion(): settings.RegionDataRef == 0");

            ulong instanceAddress = settings.InstanceAddress;
            if (instanceAddress == 0) return Logger.WarnReturn<Region>(null, "CreateRegion(): instanceAddress == 0");
            if (GetRegion(instanceAddress) != null) return Logger.WarnReturn<Region>(null, "CreateRegion(): GetRegion(instanceAddress) != null");

            // Game::AllocateRegion()
            Region region = new(Game);
            _allRegions[instanceAddress] = region;

            // No need to create a copy like the client because this is not a const&

            if (region.Initialize(settings) == false)
            {
                _allRegions.Remove(instanceAddress);
                region.Shutdown(false);
                return null;
            }

            if (region.MatchNumber != 0)    // Matches are not stored in the public region dict
                _matches[region.MatchNumber] = region;

            if (region.Id != instanceAddress)
                Logger.Warn("CreateRegion(): region.Id != instanceAddress");

            return region;
        }

        public bool DestroyRegion(ulong regionId)
        {
            // NOTE: We merged Region::DestroyRegion() with Region::destroyRegionFromIterator() from the client
            // because we don't use C++ style iterators here.

            if (_allRegions.TryGetValue(regionId, out Region region) == false)
                return Logger.WarnReturn(false, $"DestroyRegion(): Failed to retrieve region for id 0x{regionId:X}");

            if (region.MatchNumber != 0)
                _matches.Remove(region.MatchNumber);

            region.Shutdown(true);

            _allRegions.Remove(regionId);

            return true;
        }

        public void DestroyAllRegions()
        {
            foreach (var kvp in _allRegions)
                DestroyRegion(kvp.Key);
        }

        public uint AllocateAreaId()
        {
            return _areaId++;
        }

        public uint AllocateCellId()
        {
            return _cellId++;
        }

        public Cell GetCell(uint cellId)
        {
            if (_allCells.TryGetValue(cellId, out var cell)) return cell;
            return null;
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
    }
}

