using Gazillion;
using MHServerEmu.Common.Helpers;
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

        private FrontendClient _client;
        private Game _game { get => _client.CurrentGame; }
        public Dictionary<ulong, LoadStatus> LoadedEntities { get; set; }
        public Dictionary<uint, LoadStatus> LoadedCells { get; set; }

        public Region Region { get; private set; }
        public int CellsInRegion { get; set; }
        public int LoadedCellCount { get; set; } = 0;
        private ulong _currentFrame;

        private Vector3 _lastUpdateCenter;
        private const float DefaultCellWidth = 2304.0f;
        private const float UpdateDistance = 200.0f;
        private const float ViewOffset = 400.0f;
        private const float ViewExpansionDistance = 800.0f;
        private const float EntityExpansionDistance = 600.0f;
        private const float MaxZ = 100000.0f;

        private Aabb2 _playerView;
        private Aabb2 _entitiesToConsiderBounds;
        private Aabb2 _visibilityBounds;

        public Aabb2 CalcEntitiesToConsiderBounds(Vector3 playerPosition)
        {
            _entitiesToConsiderBounds = _playerView.Translate(playerPosition).Expand(EntityExpansionDistance);
            return _entitiesToConsiderBounds;
        }

        public Aabb2 CalcVisibilityBounds(Vector3 playerPosition)
        {
            _visibilityBounds = _playerView.Translate(playerPosition).Expand(ViewExpansionDistance);
            return _visibilityBounds;
        }

        public AreaOfInterest(FrontendClient client)
        {
            _client = client;
            LoadedEntities = new();
            LoadedCells = new();
            LoadedCellCount = 0;
            _lastUpdateCenter = new();
            _currentFrame = 0;
        }

        public void InitPlayerView(CameraSettingPrototype cameraSettingPrototype)
        {
            _playerView = new Aabb2(new Vector3(ViewOffset, ViewOffset, 0.0f), DefaultCellWidth * 1.5f);
            AdaptPlayerViewWithCameraSettings(cameraSettingPrototype);
        }


        private void AdaptPlayerViewWithCameraSettings(CameraSettingPrototype cameraSettingPrototype)
        {
            if (cameraSettingPrototype == null) return;

            CameraSettingCollectionPrototype cameraSettingCollectionPrototype = GameDatabase.GetPrototype<CameraSettingCollectionPrototype>(cameraSettingPrototype.DataRef);
            if (cameraSettingCollectionPrototype == null)
            {
                GlobalsPrototype globalsPrototype = GameDatabase.GetGlobalsPrototype();
                if (globalsPrototype == null) return;
                cameraSettingCollectionPrototype = GameDatabase.GetPrototype<CameraSettingCollectionPrototype>(globalsPrototype.PlayerCameraSettings);
            }

            CameraSettingPrototype cameraSetting = cameraSettingCollectionPrototype?.CameraSettings?.FirstOrDefault();
            if (cameraSetting == null) return;

            Vector3 NormalizedDirection = Vector3.Normalize2D(new(cameraSetting.DirectionX, cameraSetting.DirectionY, cameraSetting.DirectionZ));
            float angleOffset = MathHelper.WrapAngleRadians(Vector3.FromDeltaVector2D(NormalizedDirection).Yaw + MathHelper.Pi - (MathHelper.Pi / 4f));
            Transform3 rotation = Transform3.RotationZYX(new(0f, 0f, angleOffset));

            _playerView = new Aabb2();
            foreach (Point2 point in _playerView.GetPoints())
            {
                Point3 p = rotation * new Point3(point.X, point.Y, 0f);
                _playerView = _playerView.Expand(new Vector2(p.X, p.Y));
            }
        }

        public Aabb CalcCellVolume(Vector3 playerPosition)
        {            
            CalcVisibilityBounds(playerPosition);

            return new Aabb(
                new Vector3(_visibilityBounds.Min.X, _visibilityBounds.Min.Y, -MaxZ),
                new Vector3(_visibilityBounds.Max.X, _visibilityBounds.Max.Y, MaxZ));
        }

        public Aabb2 CalcEnittyVolume(Vector3 playerPosition)
        {
            CalcEntitiesToConsiderBounds(playerPosition);
            return _entitiesToConsiderBounds;
        }

        private Dictionary<uint, List<Cell>> GetNewCells(Vector3 position, Area startArea)

        {
            Dictionary<uint, List<Cell>> cellsByArea = new();
            Aabb volume = CalcCellVolume(position);

            foreach (var cell in Region.IterateCellsInVolume(volume))
            {
                if (LoadedCells.TryGetValue(cell.Id, out var status))
                {
                    status.Frame = _currentFrame;
                    continue;
                }
               // if (cell.Area.IsDynamicArea() || cell.Area == startArea || startArea.AreaConnections.Any(connection => connection.ConnectedArea == cell.Area))
                {
                    if (cellsByArea.ContainsKey(cell.Area.Id) == false)
                        cellsByArea[cell.Area.Id] = new();

                    cellsByArea[cell.Area.Id].Add(cell);
                }
            }
            return cellsByArea;
        }

        public void ResetAOI(Region region, Vector3 position)
        {   
            LoadedCells.Clear();
            LoadedEntities.Clear();
            _currentFrame = 0;
            CellsInRegion = 0;
            Region = region;          

            Cell startCell = region.GetCellAtPosition(position);
            if (startCell == null) return;
            InitPlayerView(startCell.Area.AreaPrototype.PlayerCameraSettings.As<CameraSettingPrototype>());
            /* if (RegionManager.RegionIsHub(region.PrototypeId))
                 _playerView = new(startCell.Area.RegionBounds); // Fix for HUB
             else*/
        }

        public static bool GetEntityInterest(WorldEntity worldEntity)
        {
            // TODO write all Player interests for entity
            if (worldEntity.TrackAfterDiscovery) return true;
            return false;
        }

        public List<GameMessage> UpdateCells(Vector3 position)
        {
            List<GameMessage> messageList = new ();
            Region region = Region;
            Aabb volume = CalcCellVolume(position);

            _currentFrame++;
            List<Cell> cellsInAOI = new();
            Cell startCell = region.GetCellAtPosition(position);
            if (startCell == null) return messageList;

            Area startArea = startCell.Area;
            var cellsByArea = GetNewCells(position, startArea);

            if (cellsByArea.Count == 0) return messageList;

            var sortedAreas = cellsByArea.Keys.OrderBy(id => id);

            // Add new

            HashSet<uint> usedAreas = new();

            foreach (var cellStatus in LoadedCells)
            {
                Cell cell = region.GetCellbyId(cellStatus.Key);
                if (cell == null) continue;
                usedAreas.Add(cell.Area.Id);
            }

            foreach (var areaId in sortedAreas)
            {
                if (usedAreas.Contains(areaId) == false)
                {
                    Area area = region.Areas[areaId];
                    messageList.Add(area.MessageAddArea(false));
                }

                var sortedCells = cellsByArea[areaId].OrderBy(cell => cell.Id);

                foreach (var cell in sortedCells)
                {
                    messageList.Add(cell.MessageCellCreate());
                    LoadedCells.Add(cell.Id, new(_currentFrame, false, false));
                }
            }

            CellsInRegion = LoadedCells.Count;

            if (messageList.Count > 0)
            {
                messageList.Add(new(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build()));

                // Mini map
                MiniMapArchive miniMap = new(RegionManager.RegionIsHub(region.PrototypeId)); // Reveal map by default for hubs
                if (miniMap.IsRevealAll == false) miniMap.Map = Array.Empty<byte>();

                messageList.Add(new(NetMessageUpdateMiniMap.CreateBuilder()
                    .SetArchiveData(miniMap.Serialize())
                    .Build()));

                //LoadedCellCount = client.LoadedCells.Count;
            }
            // TODO delete old

            // If cell not use and have not entity with interest then can be remove

            _lastUpdateCenter.Set(position);
            return messageList;
        }

        public List<GameMessage> UpdateEntity(Vector3 position)
        {
            Region region = Region;
            List<GameMessage> messageList = new();
            Aabb2 volume = CalcEnittyVolume(position);
            List<WorldEntity> cellEntities = new();
            _currentFrame++;
            // Update Entity
            EntityRegionSPContext context = new() { Flags = EntityRegionSPContextFlags.ActivePartition | EntityRegionSPContextFlags.StaticPartition};
            foreach (var worldEntity in region.IterateEntitiesInVolume(volume, context))
            {
                if (LoadedCells.TryGetValue(worldEntity.Location.Cell.Id, out var status))
                    if (status.Loaded == false) continue;

                if (LoadedEntities.TryGetValue(worldEntity.BaseData.EntityId, out var entityStatus))
                    entityStatus.Frame = _currentFrame;
                else
                {
                    bool interest = GetEntityInterest(worldEntity);
                    LoadedEntities.Add(worldEntity.BaseData.EntityId, new(_currentFrame, true, interest));
                    cellEntities.Add(worldEntity);
                }
            }

            if (cellEntities.Count > 0)
                messageList.AddRange(cellEntities.Select(entity => new GameMessage(entity.ToNetMessageEntityCreate())));

            List<ulong> toDelete = new();

            // TODO Delete Entity
            foreach (var entity in LoadedEntities)
            {
                if (entity.Value.Frame < _currentFrame && entity.Value.InterestToPlayer == false)
                {
                    messageList.Add(new(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(entity.Key).Build()));
                    toDelete.Add(entity.Key);
                }
            }
            foreach (var deleteId in toDelete) LoadedEntities.Remove(deleteId);

            _lastUpdateCenter.Set(position);
            return messageList;
        }

        public bool ShouldUpdate(Vector3 position)
        {
            return Vector3.DistanceSquared2D(_lastUpdateCenter, position) > UpdateDistance;
        }

        public void OnCellLoaded(uint cellId)
        {
            LoadedCellCount++;
            if (LoadedCells.TryGetValue(cellId, out var cell)) cell.Loaded = true;
        }

        public bool CheckTargeCell(Transition target)
        {
            if (LoadedCells.TryGetValue(target.Location.Cell.Id, out var cell))
                return cell.Loaded == false;
            return true;
        }

        public void ForseCellLoad()
        {
            foreach (var cell in LoadedCells)
                cell.Value.Loaded = true;
        }
    }
}
