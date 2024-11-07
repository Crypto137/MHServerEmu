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
        // TODO: Serialize on region transfer so that we don't lose minimap / entity discovery data

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<ulong> _discoveredEntities = new();

        public Player Player { get; }
        public ulong RegionId { get; }
        public LowResMap LowResMap { get; }

        public MapDiscoveryData(Region region, Player player)
        {
            Player = player;
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

        public bool LoadDiscovered()
        {
            var manager = Player?.Game.EntityManager;
            if (manager == null) return Logger.WarnReturn(false, $"Update(): EntityManager == null");

            var aoi = Player.AOI;
            foreach (var entityId in _discoveredEntities)
            {
                var entity = manager.GetEntity<WorldEntity>(entityId);
                if (entity != null) aoi.ConsiderEntity(entity);
            }

            return LowResMapUpdate();
        }

        public bool LowResMapUpdate(Vector3? position = default)
        {
            if (Player == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): Player == null");
            var aoi = Player.AOI;

            var regionManager = Player.Game.RegionManager;
            if (regionManager == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): regionManager == null");

            var region = regionManager.GetRegion(RegionId);
            if (region == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): region == null");

            HashSet<Area> areas = new();
            HashSet<Cell> cells = new();
            bool regenNavi = false;
            bool update = false;

            if (position.HasValue)
            {
                var volume = region.GetLowResVolume(position.Value);
                aoi.AddCellsFromVolume(volume, areas, cells, ref regenNavi);
            }
            else
            {
                var map = LowResMap.Map;
                Vector3 posAtIndex = Vector3.Zero;
                bool isRevealAll = LowResMap.IsRevealAll;
                int size = Math.Min(map.Size, region.GetLowResVectorSize());

                for (int index = 0; index < size; index++)
                    if (isRevealAll || map[index])
                    {
                        if (LowResMap.Translate(index, ref posAtIndex) == false) continue;
                        var volume = region.GetLowResVolume(posAtIndex);
                        aoi.AddCellsFromVolume(volume, areas, cells, ref regenNavi);
                    }

                update = true;
            }

            if (regenNavi) aoi.RegenerateClientNavi();
            if (update) SendMiniMapUpdate();

            if (aoi.RemoveCells(areas, cells)) 
                aoi.RegenerateClientNavi();

            return true;
        }

        private void SendMiniMapUpdate()
        {
            Logger.Trace($"SendMiniMapUpdate(): {Player}");
            Player.SendMessage(ArchiveMessageBuilder.BuildUpdateMiniMapMessage(LowResMap));
        }

        public bool RevealPosition(Vector3 position)
        {
            return LowResMap.RevealPosition(position) && LowResMapUpdate(position);
        }
    }
}
