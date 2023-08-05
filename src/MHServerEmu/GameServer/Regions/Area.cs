using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Regions
{
    public class Area
    {
        public uint Id { get; }
        public AreaPrototype Prototype { get; }
        public Vector3 Origin { get; }
        public bool IsStartArea { get; }

        public List<Cell> CellList { get; } = new();

        public Area(uint id, AreaPrototype prototype, Vector3 origin, bool isStartArea)
        {
            Id = id;
            Prototype = prototype;
            Origin = origin;
            IsStartArea = isStartArea;
        }

        public void AddCell(Cell cell) => CellList.Add(cell);
    }
}
