using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
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
        public Area Area { get => Cell?.Area; }
        public ulong RegionId { get => Region != null ? Region.Id : 0; }
        public uint AreaId { get => Area != null ? Area.Id : 0; }
        public uint CellId { get => Cell != null ? Cell.Id : 0; }

        public bool IsValid() => _region != null;

        private Vector3 _position;
        public Vector3 GetPosition() => IsValid() ? _position : Vector3.Zero;
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
        public Orientation GetOrientation() => IsValid() ? _orientation : Orientation.Zero;

        public void SetOrientation(Orientation orientation)
        {
            if (Orientation.IsFinite(orientation)) _orientation = orientation;
        }

        public static float ProjectToFloor(Cell cell, Vector3 position)
        {
            Vector3 cellPos = position - cell.RegionBounds.Min;
            var cellProto = cell.CellProto;
            cellPos.X /= cellProto.BoundingBox.Width;
            cellPos.Y /= cellProto.BoundingBox.Length;
            int mapX = (int)cellProto.HeightMap.HeightMapSize.X;
            int mapY = (int)cellProto.HeightMap.HeightMapSize.Y;
            int x = Math.Clamp((int)(cellPos.X * mapX), 0, mapX - 1);
            int y = Math.Clamp((int)(cellPos.Y * mapY), 0, mapY - 1);
            return cellProto.HeightMap.HeightMapData[y * mapX + x];
        }

        public static Vector3 ProjectToFloor(Region region, Vector3 regionPos)
        {
            Cell cell = region.GetCellAtPosition(regionPos);
            if (cell == null) return regionPos;
            Vector3 postion = new(regionPos);

            var height = ProjectToFloor(cell, postion);
            if (height > Int16.MinValue) 
                postion.Z = cell.RegionBounds.Center.Z + height;
            else if (region.NaviMesh.IsMeshValid)
                return region.NaviMesh.ProjectToMesh(regionPos);

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

        public void Initialize(WorldEntity worldEntity) { }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            if (Region != null && Region.HasKeyword(keywordProto)) return true;
            Area area = Area;
            if (area != null && area.HasKeyword(keywordProto)) return true;
            return false;
        }

    }

    public class RegionLocationSafe
    {
        public PrototypeId AreaRef { get; private set; }
        public PrototypeId RegionRef { get; private set; }
        public PrototypeId CellRef { get; private set; }
        public ulong RegionId { get; private set; }
        public uint AreaId { get; private set; }
        public uint CellId { get; private set; }
        public Vector3 Position { get; private set; }
        public Orientation Orientation { get; private set; }

        public Area GetArea()
        {
            if (AreaId == 0) return null;
            Region region = GetRegion();
            Area area = region?.GetAreaById(AreaId);
            return area;
        }

        public Region GetRegion()
        {
            if (RegionId == 0) return null;
            Game game = Game.Current;
            RegionManager manager = game?.RegionManager;
            return manager?.GetRegion(RegionId);
        }

        public RegionLocationSafe Set(RegionLocation regionLocation)
        {
            Region region = regionLocation.Region;
            if (region != null)
            {
                RegionId = region.Id;
                RegionRef = region.PrototypeDataRef;
            }
            else
            {
                RegionId = 0;
                RegionRef = PrototypeId.Invalid;
            }

            Area area = regionLocation.Area;
            if (area != null)
            {
                AreaId = area.Id;
                AreaRef = area.PrototypeDataRef;
            }
            else
            {
                AreaId = 0;
                AreaRef = PrototypeId.Invalid;
            }

            Cell cell = regionLocation.Cell;
            if (cell != null)
            {
                CellId = cell.Id;
                CellRef = cell.PrototypeId;
            }
            else
            {
                CellId = 0;
                CellRef = PrototypeId.Invalid;
            }

            Position = new(regionLocation.GetPosition());
            Orientation = new(regionLocation.GetOrientation());

            return this;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            Region region = GetRegion();
            if (region != null && region.HasKeyword(keywordProto)) return true;
            Area area = GetArea();
            if (area != null && area.HasKeyword(keywordProto)) return true;
            return false;
        }
    }
}