using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators;
using MHServerEmu.Networking;

namespace MHServerEmu.Games.Regions
{
    public class AreaOfInterest
    {
        public class LoadStatus
        {
            public ulong Frame;
            public bool Loaded;
            public bool InterestToPlayer;

            public LoadStatus(ulong frame, bool loaded, bool interestToPlayer)
            {
                Frame = frame;
                Loaded = loaded;
                InterestToPlayer = interestToPlayer;
            }
        }
        private static readonly Logger Logger = LogManager.CreateLogger();
        private FrontendClient _client;
        private Game _game { get => _client.CurrentGame; }
        private Dictionary<ulong, LoadStatus> _loadedEntities;
        private Dictionary<uint, LoadStatus> _loadedCells;
        private Dictionary<uint, LoadStatus> _loadedAreas;

        public Region Region { get; private set; }
        public int CellsInRegion { get; set; }
        public int LoadedCellCount { get; set; } = 0;
        public List<GameMessage> Messages { get; private set; }
        public int LoadedEntitiesCount { get => _loadedEntities.Count; }

        private ulong _currentFrame;
        private Vector3 _lastUpdateCenter;

        private float _viewOffset = 600.0f;
        private float _AOIVolume = 4000.0f;

        private const float UpdateDistance = 200.0f;
        private const float ViewExpansionDistance = 600.0f;
        private const float InvisibleExpansionDistance = 1200.0f;

        private Aabb2 _cameraView;
        private Aabb2 _entitiesVolume;
        private Aabb2 _visibileVolume;
        private Aabb2 _invisibileVolume;
        private PrototypeId _lastCameraSetting;

        public AreaOfInterest(FrontendClient client)
        {
            _client = client;
            Messages = new();
            _loadedEntities = new();
            _loadedCells = new();
            _loadedAreas = new();
            LoadedCellCount = 0;
            _lastUpdateCenter = new();
            _currentFrame = 0;
        }

        private void CalcVolumes(Vector3 playerPosition)
        {
            _entitiesVolume = _cameraView.Translate(playerPosition);
            _visibileVolume = _entitiesVolume.Expand(ViewExpansionDistance);
            _invisibileVolume = _entitiesVolume.Expand(InvisibleExpansionDistance);
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
            Messages.Clear();
            _loadedAreas.Clear();
            _loadedCells.Clear();
            _loadedEntities.Clear();
            
            _currentFrame = 0;
            CellsInRegion = 0;
            Region = region;
            _lastCameraSetting = 0;

            int volume = 0;
            if (_client.Session != null)
                volume = _client.Session.Account.Player.AOIVolume;
            SetAOIVolume(volume >= 1600 && volume <= 5000 ? volume : 3200); 
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

            foreach(var area in region.IterateAreas())
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
            Messages.Add(area.MessageAddArea(isStartArea));
        }

        private void RemoveArea(Area area)
        {
            Messages.Add(new(NetMessageRemoveArea.CreateBuilder()
                .SetAreaId(area.Id)
                .Build()));
            _loadedAreas.Remove(area.Id);
        }

        private void AddCell(Cell cell)
        {
            Messages.Add(cell.MessageCellCreate());
            _loadedCells.Add(cell.Id, new(_currentFrame, false, false));
        }

        private void RemoveCell(Cell cell)
        {
            var areaId = cell.Area.Id;
            if (_loadedAreas.ContainsKey(areaId))
            {
                Messages.Add(new(NetMessageCellDestroy.CreateBuilder()
                    .SetAreaId(areaId)
                    .SetCellId(cell.Id)
                    .Build()));
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

        private void UpdateEntity()
        {
            Region region = Region;
            List<WorldEntity> newEntities = new();

            // Update Entity
            foreach (var worldEntity in region.IterateEntitiesInVolume(_entitiesVolume, new()))
            {
                if (_loadedCells.TryGetValue(worldEntity.Location.Cell.Id, out var status))
                    if (status.Loaded == false) continue;
                
                bool interest = GetEntityInterest(worldEntity);
                if (_loadedEntities.TryGetValue(worldEntity.BaseData.EntityId, out var entityStatus))
                {
                    entityStatus.Frame = _currentFrame;
                    entityStatus.InterestToPlayer = interest;
                }
                else
                {                    
                    _loadedEntities.Add(worldEntity.BaseData.EntityId, new(_currentFrame, true, interest));                   
                    if (worldEntity.IsAlive()) 
                        newEntities.Add(worldEntity);
                    // Logger.Debug($"{GameDatabase.GetFormattedPrototypeName(worldEntity.BaseData.PrototypeId)} = {worldEntity.BaseData.PrototypeId},");
                }
            }

            // Add new Entity
            if (newEntities.Count > 0)
                Messages.AddRange(newEntities.Select(entity => new GameMessage(entity.ToNetMessageEntityCreate()))); // TODO AddEntity            

            // Delete Entity
            List<ulong> toDelete = new();
            foreach (var entity in _loadedEntities)
            {
                if (entity.Value.Frame < _currentFrame && entity.Value.InterestToPlayer == false)
                {
                    Messages.Add(new(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(entity.Key).Build()));
                    toDelete.Add(entity.Key);
                }
            }
            foreach (var deleteId in toDelete) 
                _loadedEntities.Remove(deleteId); // TODO RemoveEntity           
        }

        public bool ShouldUpdate(Vector3 position)
        {
            return Vector3.DistanceSquared2D(_lastUpdateCenter, position) > UpdateDistance;
        }

        public void OnCellLoaded(uint cellId)
        {
            LoadedCellCount++;
            if (_loadedCells.TryGetValue(cellId, out var cell)) cell.Loaded = true;
        }

        public bool CheckTargeCell(Transition target)
        {
            if (_loadedCells.TryGetValue(target.Location.Cell.Id, out var cell))
                return cell.Loaded == false;
            return true;
        }

        public void ForceCellLoad()
        {
            foreach (var cell in _loadedCells)
                cell.Value.Loaded = true;
        }

        public void SetAOIVolume(float volume)
        {
            _AOIVolume = volume;
            _viewOffset = _AOIVolume / 8; // 3200 / 8 = 400
            InitPlayerView(_lastCameraSetting);            
        }

        public bool Update(Vector3 newPosition, bool isStart = false)
        {
            Messages.Clear();
            CalcVolumes(newPosition);
            if (isStart)
            {
                Area startArea = Region.GetAreaAtPosition(newPosition);
                AddArea(startArea, true);
            }

            _currentFrame++;
            // update Entities
            if (isStart == false) UpdateEntity();

            // update Areas
            UpdateAreas();

            // update Cells
            if (UpdateCells())
            {
                Messages.Add(new(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build()));

                // Mini map
                MiniMapArchive miniMap = new(Region.RegionPrototype.AlwaysRevealFullMap);
                if (miniMap.IsRevealAll == false) miniMap.Map = Array.Empty<byte>();

                Messages.Add(new(NetMessageUpdateMiniMap.CreateBuilder()
                    .SetArchiveData(miniMap.Serialize())
                    .Build()));
            }

            bool update = Messages.Count > 0;
            if (update) _lastUpdateCenter.Set(newPosition);
            return update;
        }

        public bool EntityLoaded(ulong entityId)
        {
            return _loadedEntities.ContainsKey(entityId);
        }
    }
}
