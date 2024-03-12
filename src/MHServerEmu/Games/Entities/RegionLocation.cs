using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class RegionLocation
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        private Region _region;
        public Region Region { get => _region; set { _region = value; Cell = null; } }
        public Cell Cell { get; set; }
        public Area Area { get => Cell.Area; }
        public ulong RegionId { get => Region.Id; }
        public uint AreaId { get => Area.Id; }
        public uint CellId { get => Cell.Id; }

        public bool IsValid() => _region != null;

        private Vector3 _position;
        public Vector3 GetPosition() => IsValid() ? _position : new();
        public bool SetPosition(Vector3 position)
        {
            if (!Vector3.IsFinite(position))
            {
                Logger.Warn($"Non-finite position ({position}) given to region location: {ToString()}");
                return false;
            }
            if (_region == null) return false;

            Cell oldCell = Cell;
            if (oldCell == null || !oldCell.IntersectsXY(position))
            {
                Cell newCell = _region.GetCellAtPosition(position);
                if (newCell == null) return false;
                else Cell = newCell;
            }
            _position = position;
            return true;
        }

        private Orientation _orientation;
        public Orientation GetOrientation() => IsValid() ? _orientation : new();

        public void SetOrientation(Orientation orientation)
        {
            if (Orientation.IsFinite(orientation)) _orientation = orientation;
        }

        public static float ProjectToFloor(CellPrototype cell, Vector3 position)
        {
            Vector3 cellPos = position - cell.BoundingBox.Min;
            cellPos.X /= cell.BoundingBox.Width;
            cellPos.Y /= cell.BoundingBox.Length;
            int mapX = (int)cell.HeightMap.HeightMapSize.X;
            int mapY = (int)cell.HeightMap.HeightMapSize.Y;
            int x = Math.Clamp((int)(cellPos.X * mapX), 0, mapX - 1);
            int y = Math.Clamp((int)(cellPos.Y * mapY), 0, mapY - 1);
            return cell.HeightMap.HeightMapData[y * mapX + x];
        }

        public static Vector3 ProjectToFloor(Region region, Vector3 regionPos)
        {
            Cell cell = region.GetCellAtPosition(regionPos);
            if (cell == null) return regionPos;
            Vector3 postion = new(regionPos);
            postion.Z = cell.RegionBounds.Center.Z + ProjectToFloor(cell.CellProto, postion);
            return postion;
        }

        public override string ToString()
        {
            return string.Format("rloc.pos={0}, rloc.rot={1}, rloc.region={2}, rloc.area={3}, rloc.cell={4}, rloc.entity={5}",
               _position,
               _orientation,
               _region != null ? _region.ToString() : "Unknown",
               Area != null ? Area : "Unknown",
               Cell != null ? Cell : "Unknown",
               "Unknown");
        }
    }    
}