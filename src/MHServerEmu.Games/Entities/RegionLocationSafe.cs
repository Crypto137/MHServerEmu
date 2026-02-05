using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public struct RegionLocationSafe
    {
        public ulong RegionId { get; private set; }
        public PrototypeId RegionRef { get; private set; }
        public uint AreaId { get; private set; }
        public PrototypeId AreaRef { get; private set; }
        public uint CellId { get; private set; }
        public PrototypeId CellRef { get; private set; }
        public Vector3 Position { get; private set; }
        public Orientation Orientation { get; private set; }

        public Area GetArea()
        {
            if (AreaId == 0)
                return null;

            Region region = GetRegion();
            Area area = region?.GetAreaById(AreaId);
            return area;
        }

        public Region GetRegion()
        {
            if (RegionId == 0)
                return null;

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
            if (region != null && region.HasKeyword(keywordProto))
                return true;

            Area area = GetArea();
            if (area != null && area.HasKeyword(keywordProto))
                return true;

            return false;
        }
    }
}
