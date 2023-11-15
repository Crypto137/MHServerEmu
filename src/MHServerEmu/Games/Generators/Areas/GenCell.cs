using MHServerEmu.Games.Common;
using MHServerEmu.Games.Regions;
using System.Collections;

namespace MHServerEmu.Games.Generators.Areas
{
    public class GenCellContainer : IEnumerable<GenCell>
    {
        private readonly List<GenCell> Cells = new ();
        public IEnumerator<GenCell> GetEnumerator()
        {
            return Cells.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class GenCell
    {
        public Vector3 Position { get; private set; }
        public ulong CellRef { get; private set; }

        public uint Id { get; private set; }
    }
}
