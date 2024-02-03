using Gazillion;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Networking;

namespace MHServerEmu.Games.Regions
{
    public class AreaOfInterest
    {
        private FrontendClient _client;
        private Game _game { get => _client.CurrentGame; }
        public HashSet<ulong> LoadedEntities { get; set; }
        public HashSet<uint> LoadedCells { get; set; }

        private float _areaSize = 4000.0f;

        public AreaOfInterest(FrontendClient client) 
        {
            _client = client;
            LoadedEntities = new();
            LoadedCells = new();
        }

        public Aabb CalcAOIVolume(Vector3 pos)
        {
            return new(pos, _areaSize, _areaSize, 2034.0f);
        }

        public int LoadCellMessages(Region region, Vector3 position, List<GameMessage> messageList)
        {
            LoadedCells.Clear();
            Aabb volume = CalcAOIVolume(position);
            List<Cell> cellsInAOI = new ();

            Dictionary<uint, List<Cell>> cellsByArea = new ();

            Cell startCell = region.GetCellAtPosition(position);
            if (startCell == null) return 0;

            uint startArea = startCell.Area.Id;
            foreach (var cell in region.IterateCellsInVolume(volume))
            {
                if (cellsByArea.ContainsKey(cell.Area.Id) == false)
                    cellsByArea[cell.Area.Id] = new ();

                cellsByArea[cell.Area.Id].Add(cell);
            }

            var sortedAreas = cellsByArea.Keys.OrderBy(id => id);

            foreach (var areaId in sortedAreas)
            {
                Area area = region.GetAreaById(areaId);
                messageList.Add(area.MessageAddArea(areaId == startArea));

                var sortedCells = cellsByArea[areaId].OrderBy(cell => cell.Id);

                foreach (var cell in sortedCells)
                {
                    messageList.Add(cell.MessageCellCreate());
                    LoadedCells.Add(cell.Id);
                }
            }
            return LoadedCells.Count;
        }

        public List<GameMessage> UpdateAOI(Region region, Vector3 position)
        {
            List<GameMessage> messageList = new ();

            Aabb volume = CalcAOIVolume(position);
            List<Cell> cellsInAOI = new();
            HashSet<uint> cells = LoadedCells;
            
            Dictionary<uint, List<Cell>> cellsByArea = new();

            Cell startCell = region.GetCellAtPosition(position);
            if (startCell == null) return messageList;

            uint startArea = startCell.Area.Id;
            foreach (var cell in region.IterateCellsInVolume(volume))
            {
                if (cells.Contains(cell.Id)) continue;
                if (cellsByArea.ContainsKey(cell.Area.Id) == false)
                    cellsByArea[cell.Area.Id] = new();

                cellsByArea[cell.Area.Id].Add(cell);
            }

            if (cellsByArea.Count == 0) return messageList;

            var sortedAreas = cellsByArea.Keys.OrderBy(id => id);

            // Add new

            HashSet<uint> usedAreas = new();

            foreach (var cellId in cells)
            {
                Cell cell = region.GetCellbyId(cellId);
                if (cell == null) continue;
                usedAreas.Add(cell.Area.Id);
            }

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
                    cells.Add(cell.Id); 
                }
            }

            region.CellsInRegion = LoadedCells.Count;

            if (messageList.Count > 0)
            {
                messageList.Add(new(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build()));

                // Mini map
                MiniMapArchive miniMap = new(RegionManager.RegionIsHub(region.PrototypeId)); // Reveal map by default for hubs
                if (miniMap.IsRevealAll == false) miniMap.Map = Array.Empty<byte>();

                messageList.Add(new(NetMessageUpdateMiniMap.CreateBuilder()
                    .SetArchiveData(miniMap.Serialize())
                    .Build()));
                
                //client.LoadedCellCount = client.LoadedCells.Count;
            }
            // TODO delete old

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
                if (LoadedEntities.Contains(worldEntity.Location.Cell.Id) == false)
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
    }
}
