using Gazillion;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Networking;

namespace MHServerEmu.Games.Regions
{
    public class AreaOfInterest
    {
        public static void LoadMessagesForAOI(Region region, Vector3 position, List<GameMessage> messageList, HashSet<uint> cells)
        {
            Aabb volume = CalcAOIVolume(position);
            List<Cell> cellsInAOI = new ();

            Dictionary<uint, List<Cell>> cellsByArea = new ();

            Cell startCell = region.GetCellAtPosition(position);
            if (startCell == null) return;

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
                    cells.Add(cell.Id);
                }
            }
        }

        public static List<GameMessage> UpdateAOI(FrontendClient client, Vector3 position)
        {
            List<GameMessage> messageList = new ();
            List<WorldEntity> regionEntities = new();

            Aabb volume = CalcAOIVolume(position);
            List<Cell> cellsInAOI = new();
            HashSet<uint> cells = client.LoadedCells;
            
            Dictionary<uint, List<Cell>> cellsByArea = new();

            Region region = client.Region;

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
                    regionEntities.AddRange(client.CurrentGame.EntityManager.GetNewEntitiesForCell(region, cell.Id, client));
                }
            }

            region.CellsInRegion = client.LoadedCells.Count;

            if (messageList.Count > 0)
            {
                messageList.Add(new(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build()));

                // Mini map
                MiniMapArchive miniMap = new(RegionManager.RegionIsHub(region.PrototypeId)); // Reveal map by default for hubs
                if (miniMap.IsRevealAll == false) miniMap.Map = Array.Empty<byte>();

                messageList.Add(new(NetMessageUpdateMiniMap.CreateBuilder()
                    .SetArchiveData(miniMap.Serialize())
                    .Build()));
                
                messageList.AddRange(regionEntities.Select(
                    entity => new GameMessage(entity.ToNetMessageEntityCreate())
                ));
                //client.LoadedCellCount = client.LoadedCells.Count;
            }
            // TODO delete old

            return messageList;
        }

        public static Aabb CalcAOIVolume(Vector3 pos)
        {
            return new(pos, 4000.0f, 4000.0f, 2034.0f);
        }


    }
}
