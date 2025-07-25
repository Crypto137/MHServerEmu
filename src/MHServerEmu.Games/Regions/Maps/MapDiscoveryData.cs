using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions.Maps
{
    /// <summary>
    /// Keeps track of minimap sections and entities discovered by a player in a specific region instance.
    /// </summary>
    public class MapDiscoveryData : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ulong _regionId;
        private PrototypeId _regionProtoRef;    // Needed to reset discovery for region instances in other games.
        private TimeSpan _accessTimestamp;
        private HashSet<ulong> _discoveredEntities = new();

        public LowResMap LowResMap { get; } = new();

        public ulong RegionId { get => _regionId; }
        public PrototypeId RegionProtoRef { get => _regionProtoRef; }
        public TimeSpan AccessTimestamp { get => _accessTimestamp; }

        public MapDiscoveryData() { }

        public MapDiscoveryData(ulong regionId)
        {
            _regionId = regionId;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _regionId);

            if (archive.Version >= ArchiveVersion.AddedRegionProtoRefToMapDiscoveryData)
                success &= Serializer.Transfer(archive, ref _regionProtoRef);

            success &= Serializer.Transfer(archive, ref _accessTimestamp);
            success &= Serializer.Transfer(archive, ref _discoveredEntities);
            success &= Serializer.Transfer(archive, LowResMap);

            return success;
        }

        public void InitIfNecessary(Region region)
        {
            if (LowResMap.InitIfNecessary(region) == false)
                return;

            RegionPrototype regionProto = region.Prototype;
            _regionProtoRef = regionProto.DataRef;

            if (regionProto.AlwaysRevealFullMap)
                LowResMap.RevealAll();
        }

        public void UpdateAccessTimestamp()
        {
            _accessTimestamp = Game.Current.CurrentTime;
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

        public bool LowResMapUpdate(Player player, Vector3? position = null)
        {
            var aoi = player.AOI;
            if (aoi == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): AOI == null");

            var regionManager = player.Game.RegionManager;
            if (regionManager == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): regionManager == null");

            var region = regionManager.GetRegion(RegionId);
            if (region == null) return Logger.WarnReturn(false, $"LowResMapUpdate(): region == null");

            bool regenNavi = false;
            bool update = false;

            HashSet<Area> areas = HashSetPool<Area>.Instance.Get();
            HashSet<Cell> cells = HashSetPool<Cell>.Instance.Get();

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
                int size = Math.Min(map.Size, region.LowResVectorSize);

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
            if (update) SendMiniMapUpdate(player);

            if (aoi.RemoveCells(areas, cells)) 
                aoi.RegenerateClientNavi();

            HashSetPool<Area>.Instance.Return(areas);
            HashSetPool<Cell>.Instance.Return(cells);

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
