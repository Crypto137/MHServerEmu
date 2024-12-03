using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions.Maps
{
    /// <summary>
    /// Keeps track of minimap sections and entities discovered by a player in a specific region instance.
    /// </summary>
    public class MapDiscoveryData
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<ulong> _discoveredEntities = new();
        private readonly HashSet<Area> _areas = new();
        private readonly HashSet<Cell> _cells = new();

        public ulong RegionId { get; }
        public LowResMap LowResMap { get; }

        public MapDiscoveryData(Region region)
        {
            RegionId = region.Id;
            LowResMap = new(region); // InitIfNecessary
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

        public bool LoadPlayerDiscovered(Player player)
        {
            var manager = player.Game.EntityManager;
            if (manager == null) return Logger.WarnReturn(false, $"Update(): EntityManager == null");

            var aoi = player.AOI;
            foreach (var entityId in _discoveredEntities)
            {
                var entity = manager.GetEntity<WorldEntity>(entityId);
                if (entity != null) aoi.ConsiderEntity(entity);
            }

            return LowResMapUpdate(player);
        }

        public bool LowResMapUpdate(Player player, Vector3? position = default)
        {
            var aoi = player.AOI;
            if (aoi == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): AOI == null");

            var regionManager = player.Game.RegionManager;
            if (regionManager == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): regionManager == null");

            var region = regionManager.GetRegion(RegionId);
            if (region == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): region == null");

            bool regenNavi = false;
            bool update = false;

            if (position.HasValue)
            {
                var volume = region.GetLowResVolume(position.Value);
                aoi.AddCellsFromVolume(volume, _areas, _cells, ref regenNavi);
            }
            else
            {
                var map = LowResMap.Map;
                Vector3 posAtIndex = Vector3.Zero;
                bool isRevealAll = LowResMap.IsRevealAll;
                int size = Math.Min(map.Size, region.LowResVectorSize);

                for (int index = 0; index < size; index++)
                    if (isRevealAll || map[index])
                    {
                        if (LowResMap.Translate(index, ref posAtIndex) == false) continue;
                        var volume = region.GetLowResVolume(posAtIndex);
                        aoi.AddCellsFromVolume(volume, _areas, _cells, ref regenNavi);
                    }

                update = true;
            }

            if (regenNavi) aoi.RegenerateClientNavi();
            if (update) SendMiniMapUpdate(player);

            if (aoi.RemoveCells(_areas, _cells)) 
                aoi.RegenerateClientNavi();

            _areas.Clear();
            _cells.Clear();

            return true;
        }

        private void SendMiniMapUpdate(Player player)
        {
            player.SendMessage(ArchiveMessageBuilder.BuildUpdateMiniMapMessage(LowResMap));
        }

        public bool RevealPosition(Player player, Vector3 position)
        {
            return LowResMap.RevealPosition(position) && LowResMapUpdate(player, position);
        }
    }
}
