using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class SpawnLocation
    {
        public Region Region;
        public HashSet<Area> SpawnAreas;
        public HashSet<Cell> SpawnCells;

        public SpawnLocation(Region region, Area area)
        {
            Region = region;
            SpawnAreas = new() { area };
            SpawnCells = new();
        }

        public SpawnLocation(Region region)
        {
            Region = region;
            SpawnAreas = new();
            SpawnCells = new();
        }

        public SpawnLocation(Region region, PrototypeId[] restrictToAreas, AssetId[] restrictToCells)
        {
            Region = region;
            SpawnAreas = new();
            AddAreaRefs(restrictToAreas);
            SpawnCells = new();
            AddCellAssets(restrictToCells);
        }

        public SpawnLocation(Region region, AssetId[] restrictToCells)
        {
            Region = region;
            SpawnAreas = new();
            SpawnCells = new();
            AddCellAssets(restrictToCells);
        }

        public SpawnLocation(SpawnLocation other, PrototypeId[] restrictToAreas, AssetId[] restrictToCells)
        {
            Region = other.Region;
            SpawnAreas = new(other.SpawnAreas);
            AddAreaRefs(restrictToAreas);

            SpawnCells = new(other.SpawnCells);
            AddCellAssets(restrictToCells);
        }

        public SpawnLocation(Region region, Cell cell)
        {
            Region = region;
            SpawnAreas = new();
            SpawnCells = new() { cell };
        }

        public void AddAreaRefs(PrototypeId[] restrictToAreas)
        {
            foreach (var areaRef in restrictToAreas)
                AddAreaRef(areaRef);
        }

        private void AddAreaRef(PrototypeId areaRef)
        {
            foreach (var area in Region.Areas.Values)
                if (area.PrototypeDataRef == areaRef)
                    SpawnAreas.Add(area);
        }

        public void AddCellAssets(AssetId[] assets)
        {
            if (assets.IsNullOrEmpty()) return;
            foreach (var asset in assets)
                AddCellRef(GameDatabase.GetDataRefByAsset(asset));
        }

        private void AddCellRef(PrototypeId cellRef)
        {
            foreach (var cell in Region.Cells)
                if (cell.PrototypeDataRef == cellRef)
                    SpawnCells.Add(cell);
        }

        public float CalcSpawnableArea()
        {
            float spawnableArea = 0.0f;
            foreach (var cell in Region.Cells)
                if (SpawnableCell(cell))
                    spawnableArea += cell.SpawnableArea;
            return spawnableArea;
        }

        public bool SpawnableCell(Cell cell)
        {
            if (SpawnAreas.Count > 0) 
                if (SpawnAreas.Contains(cell.Area) == false) 
                    return false;
            if (SpawnCells.Count > 0) 
                if (SpawnCells.Contains(cell) == false)
                    return false;
            return true;
        }
    }
}
