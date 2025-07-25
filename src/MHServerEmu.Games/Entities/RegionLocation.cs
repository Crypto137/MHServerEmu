using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class RegionLocation
    {
        public enum SetPositionResult
        {
	        Invalid = -1,
	        Success = 0,
	        InvalidRegion = 1,
	        InvalidCell = 2,
        };

        private static readonly Logger Logger = LogManager.CreateLogger();

        private Region _region;
        public Region Region { get => _region; set { _region = value; Cell = null; } }
        public Cell Cell { get; set; }
        public Area Area { get => Cell?.Area; }
        public ulong RegionId { get => Region != null ? Region.Id : 0; }
        public uint AreaId { get => Area != null ? Area.Id : 0; }
        public uint CellId { get => Cell != null ? Cell.Id : 0; }
        public NaviMesh NaviMesh { get => Region?.NaviMesh; }
        public bool IsValid() => _region != null;

        private Vector3 _position;
        public Vector3 Position { get => IsValid() ? _position : Vector3.Zero; private set => _position = value; }

        private Orientation _orientation;
        public Orientation Orientation
        {
            get => IsValid() ? _orientation : Orientation.Zero;
            set
            {
                if (Orientation.IsFinite(value)) _orientation = value;
            }
        }

        public static RegionLocation Invalid = new();

        public RegionLocation(RegionLocation other)
        {
            Set(other);
        }

        public RegionLocation()
        {
            _position = Vector3.Zero;
            _orientation = Orientation.Zero;
        }

        public void Set(RegionLocation other)
        {
            _position = other._position;
            _orientation = other._orientation;
            _region = other._region;
            Cell = other.Cell;
        }

        public NetStructRegionLocation ToProtobuf()
        {
            ulong regionId = 0;
            Vector3 position = Vector3.Zero;

            if (Region != null)
            {
                regionId = Region.Id;
                position = Position;
            }

            return NetStructRegionLocation.CreateBuilder()
                .SetRegionId(regionId)
                .SetPosition(position.ToNetStructPoint3())
                .Build();
        }

        public static Vector3 ProjectToFloor(Cell cell, in Vector3 regionPos)
        {
            if (cell == null || cell.RegionBounds.IntersectsXY(regionPos) == false) return regionPos;
            var cellProto = cell.Prototype;
            if (cellProto == null) return regionPos;

            short height;
            if (cellProto.HeightMap.HeightMapData.HasValue())
            {
                Vector3 cellPos = regionPos - cell.RegionBounds.Min;
                cellPos.X /= cellProto.BoundingBox.Width;
                cellPos.Y /= cellProto.BoundingBox.Length;
                int mapX = (int)cellProto.HeightMap.HeightMapSize.X;
                int mapY = (int)cellProto.HeightMap.HeightMapSize.Y;
                int x = Math.Clamp((int)(cellPos.X * mapX), 0, mapX - 1);
                int y = Math.Clamp((int)(cellPos.Y * mapY), 0, mapY - 1);
                height = cellProto.HeightMap.HeightMapData[y * mapX + x];
            }
            else
                height = short.MinValue;

            if (height > short.MinValue)
            {
                Vector3 resultPos = regionPos;
                resultPos.Z = cell.RegionBounds.Center.Z + height;
                return resultPos;
            }
            else
            {
                var naviMesh = cell.Region.NaviMesh;
                if (naviMesh.IsMeshValid)
                    return naviMesh.ProjectToMesh(regionPos);
                else
                    return regionPos;
            }
        }

        public static Vector3 ProjectToFloor(Region region, Vector3 regionPos)
        {
            if (region == null) return regionPos;
            Cell cell = region.GetCellAtPosition(regionPos);
            if (cell == null) return regionPos;
            return ProjectToFloor(cell, regionPos);
        }

        public static Vector3 ProjectToFloor(Region region, Cell cell, Vector3 regionPos)
        {
            if (cell != null && cell.IntersectsXY(regionPos))
                return ProjectToFloor(cell, regionPos);
            else
                return ProjectToFloor(region, regionPos);
        }

        public Vector3 ProjectToFloor()
        {
            return ProjectToFloor(Cell, Position);
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

        public SetPositionResult SetPosition(Vector3 value)
        {
            if (Vector3.IsFinite(value) == false)
                return Logger.WarnReturn(SetPositionResult.Invalid, $"Non-finite position ({value}) given to region location: {ToString()}");

            if (_region == null)
                return SetPositionResult.InvalidRegion;

            Cell oldCell = Cell;

            if (oldCell == null || !oldCell.IntersectsXY(value))
            {
                Cell newCell = _region.GetCellAtPosition(value);
                if (newCell == null)
                    return SetPositionResult.InvalidCell;

                Cell = newCell;
            }

            _position = value;
            return SetPositionResult.Success;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            if (Region != null && Region.HasKeyword(keywordProto)) return true;
            Area area = Area;
            if (area != null && area.HasKeyword(keywordProto)) return true;
            return false;
        }

        public KeywordsMask GetKeywordsMask()
        {
            KeywordsMask keywordsMask = new();

            if (_region != null)
                GBitArray.Or(keywordsMask, _region.GetKeywordsMask());

            Area area = Area;
            if (area != null)
                GBitArray.Or(keywordsMask, area.GetKeywordsMask());

            return keywordsMask;
        }

        public Vector3 GetVectorFrom(RegionLocation other)
        {
            if (ValidateSameRegion(other) == false) return Vector3.Zero;
            return Position - other.Position;
        }

        private bool ValidateSameRegion(RegionLocation other)
        {
            return RegionId == other.RegionId && RegionId != 0;
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
                CellRef = cell.PrototypeDataRef;
            }
            else
            {
                CellId = 0;
                CellRef = PrototypeId.Invalid;
            }

            Position = regionLocation.Position;
            Orientation = regionLocation.Orientation;

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