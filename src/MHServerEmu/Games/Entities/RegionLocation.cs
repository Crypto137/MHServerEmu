using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class RegionLocation
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        private Region _region;
        public Region Region { get => _region; set { _region = value; Cell = null; } }
        public Cell Cell { get; private set; }
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

        private Vector3 _orientation;
        public Vector3 GetOrientation() => IsValid() ? _orientation : new();
        public void SetOrientation(Vector3 orientation)
        {
            if (Vector3.IsFinite(orientation)) _orientation = orientation;
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