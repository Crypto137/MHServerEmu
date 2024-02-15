using Gazillion;
using MHServerEmu.Common.Extensions;
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
        private FrontendClient _client;
        private Game _game { get => _client.CurrentGame; }
        public HashSet<ulong> LoadedEntities { get; set; }
        public HashSet<uint> LoadedCells { get; set; }
        public int CellsInRegion { get; set; }
        public int LoadedCellCount { get; set; } = 0;

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
            _lastUpdateCenter = new();
        }

        public Aabb CalcAOIVolume(Vector3 playerPosition)
        {
            Aabb volume = _playerView.Translate(playerPosition);
            return volume.Expand(ExpandDistance);
        }

        private Dictionary<uint, List<Cell>> GetNewCells(Region region,Vector3 position, Area startArea)
        {
            Dictionary<uint, List<Cell>> cellsByArea = new();
            Aabb volume = CalcAOIVolume(position);

            foreach (var cell in region.IterateCellsInVolume(volume))
            {
                if (LoadedCells.Contains(cell.Id)) continue;
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

        public int LoadCellMessages(Region region, Vector3 position, List<GameMessage> messageList)
        {
            LoadedCells.Clear();
            Cell startCell = region.GetCellAtPosition(position);
            if (startCell == null) return 0;
            Area startArea = startCell.Area;
            CalcPlayerVolume(startCell.RegionBounds.Width);
            
            List<Cell> cellsInAOI = new ();
            var cellsByArea = GetNewCells(region, position, startArea);
            var sortedAreas = cellsByArea.Keys.OrderBy(id => id);
            foreach (var areaId in sortedAreas)
            {
                Area area = region.GetAreaById(areaId);
                messageList.Add(area.MessageAddArea(areaId == startArea.Id));

                var sortedCells = cellsByArea[areaId].OrderBy(cell => cell.Id);

                foreach (var cell in sortedCells)
                {
                    messageList.Add(cell.MessageCellCreate());
                    LoadedCells.Add(cell.Id);
                }
            }
            _lastUpdateCenter.Set(position);
            return LoadedCells.Count;
        }

        public List<GameMessage> UpdateAOI(Region region, Vector3 position)
        {
            List<GameMessage> messageList = new ();

            Aabb volume = CalcAOIVolume(position);
            List<Cell> cellsInAOI = new();      

            Cell startCell = region.GetCellAtPosition(position);
            if (startCell == null) return messageList;

            Area startArea = startCell.Area;
            var cellsByArea = GetNewCells(region, position, startArea);

            if (cellsByArea.Count != 0)
            {
                var sortedAreas = cellsByArea.Keys.OrderBy(id => id);

                // Add new

                HashSet<uint> usedAreas = new();

                foreach (var cellId in LoadedCells)
                {
                    Cell cell = region.GetCellbyId(cellId);
                    if (cell == null) continue;
                    usedAreas.Add(cell.Area.Id);
                }

               // int newCells = 0;
                foreach (var areaId in sortedAreas)
                {
                    if (usedAreas.Contains(areaId) == false)
                    {
                        Area area = region.GetAreaById(areaId);
                        messageList.Add(area.MessageAddArea(false));
                    }

                    var sortedCells = cellsByArea[areaId].OrderBy(cell => cell.Id);

                    foreach (var cell in sortedCells)
                    {
                        messageList.Add(cell.MessageCellCreate());
                        LoadedCells.Add(cell.Id); 
                        //newCells++;
                    }
                }

                CellsInRegion = LoadedCells.Count;// + newCells;
            } else return messageList;

            // Update Entity
           /* List<WorldEntity> cellEntities = new();
            EntityRegionSPContext context = new() { Flags = EntityRegionSPContextFlags.ActivePartition };
            Aabb entityBound = volume.Expand(-200.0f);
            foreach (var worldEntity in region.IterateEntitiesInVolume(entityBound, context))
            {
                if (LoadedCells.Contains(worldEntity.Location.Cell.Id) == true 
                    && LoadedEntities.Contains(worldEntity.BaseData.EntityId) == false)
                {
                    LoadedEntities.Add(worldEntity.BaseData.EntityId);
                    cellEntities.Add(worldEntity);
                }
            }
            // TODO Delete Entity

            if (cellEntities.Count > 0)
                messageList.AddRange(cellEntities.Select(entity => new GameMessage(entity.ToNetMessageEntityCreate())));
            */
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

            _lastUpdateCenter.Set(position);
            return messageList;
        }
        
        public List<GameMessage> EntitiesForCellId(uint cellId)
        { 
            List<GameMessage> messageList = new();

            Cell cell = _game.RegionManager.GetCell(cellId);
            if (cell == null || cell.Area.IsDynamicArea()) return messageList;

            List<WorldEntity> cellEntities = new();

            foreach (var entity in cell.Entities)
            {
                var worldEntity = entity as WorldEntity;
                if (LoadedEntities.Contains(worldEntity.BaseData.EntityId) == false)
                {
                    LoadedEntities.Add(entity.BaseData.EntityId);
                    cellEntities.Add(worldEntity);
                }
            }

            if (cellEntities.Count > 0)
                messageList.AddRange(cellEntities.Select(entity => new GameMessage(entity.ToNetMessageEntityCreate())));

            return messageList;
        }

        public List<GameMessage> EntitiesForRegion(Region region)
        {
            LoadedEntities.Clear();
            List<GameMessage> messageList = new();
            List<WorldEntity> regionEntities = new();

            foreach (var entity in region.Entities)
            {
                var worldEntity = entity as WorldEntity;
                if (LoadedCells.Contains(worldEntity.Location.Cell.Id))
                {
                    LoadedEntities.Add(entity.BaseData.EntityId);
                    regionEntities.Add(worldEntity);
                }
            }

            messageList.AddRange(regionEntities.Select(
                entity => new GameMessage(entity.ToNetMessageEntityCreate())
            ));

            return messageList;
        }

        public bool ShouldUpdate(Vector3 position)
        {
            return Vector3.DistanceSquared2D(_lastUpdateCenter, position) > UpdateDistance;
        }

    }
}
