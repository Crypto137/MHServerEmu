using Gazillion;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
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
        private const float ExpandDistance = 800.0f;
        private const float DefaultDistance = 800.0f;
        private const float MaxZ = 100000.0f;

        private Aabb _playerView;

        public AreaOfInterest(FrontendClient client) 
        {
            _client = client;
            LoadedEntities = new();
            LoadedCells = new();
            LoadedCellCount = 0;
            _lastUpdateCenter = new();
            _currentFrame = 0;
        }

        public Aabb CalcAOIVolume(Vector3 playerPosition)
        {
            Aabb volume = _playerView.Translate(playerPosition);
            return volume.Expand(ExpandDistance);
        }

        private Dictionary<uint, List<Cell>> GetNewCells(Vector3 position, Area startArea)
        {
            Dictionary<uint, List<Cell>> cellsByArea = new();
            Aabb volume = CalcAOIVolume(position);

            foreach (var cell in Region.IterateCellsInVolume(volume))
            {
                if (LoadedCells.TryGetValue(cell.Id, out var status))
                {
                    status.Frame = _currentFrame;
                    continue;
                }
                if (cell.Area.IsDynamicArea() || cell.Area == startArea || startArea.AreaConnections.Any(connection => connection.ConnectedArea == cell.Area))
                {
                    if (cellsByArea.ContainsKey(cell.Area.Id) == false)
                        cellsByArea[cell.Area.Id] = new();

                    cellsByArea[cell.Area.Id].Add(cell);
                }
            }
            return cellsByArea;
        }

        public void CalcPlayerVolume(float cellWidth)
        {
            float distance = DefaultDistance;
            if (cellWidth != DefaultCellWidth) distance = cellWidth / 1.5f;
            Vector3 offset = new(ViewOffset, ViewOffset, 0.0f);
            _playerView = new Aabb(
                new(-distance, -distance, -MaxZ),
                new(distance, distance, MaxZ))
                .Translate(offset);
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
           /* if (RegionManager.RegionIsHub(region.PrototypeId))
                _playerView = new(startCell.Area.RegionBounds); // Fix for HUB
            else*/
                CalcPlayerVolume(startCell.RegionBounds.Width);
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
            Aabb volume = CalcAOIVolume(position);
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
            Aabb volume = CalcAOIVolume(position);
            List<WorldEntity> cellEntities = new();
            _currentFrame++;
            // Update Entity
            EntityRegionSPContext context = new() { Flags = EntityRegionSPContextFlags.ActivePartition };
            foreach (var worldEntity in region.IterateEntitiesInVolume(volume, context))
            {
                if (LoadedCells.TryGetValue(worldEntity.Location.Cell.Id, out var status))
                    if (status.Loaded == false) continue;

                if (LoadedEntities.TryGetValue(worldEntity.BaseData.EntityId, out var entityStatus))
                    entityStatus.Frame = _currentFrame;
                else
                {
                    bool interest = GetEntityInterest(worldEntity);
                    LoadedEntities.Add(worldEntity.BaseData.EntityId, new(_currentFrame,true, interest));
                    cellEntities.Add(worldEntity);
                }
            }

            if (cellEntities.Count > 0)
                messageList.AddRange(cellEntities.Select(entity => new GameMessage(entity.ToNetMessageEntityCreate())));

            List<ulong> toDelete = new();

            // TODO Delete Entity
            foreach(var entity in LoadedEntities) 
            {
                if (entity.Value.Frame < _currentFrame && entity.Value.InterestToPlayer == false)
                {
                    messageList.Add(new(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(entity.Key).Build()));
                    toDelete.Add(entity.Key);
                }
            }
            foreach(var deleteId in toDelete) LoadedEntities.Remove(deleteId);

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
