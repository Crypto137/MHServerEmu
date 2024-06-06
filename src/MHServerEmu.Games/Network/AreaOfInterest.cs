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

        private readonly Dictionary<uint, AreaInterestStatus> _trackedAreas = new();
        private readonly Dictionary<uint, CellInterestStatus> _trackedCells = new();
        private readonly Dictionary<ulong, EntityInterestStatus> _trackedEntities = new();

        private PlayerConnection _playerConnection;
        private Game _game;

        private ulong _currentFrame = 0;
        private Vector3 _lastUpdatePosition = new();

        private float _viewOffset = 600.0f;
        private float _aoiVolume = AOIVolumeDefault;

        private Aabb2 _cameraView;
        private Aabb2 _entitiesVolume;
        private Aabb2 _visibleVolume;
        private Aabb2 _invisibleVolume;
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
            if (_trackedCells.TryGetValue(cellId, out CellInterestStatus cellInterest) == false)
                return false;

            return cellInterest.IsLoaded;
        }

        public bool InterestedInEntity(ulong entityId)
        {
            // TODO: Filter by channel
            return _trackedEntities.ContainsKey(entityId);
        }

        public bool OnCellLoaded(uint cellId)
        {
            if (_trackedCells.TryGetValue(cellId, out CellInterestStatus cell) == false)
                Logger.WarnReturn(false, $"OnCellLoaded(): Loaded cell id {cell} is not being tracked!");

            LoadedCellCount++;
            _trackedCells[cellId] = new(_currentFrame, true);
            return true;
        }

        public void ForceCellLoad()
        {
            foreach (var kvp in _trackedCells.Where(kvp => kvp.Value.IsLoaded == false))
                _trackedCells[kvp.Key] = new(_currentFrame, true);
        }

        public static bool IsDiscoverable(WorldEntity worldEntity)
        {
            // REMOVEME: Use GetCurrentInterestPolicies() and GetNewInterestPolicies() instead
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
                    if (area.RegionBounds.Intersects(_invisibleVolume) == false)
                        RemoveArea(area);
                }
                else
                {
                    if (area.RegionBounds.Intersects(_visibleVolume))
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
                if (cell.RegionBounds.Intersects(_invisibleVolume) == false)
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
            foreach (Cell cell in region.IterateCellsInVolume(_visibleVolume))
            {
                if (_trackedAreas.ContainsKey(cell.Area.Id) == false) continue;
                if (_trackedCells.ContainsKey(cell.Id)) continue;

                if (cell.RegionBounds.Intersects(_visibleVolume))
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

                // TODO: Remove this when we start using GetNewInterestPolicies() that does this check 
                if (InterestedInCell(worldEntity.RegionLocation.Cell.Id) == false)
                    continue;

                if (_trackedEntities.ContainsKey(worldEntity.Id) == false && worldEntity.IsAlive())
                    newEntities.Add(worldEntity);

                // TODO: Use GetNewInterestPolicies() instead
                AOINetworkPolicyValues interestPolicies = AOINetworkPolicyValues.AOIChannelProximity;
                if (IsDiscoverable(worldEntity)) interestPolicies |= AOINetworkPolicyValues.AOIChannelDiscovery;
                _trackedEntities[worldEntity.Id] = new(_currentFrame, interestPolicies);
            }

            // Delete entities we are no longer interested in
            foreach (var kvp in _trackedEntities.Where(kvp => kvp.Value.LastUpdateFrame < _currentFrame
            && kvp.Value.InterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelDiscovery) == false))
                RemoveEntity(kvp.Key);      // TODO: Pass a reference to the entity we are removing instead

            // Add new entities
            foreach (Entity entity in newEntities)
                AddEntity(entity);
        }

        private void AddArea(Area area, bool isStartArea)
        {
            _trackedAreas.Add(area.Id, new(_currentFrame));
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
            _trackedCells.Add(cell.Id, new(_currentFrame, false));
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

        /// <summary>
        /// Returns the current <see cref="AOINetworkPolicyValues"/> for the provided <see cref="Entity"/>.
        /// </summary>
        private AOINetworkPolicyValues GetCurrentInterestPolicies(Entity entity)
        {
            if (_trackedEntities.TryGetValue(entity.Id, out EntityInterestStatus interestStatus) == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            return interestStatus.InterestPolicies;
        }

        /// <summary>
        /// Builds new <see cref="AOINetworkPolicyValues"/> for the provided <see cref="Entity"/>.
        /// </summary>
        private AOINetworkPolicyValues GetNewInterestPolicies(Entity entity)
        {
            if (entity == null) return Logger.WarnReturn(AOINetworkPolicyValues.AOIChannelNone, "GetNewInterestPolicies(): entity == null");

            Player player = _playerConnection.Player;

            // Destroyed and not in game entities cannot have interest
            if (entity.IsDestroyed || entity.IsInGame == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            // Players who are not in game cannot be interested in entities
            if (player.IsInGame == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            //      Add more filters here

            AOINetworkPolicyValues newInterestPolicies = AOINetworkPolicyValues.AOIChannelNone;

            if (entity is WorldEntity worldEntity)
            {
                // Validate that the entity's location is valid on the client before including it in the proximity channel
                if (worldEntity.IsInWorld && _visibleVolume.IntersectsXY(worldEntity.RegionLocation.Position) && InterestedInCell(worldEntity.Cell.Id))
                    newInterestPolicies |= AOINetworkPolicyValues.AOIChannelProximity;
            }

            // Ownership
            if (entity.IsOwnedBy(player.Id))
                newInterestPolicies |= AOINetworkPolicyValues.AOIChannelOwner;

            // TODO: Discovery, Party, Trade

            // Filter out results that don't match channels specified in the entity prototype
            if ((newInterestPolicies & entity.CompatibleReplicationChannels) == AOINetworkPolicyValues.AOIChannelNone)
                return AOINetworkPolicyValues.AOIChannelNone;

            return newInterestPolicies;
        }

        private void CalcVolumes(Vector3 playerPosition)
        {
            _entitiesVolume = _cameraView.Translate(playerPosition);
            _visibleVolume = _entitiesVolume.Expand(ViewExpansionDistance);
            _invisibleVolume = _entitiesVolume.Expand(InvisibleExpansionDistance);
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

        private readonly struct AreaInterestStatus
        {
            public readonly ulong LastUpdateFrame;

            public AreaInterestStatus(ulong frame)
            {
                LastUpdateFrame = frame;
            }
        }

        private readonly struct CellInterestStatus
        {
            public readonly ulong LastUpdateFrame;
            public readonly bool IsLoaded;

            public CellInterestStatus(ulong frame, bool isLoaded)
            {
                LastUpdateFrame = frame;
                IsLoaded = isLoaded;
            }
        }

        private readonly struct EntityInterestStatus
        {
            public readonly ulong LastUpdateFrame;
            public readonly AOINetworkPolicyValues InterestPolicies;

            public EntityInterestStatus(ulong frame, AOINetworkPolicyValues interestPolicies)
            {
                LastUpdateFrame = frame;
                InterestPolicies = interestPolicies;
            }
        }
    }
}
