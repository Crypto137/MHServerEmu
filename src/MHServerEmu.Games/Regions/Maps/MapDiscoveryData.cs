using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Regions.Maps
{
    /// <summary>
    /// Keeps track of minimap sections and entities discovered by a player in a specific region instance.
    /// </summary>
    public class MapDiscoveryData
    {
        // TODO: Serialize on region transfer so that we don't lose minimap / entity discovery data

        private readonly HashSet<ulong> _discoveredEntities = new();

        public ulong RegionId { get; }
        public LowResMap LowResMap { get; }

        public MapDiscoveryData(Region region)
        {
            RegionId = region.Id;
            LowResMap = new(region.Prototype.AlwaysRevealFullMap);
        }

        public bool DiscoverEntity(WorldEntity worldEntity)
        {
            return _discoveredEntities.Add(worldEntity.Id);
        }

        public bool UndiscoverEntity(WorldEntity worldEntity)
        {
            return _discoveredEntities.Remove(worldEntity.Id);
        }

        public bool IsEntityDiscovered(WorldEntity worldEntity)
        {
            return _discoveredEntities.Contains(worldEntity.Id);
        }
    }
}
