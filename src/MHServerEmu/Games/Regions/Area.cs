using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Areas;

namespace MHServerEmu.Games.Regions
{
    public partial class Area
    {
        public uint Id { get; private set; }
        public AreaPrototypeId PrototypeId { get; private set; }
        public Vector3 Origin { get; set; }
        public bool IsStartArea { get; }

        public List<Cell> CellList { get; } = new();

        public Area(uint id, AreaPrototypeId prototype, Vector3 origin, bool isStartArea)
        {
            Id = id;
            PrototypeId = prototype;
            Origin = origin;
            IsStartArea = isStartArea;
        }

        public void AddCell(Cell cell) => CellList.Add(cell);

        internal void AddSubArea(Area subArea)
        {
            throw new NotImplementedException();
        }
    }
}
