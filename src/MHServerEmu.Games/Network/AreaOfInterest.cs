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
        private const float UpdateDistanceSquared = 256f * 256f;
        private const float ViewExpansionDistance = 600.0f;
        private const float InvisibleExpansionDistance = 1200.0f;

        private const int AOIVolumeDefault = 3200;
        private const int AOIVolumeMin = 1600;
        private const int AOIVolumeMax = 5000;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<uint, InterestStatus> _trackedAreas = new();
        private readonly Dictionary<uint, InterestStatus> _trackedCells = new();
        private readonly Dictionary<ulong, InterestStatus> _trackedEntities = new();

        private PlayerConnection _playerConnection;
        private Game _game;

        private ulong _currentFrame = 0;
        private Vector3 _lastUpdatePosition = new();

        private float _viewOffset = 600.0f;
        private float _aoiVolume = AOIVolumeDefault;

        private Aabb2 _cameraView;
        private Aabb2 _entitiesVolume;
        private Aabb2 _visibileVolume;
        private Aabb2 _invisibileVolume;
        private PrototypeId _lastCameraSetting;

        public Region Region { get; private set; }
        public int CellsInRegion { get; set; }
        public int LoadedCellCount { get; set; } = 0;
        public int TrackedEntitiesCount { get => _trackedEntities.Count; }
        public float AOIVolume { get => _aoiVolume; set => SetAOIVolume(value); }

        public AreaOfInterest(PlayerConnection playerConnection, int aoiVolume = AOIVolumeDefault)
        {
            _playerConnection = playerConnection;
            _game = playerConnection.Game;

            if (aoiVolume.IsWithin(AOIVolumeMin, AOIVolumeMax) == false)
            {
                Logger.Warn($"AreaOfInterest(): aoiVolume {aoiVolume} is outside the expected range of {AOIVolumeMin} to {AOIVolumeMax}, defaulting to {AOIVolumeDefault}");
                aoiVolume = AOIVolumeDefault;
            }

            SetAOIVolume(aoiVolume);
        }

        public void InitializePlayerView(PrototypeId cameraSettingPrototype)
        {
            _cameraView = new Aabb2(new Vector3(_viewOffset, _viewOffset, 0.0f), _aoiVolume);

            if (cameraSettingPrototype == PrototypeId.Invalid) return;

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

        public void Update(Vector3 position, bool forceUpdate = false, bool isStart = false)
        {
            _currentFrame++;

            // Unless forceUpdate is set, we update only when we move far enough from the last update position.
            // NOTE: We use DistanceSquared2D() instead of Distance2D() to avoid calculating the square root of distance and speed this check up.
            if (forceUpdate == false && Vector3.DistanceSquared2D(_lastUpdatePosition, position) < UpdateDistanceSquared)
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

            UpdateAreas();

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

        public void Reset(Region region)
        {
            _trackedAreas.Clear();
            _trackedCells.Clear();
            _trackedEntities.Clear();

            _currentFrame = 0;
            CellsInRegion = 0;
            Region = region;
            _lastCameraSetting = 0;
        }

        public bool InterestedInArea(uint areaId)
        {
            return _trackedAreas.ContainsKey(areaId);
        }

        public bool InterestedInCell(uint cellId)
        {
            return _trackedCells.ContainsKey(cellId);
        }

        public bool InterestedInEntity(ulong entityId)
        {
            return _trackedEntities.ContainsKey(entityId);
        }

        public void OnCellLoaded(uint cellId)
        {
            LoadedCellCount++;
            if (_trackedCells.TryGetValue(cellId, out var cell))
                cell.Loaded = true;
        }

        public bool IsTargetCellLoaded(Transition target)
        {
            if (_trackedCells.TryGetValue(target.RegionLocation.Cell.Id, out InterestStatus cellInterest))
                return cellInterest.Loaded;

            return false;
        }

        public void ForceCellLoad()
        {
            foreach (var cell in _trackedCells)
                cell.Value.Loaded = true;
        }

        public static bool GetEntityInterest(WorldEntity worldEntity)
        {
            // TODO write all Player interests for entity
            if (worldEntity.TrackAfterDiscovery) return true;
            if (worldEntity.IsAlive() == false) return true;
            return false;
        }

        private void UpdateAreas()
        {
            Region region = Region;

            foreach (var area in region.IterateAreas())
            {
                if (_trackedAreas.ContainsKey(area.Id))
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

        private bool UpdateCells()
        {
            Region region = Region;

            RegionManager manager = _game.RegionManager;
            Stack<Cell> invisibleCells = new();
            bool environmentUpdate = false;

            // search invisible cells
            foreach (var cellStatus in _trackedCells)
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
                if (_trackedAreas.ContainsKey(cell.Area.Id) == false) continue;
                if (_trackedCells.ContainsKey(cell.Id)) continue;

                if (cell.RegionBounds.Intersects(_visibileVolume))
                {
                    AddCell(cell);
                    // EnvironmentUpdate
                    environmentUpdate |= cell.Area.IsDynamicArea() == false;
                }
            }

            CellsInRegion = _trackedCells.Count;
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

                if (_trackedCells.TryGetValue(worldEntity.RegionLocation.Cell.Id, out var status))
                    if (status.Loaded == false) continue;

                bool interest = GetEntityInterest(worldEntity);
                if (_trackedEntities.TryGetValue(worldEntity.Id, out var entityStatus))
                {
                    entityStatus.Frame = _currentFrame;
                    entityStatus.InterestToPlayer = interest;
                }
                else
                {
                    _trackedEntities.Add(worldEntity.Id, new(_currentFrame, true, interest));
                    if (worldEntity.IsAlive())
                        newEntities.Add(worldEntity);
                    // Logger.Debug($"{GameDatabase.GetFormattedPrototypeName(worldEntity.BaseData.PrototypeId)} = {worldEntity.BaseData.PrototypeId},");
                }
            }

            // Delete entities we are no longer interested in
            foreach (var kvp in _trackedEntities.Where(kvp => kvp.Value.Frame < _currentFrame && kvp.Value.InterestToPlayer == false))
                RemoveEntity(kvp.Key);      // TODO: Pass a reference to the entity we are removing instead

            // Add new entities
            foreach (Entity entity in newEntities)
                AddEntity(entity);
        }

        private void AddArea(Area area, bool isStartArea)
        {
            _trackedAreas.Add(area.Id, new(_currentFrame, true, true));
            SendMessage(area.MessageAddArea(isStartArea));
        }

        private void RemoveArea(Area area)
        {
            SendMessage(NetMessageRemoveArea.CreateBuilder()
                .SetAreaId(area.Id)
                .Build());
            _trackedAreas.Remove(area.Id);
        }

        private void AddCell(Cell cell)
        {
            SendMessage(cell.MessageCellCreate());
            _trackedCells.Add(cell.Id, new(_currentFrame, false, false));
        }

        private void RemoveCell(Cell cell)
        {
            var areaId = cell.Area.Id;
            if (_trackedAreas.ContainsKey(areaId))
            {
                SendMessage(NetMessageCellDestroy.CreateBuilder()
                    .SetAreaId(areaId)
                    .SetCellId(cell.Id)
                    .Build());
            }
            _trackedCells.Remove(cell.Id);
            LoadedCellCount--;
        }

        private void AddEntity(Entity entity)
        {
            SendMessage(entity.ToNetMessageEntityCreate());
        }

        private void RemoveEntity(ulong entityId)
        {
            // TODO: Pass a reference to the entity we are removing instead
            _trackedEntities.Remove(entityId);
            SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(entityId).Build());
        }

        private void CalcVolumes(Vector3 playerPosition)
        {
            _entitiesVolume = _cameraView.Translate(playerPosition);
            _visibileVolume = _entitiesVolume.Expand(ViewExpansionDistance);
            _invisibileVolume = _entitiesVolume.Expand(InvisibleExpansionDistance);
        }

        private void SetAOIVolume(float volume)
        {
            _aoiVolume = volume;
            _viewOffset = _aoiVolume / 8; // 3200 / 8 = 400
            InitializePlayerView(_lastCameraSetting);
        }

        private void SendMessage(IMessage message)
        {
            _playerConnection.SendMessage(message);
        }

        private class InterestStatus
        {
            public ulong Frame { get; set; }
            public bool Loaded { get; set; }
            public bool InterestToPlayer { get; set; }

            public InterestStatus(ulong frame, bool loaded, bool interestToPlayer)
            {
                Frame = frame;
                Loaded = loaded;
                InterestToPlayer = interestToPlayer;
            }
        }
    }
}
