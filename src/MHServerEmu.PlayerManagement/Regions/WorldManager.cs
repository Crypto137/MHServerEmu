using MHServerEmu.Core.System;

namespace MHServerEmu.PlayerManagement.Regions
{
    /// <summary>
    /// Global manager for all regions across all games.
    /// </summary>
    public class WorldManager
    {
        private readonly IdGenerator _idGenerator = new(IdType.Region, 0);

        private readonly Dictionary<ulong, RegionHandle> _allRegions = new();

        private readonly PlayerManagerService _playerManager;

        public ulong NextRegionId { get => _idGenerator.Generate(); }

        public WorldManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public bool AddRegion(RegionHandle region)
        {
            _allRegions.Add(region.Id, region);
            return true;
        }

        public bool RemoveRegion(RegionHandle region)
        {
            return _allRegions.Remove(region.Id);
        }
    }
}
