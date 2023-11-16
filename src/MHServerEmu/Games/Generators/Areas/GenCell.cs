using MHServerEmu.Games.Common;
using System.Collections;

namespace MHServerEmu.Games.Generators.Areas
{
    public class GenCellContainer : IEnumerable<GenCell>
    {
        private readonly List<GenCell> Cells = new ();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<GenCell> GetEnumerator() => Cells.GetEnumerator();

        public bool CreateCell(uint id, Vector3 position, ulong cellRef)
        {
            GenCell cell = new(id, position, cellRef);
            Cells.Add(cell);
            return true;
        }

        public void Initialize(uint id = 0)
        {
           // TODO: Cells.Resize(id)
        }
     
    }

    public class GenCell
    {
        public uint Id { get; set; }
        public Vector3 Position { get; set; }
        public ulong CellRef { get; set; }

        public GenCell(uint id, Vector3 position, ulong cellRef)
        {
            Id = id;
            Position = position;
            CellRef = cellRef;
        }
    }
}
