using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.Maps;

namespace MHServerEmu.Games.Network
{
    public class AreaOfInterest
    {
        private const float UpdateDistance = 256f;
        private const float ViewExpansionDistance = 600.0f;
        private const float InvisibleExpansionDistance = 1200.0f;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private PlayerConnection _playerConnection;
        private Game _game;

        private readonly Dictionary<ulong, LoadStatus> _loadedEntities = new();
        private readonly Dictionary<uint, LoadStatus> _loadedCells = new();
        private readonly Dictionary<uint, LoadStatus> _loadedAreas = new();

        public Region Region { get; private set; }
        public int CellsInRegion { get; set; }
        public int LoadedCellCount { get; set; } = 0;
        public int LoadedEntitiesCount { get => _loadedEntities.Count; }
        public float AOIVolume { get => _AOIVolume; set => SetAOIVolume(value); }

        private ulong _currentFrame = 0;
        private Vector3 _lastUpdatePosition = new();

        private float _viewOffset = 600.0f;
        private float _AOIVolume = 3200.0f;

        private Aabb2 _cameraView;
        private Aabb2 _entitiesVolume;
        private Aabb2 _visibileVolume;
        private Aabb2 _invisibileVolume;
        private PrototypeId _lastCameraSetting;

        public AreaOfInterest(PlayerConnection playerConnection, int AOIVolume = 3200)
        {
            _playerConnection = playerConnection;
            _game = playerConnection.Game;

            SetAOIVolume(AOIVolume >= 1600 && AOIVolume <= 5000 ? AOIVolume : 3200);
        }

        public void InitPlayerView(PrototypeId cameraSettingPrototype)
        {
            _cameraView = new Aabb2(new Vector3(_viewOffset, _viewOffset, 0.0f), _AOIVolume);

            if (cameraSettingPrototype != 0)
            {
                CameraSettingCollectionPrototype cameraSettingCollectionPrototype = GameDatabase.GetPrototype<CameraSettingCollectionPrototype>(cameraSettingPrototype);
                if (cameraSettingCollectionPrototype == null)
                {
                    GlobalsPrototype globalsPrototype = GameDatabase.GlobalsPrototype;
                    if (globalsPrototype == null) return;
                    cameraSettingCollectionPrototype = GameDatabase.GetPrototype<CameraSettingCollectionPrototype>(globalsPrototype.PlayerCameraSettings);
                }

                if (cameraSettingCollectionPrototype.CameraSettings.IsNullOrEmpty()) return;
                _lastCameraSetting = cameraSettingPrototype;
                CameraSettingPrototype cameraSetting = cameraSettingCollectionPrototype.CameraSettings.First();

                var normalizedDirection = Vector3.Normalize2D(new(cameraSetting.DirectionX, cameraSetting.DirectionY, cameraSetting.DirectionZ));
                float angle = Orientation.WrapAngleRadians(Orientation.FromDeltaVector2D(normalizedDirection).Yaw + MathHelper.Pi - MathHelper.PiOver4);
                _cameraView = Transform3.RotationZ(angle) * _cameraView;
            }
        }

        public void Reset(Region region)
        {
            _loadedAreas.Clear();
            _loadedCells.Clear();
            _loadedEntities.Clear();

            _currentFrame = 0;
            CellsInRegion = 0;
            Region = region;
            _lastCameraSetting = 0;
        }

        public void Update(Vector3 position, bool forceUpdate = false, bool isStart = false)
        {
            _currentFrame++;

            if (forceUpdate == false && Vector3.Distance2D(_lastUpdatePosition, position) < UpdateDistance)
                return;

            CalcVolumes(position);

            if (isStart)
            {
                Area startArea = Region.GetAreaAtPosition(position);
                AddArea(startArea, true);
            }
            else
            {
                UpdateEntities();
            }               

            // update Areas
            UpdateAreas();

            // update Cells
            if (UpdateCells())
            {
                SendMessage(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build());

                // Mini map
                using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.DefaultPolicy))
                {
                    LowResMap lowResMap = new(Region.RegionPrototype.AlwaysRevealFullMap);
                    Serializer.Transfer(archive, ref lowResMap);

                    SendMessage(NetMessageUpdateMiniMap.CreateBuilder()
                        .SetArchiveData(archive.ToByteString())
                        .Build());
                }
            }

            _lastUpdatePosition.Set(position);
        }

        public static bool GetEntityInterest(WorldEntity worldEntity)
        {
            // TODO write all Player interests for entity
            if (worldEntity.TrackAfterDiscovery) return true;
            if (worldEntity.IsAlive() == false) return true;
            return false;
        }

        public void OnCellLoaded(uint cellId)
        {
            LoadedCellCount++;
            if (_loadedCells.TryGetValue(cellId, out var cell)) cell.Loaded = true;
        }

        public bool CheckTargetCell(Transition target)
        {
            if (_loadedCells.TryGetValue(target.RegionLocation.Cell.Id, out var cell))
                return cell.Loaded == false;

            return true;
        }

        public void ForceCellLoad()
        {
            foreach (var cell in _loadedCells)
                cell.Value.Loaded = true;
        }

        public bool IsEntityLoaded(ulong entityId)
        {
            return _loadedEntities.ContainsKey(entityId);
        }

        private void SendMessage(IMessage message)
        {
            _playerConnection.SendMessage(message);
        }

        private void CalcVolumes(Vector3 playerPosition)
        {
            _entitiesVolume = _cameraView.Translate(playerPosition);
            _visibileVolume = _entitiesVolume.Expand(ViewExpansionDistance);
            _invisibileVolume = _entitiesVolume.Expand(InvisibleExpansionDistance);
        }

        private void UpdateAreas()
        {
            Region region = Region;

            foreach (var area in region.IterateAreas())
            {
                if (_loadedAreas.ContainsKey(area.Id))
                {
                    if (area.RegionBounds.Intersects(_invisibileVolume) == false)
                        RemoveArea(area);
                }
                else
                {
                    if (area.RegionBounds.Intersects(_visibileVolume))
                        AddArea(area, false);
                }
            }
        }

        private void AddArea(Area area, bool isStartArea)
        {
            _loadedAreas.Add(area.Id, new(_currentFrame, true, true));
            SendMessage(area.MessageAddArea(isStartArea));
        }

        private void RemoveArea(Area area)
        {
            SendMessage(NetMessageRemoveArea.CreateBuilder()
                .SetAreaId(area.Id)
                .Build());
            _loadedAreas.Remove(area.Id);
        }

        private void AddCell(Cell cell)
        {
            SendMessage(cell.MessageCellCreate());
            _loadedCells.Add(cell.Id, new(_currentFrame, false, false));
        }

        private void RemoveCell(Cell cell)
        {
            var areaId = cell.Area.Id;
            if (_loadedAreas.ContainsKey(areaId))
            {
                SendMessage(NetMessageCellDestroy.CreateBuilder()
                    .SetAreaId(areaId)
                    .SetCellId(cell.Id)
                    .Build());
            }
            _loadedCells.Remove(cell.Id);
            LoadedCellCount--;
        }

        private bool UpdateCells()
        {
            Region region = Region;

            RegionManager manager = _game.RegionManager;
            Stack<Cell> invisibleCells = new();
            bool environmentUpdate = false;

            // search invisible cells
            foreach (var cellStatus in _loadedCells)
            {
                Cell cell = manager.GetCell(cellStatus.Key);
                if (cell == null) continue;
                if (cell.RegionBounds.Intersects(_invisibileVolume) == false)
                    invisibleCells.Push(cell);
            }

            // Remove invisible cells
            while (invisibleCells.Count != 0)
            {
                Cell cell = invisibleCells.Pop();
                RemoveCell(cell);
                // EnvironmentUpdate
                environmentUpdate |= cell.Area.IsDynamicArea() == false;
            }

            // Add new cells
            foreach (Cell cell in region.IterateCellsInVolume(_visibileVolume))
            {
                if (_loadedAreas.ContainsKey(cell.Area.Id) == false) continue;
                if (_loadedCells.ContainsKey(cell.Id)) continue;

                if (cell.RegionBounds.Intersects(_visibileVolume))
                {
                    AddCell(cell);
                    // EnvironmentUpdate
                    environmentUpdate |= cell.Area.IsDynamicArea() == false;
                }
            }

            CellsInRegion = _loadedCells.Count;
            return environmentUpdate;
        }

        private void UpdateEntities()
        {
            Region region = Region;
            List<WorldEntity> newEntities = new();

            // Update Entity
            foreach (var worldEntity in region.IterateEntitiesInVolume(_entitiesVolume, new()))
            {
                if (worldEntity.RegionLocation.Cell == null)
                {
                    Logger.Warn($"UpdateEntity(): worldEntity.RegionLocation.Cell == null, entity: {worldEntity}");
                    continue;
                }

                if (_loadedCells.TryGetValue(worldEntity.RegionLocation.Cell.Id, out var status))
                    if (status.Loaded == false) continue;

                bool interest = GetEntityInterest(worldEntity);
                if (_loadedEntities.TryGetValue(worldEntity.Id, out var entityStatus))
                {
                    entityStatus.Frame = _currentFrame;
                    entityStatus.InterestToPlayer = interest;
                }
                else
                {
                    _loadedEntities.Add(worldEntity.Id, new(_currentFrame, true, interest));
                    if (worldEntity.IsAlive())
                        newEntities.Add(worldEntity);
                    // Logger.Debug($"{GameDatabase.GetFormattedPrototypeName(worldEntity.BaseData.PrototypeId)} = {worldEntity.BaseData.PrototypeId},");
                }
            }

            // Add new Entity       // TODO AddEntity 
            if (newEntities.Count > 0)
            {
                foreach (Entity entity in newEntities)
                    SendMessage(entity.ToNetMessageEntityCreate());
            }    

            // Delete entities
            foreach (var kvp in _loadedEntities.Where(kvp => kvp.Value.Frame < _currentFrame && kvp.Value.InterestToPlayer == false))
            {
                // TODO RemoveEntity
                _loadedEntities.Remove(kvp.Key);
                SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(kvp.Key).Build());
            }                         
        }

        private void SetAOIVolume(float volume)
        {
            _AOIVolume = volume;
            _viewOffset = _AOIVolume / 8; // 3200 / 8 = 400
            InitPlayerView(_lastCameraSetting);
        }

        private class LoadStatus
        {
            public ulong Frame { get; set; }
            public bool Loaded { get; set; }
            public bool InterestToPlayer { get; set; }

            public LoadStatus(ulong frame, bool loaded, bool interestToPlayer)
            {
                Frame = frame;
                Loaded = loaded;
                InterestToPlayer = interestToPlayer;
            }
        }
    }
}
