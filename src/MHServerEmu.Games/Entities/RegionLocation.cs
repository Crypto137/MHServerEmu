using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public struct RegionLocation
    {
        public enum SetPositionResult
        {
	        Invalid = -1,
	        Success = 0,
	        InvalidRegion = 1,
	        InvalidCell = 2,
        };

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static RegionLocation Invalid { get; }

        private Region _region;
        private Cell _cell;
        private Vector3 _position;
        private Orientation _orientation;

        public bool IsValid { get => _region != null; }

        public Region Region { get => _region; }
        public ulong RegionId { get => _region != null ? _region.Id : 0; }
        public NaviMesh NaviMesh { get => _region?.NaviMesh; }
        public Area Area { get => _cell?.Area; }
        public uint AreaId { get { Area area = Area; return area != null ? area.Id : 0; } }
        public Cell Cell { get => _cell; }
        public uint CellId { get => _cell != null ? _cell.Id : 0; }
        public Vector3 Position { get => IsValid ? _position : Vector3.Zero; }
        public Orientation Orientation { get => IsValid ? _orientation : Orientation.Zero; }

        public RegionLocation() { }

        public override string ToString()
        {
            return string.Format("rloc.pos={0}, rloc.rot={1}, rloc.region={2}, rloc.area={3}, rloc.cell={4}, rloc.entity={5}",
               _position,
               _orientation,
               _region != null ? _region : "Unknown",
               Area != null ? Area : "Unknown",
               _cell != null ? _cell : "Unknown",
               "Unknown");
        }

        public NetStructRegionLocation ToProtobuf()
        {
            ulong regionId;
            Vector3 position;

            if (_region != null)
            {
                regionId = _region.Id;
                position = Position;
            }
            else
            {
                regionId = 0;
                position = Vector3.Zero;
            }

            return NetStructRegionLocation.CreateBuilder()
                .SetRegionId(regionId)
                .SetPosition(position.ToNetStructPoint3())
                .Build();
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            if (_region != null && _region.HasKeyword(keywordProto))
                return true;

            Area area = Area;
            if (area != null && area.HasKeyword(keywordProto))
                return true;

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

        public Vector3 GetVectorFrom(ref RegionLocation other)
        {
            if (ValidateSameRegion(ref other) == false)
                return Vector3.Zero;

            return Position - other.Position;
        }

        private bool ValidateSameRegion(ref RegionLocation other)
        {
            ulong regionId = RegionId;
            return regionId != 0 && regionId == other.RegionId;
        }

        public void SetRegion(Region region)
        {
            _region = region;
            _cell = null;
        }

        public SetPositionResult SetPosition(Vector3 value)
        {
            if (Vector3.IsFinite(value) == false)
                return Logger.WarnReturn(SetPositionResult.Invalid, $"SetPosition() Non-finite position ({value}) given to region location: {this}");

            if (_region == null)
                return SetPositionResult.InvalidRegion;

            Cell oldCell = Cell;

            if (oldCell == null || oldCell.IntersectsXY(value) == false)
            {
                Cell newCell = _region.GetCellAtPosition(value);
                if (newCell == null)
                    return SetPositionResult.InvalidCell;

                _cell = newCell;
            }

            _position = value;
            return SetPositionResult.Success;
        }

        public bool SetOrientation(Orientation orientation)
        {
            if (Orientation.IsFinite(ref orientation) == false)
                return Logger.WarnReturn(false, "SetOrientation(): Orientation.IsFinite(orientation) == false");

            _orientation = orientation;
            return true;
        }

        public Vector3 ProjectToFloor()
        {
            Vector3 position = Position;
            return ProjectToFloor(_cell, ref position);
        }

        public static Vector3 ProjectToFloor(Region region, Vector3 regionPos)
        {
            if (region == null)
                return regionPos;

            Cell cell = region.GetCellAtPosition(regionPos);
            if (cell == null)
                return regionPos;

            return ProjectToFloor(cell, ref regionPos);
        }

        public static Vector3 ProjectToFloor(Region region, Cell cell, Vector3 regionPos)
        {
            if (cell != null && cell.IntersectsXY(regionPos))
                return ProjectToFloor(cell, ref regionPos);
            else
                return ProjectToFloor(region, regionPos);
        }

        public static Vector3 ProjectToFloor(Cell cell, ref Vector3 regionPos)
        {
            if (cell == null || cell.RegionBounds.IntersectsXY(regionPos) == false)
                return regionPos;

            CellPrototype cellProto = cell.Prototype;
            if (cellProto == null)
                return regionPos;

            // Use a height map if possible.
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
            {
                height = short.MinValue;
            }

            if (height > short.MinValue)
            {
                Vector3 resultPos = regionPos;
                resultPos.Z = cell.RegionBounds.Center.Z + height;
                return resultPos;
            }
            else
            {
                // Fall back to the more expensive NaviMesh projection.
                NaviMesh naviMesh = cell.Region.NaviMesh;
                if (naviMesh.IsMeshValid)
                    return naviMesh.ProjectToMesh(regionPos);
                else
                    return regionPos;
            }
        }
    }
}