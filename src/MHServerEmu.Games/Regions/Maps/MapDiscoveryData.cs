using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Regions.Maps
{
    /// <summary>
    /// Keeps track of minimap sections and entities discovered by a player in a specific region instance.
    /// </summary>
    public class MapDiscoveryData
    {
        // TODO: Serialize on region transfer so that we don't lose minimap / entity discovery data

        private static readonly Logger Logger = LogManager.CreateLogger();

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
            if (worldEntity.IsDiscoverable == false)
                return Logger.WarnReturn(false, $"DiscoverEntity(): Entity {worldEntity} is not discoverable");

            return _discoveredEntities.Add(worldEntity.Id);
        }

        public bool UndiscoverEntity(WorldEntity worldEntity)
        {
            if (worldEntity.IsDiscoverable == false)
                return Logger.WarnReturn(false, $"UndiscoverEntity(): Entity {worldEntity} is not discoverable");

            return _discoveredEntities.Remove(worldEntity.Id);
        }

        public bool IsEntityDiscovered(WorldEntity worldEntity)
        {
            return _discoveredEntities.Contains(worldEntity.Id);
        }
    }
}
