using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Regions;
using System.Collections;

namespace MHServerEmu.Games.Generators.Areas
{
    public class GenCellGridContainer : GenCellContainer
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly List<GenCell> StartCells = new();
        private readonly List<GenCell> DestinationCells = new();

        public int GetIndex(int iX, int iY)
        {
            return Width * iY + iX;
        }

        internal GenCell GetCell(int x, int y, bool v = true)
        {
            throw new NotImplementedException();
        }

        internal bool Initialize(int cellsX, int cellsY, CellSetRegistry cellSetRegistry, int deadEndMax)
        {
            throw new NotImplementedException();
        }

        internal void ConnectAll()
        {
            throw new NotImplementedException();
        }

        internal int NumCells()
        {
            throw new NotImplementedException();
        }

        internal void DetermineCellDepthsAndShortestPath()
        {
            throw new NotImplementedException();
        }

        internal bool DestroyableCell(int x, int y)
        {
            throw new NotImplementedException();
        }

        internal void DestroyCell(int x, int y)
        {
            throw new NotImplementedException();
        }

        public bool AddStartOrDestinationCell(GenCell cell, GenCell.GenCellType type)
        {
            if (cell == null)  return false;

            switch (type)
            {
                case GenCell.GenCellType.None:
                    return false;

                case GenCell.GenCellType.Start:
                    StartCells.Add(cell);
                    return true;

                case GenCell.GenCellType.Destination:
                    DestinationCells.Add(cell);
                    return true;

                default:
                    return false;
            }

        }

        internal bool ReserveCell(int x, int y, ulong cellRef, GenCell.GenCellType genCellType)
        {
            throw new NotImplementedException();
        }

        internal bool ReservableCell(int x, int y, ulong cellRef)
        {
            throw new NotImplementedException();
        }

        public GenCellGridContainer() { }
    }

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
        public static readonly Logger Logger = LogManager.CreateLogger();
        public enum GenCellType
        {
            None,
            Start,
            Destination
        }

        public uint Id { get; set; }
        public Vector3 Position { get; set; }
        public ulong CellRef { get; set; }
        public ulong PopulationThemeOverrideRef { get; set; }

        public Cell.Type ExternalConnections { get; private set; }
        public Cell.Walls RequiredWalls { get; private set; }
        public Cell.Walls PreventWalls { get; private set; }
        public int Depth { get; private set; }
        public int DotCorner { get; private set; }
        public int Prev { get; private set; }

        private readonly Area[] ConnectedAreas = new Area[4];

        public GenCell(uint id, Vector3 position, ulong cellRef)
        {
            Id = id;
            Position = position;
            CellRef = cellRef;
        }

        public void SetExternalConnection(Cell.Type connectionType, Area connectedArea, ConnectPosition connectPosition)
        {
            ExternalConnections |= connectionType;

            switch (connectionType)
            {
                case Cell.Type.N:
                    ConnectedAreas[0] = connectedArea;
                    RequiredWalls &= ~Cell.Walls.N;
                    PreventWalls = Cell.Walls.W |Cell.Walls.SW |Cell.Walls.S |Cell.Walls.SE | Cell.Walls.E | Cell.Walls.N ;
                    if (connectPosition == ConnectPosition.Begin || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.NE;
                        PreventWalls |= Cell.Walls.NE;
                    }
                    if (connectPosition == ConnectPosition.End || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.NW;
                        PreventWalls |= Cell.Walls.NW;
                    }
                    break;

                case Cell.Type.E:
                    ConnectedAreas[1] = connectedArea;
                    RequiredWalls &= ~Cell.Walls.E;
                    PreventWalls =  Cell.Walls.NW | Cell.Walls.W |Cell.Walls.SW | Cell.Walls.S | Cell.Walls.E | Cell.Walls.N;
                    if (connectPosition == ConnectPosition.Begin || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.NE;
                        PreventWalls |= Cell.Walls.NE;
                    }
                    if (connectPosition == ConnectPosition.End || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.SE;
                        PreventWalls |= Cell.Walls.SE;
                    }
                    break;

                case Cell.Type.S:
                    ConnectedAreas[2] = connectedArea;
                    RequiredWalls &= ~Cell.Walls.S;
                    PreventWalls = Cell.Walls.NW | Cell.Walls.W | Cell.Walls.S | Cell.Walls.E | Cell.Walls.NE | Cell.Walls.N;
                    if (connectPosition == ConnectPosition.Begin || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.SE;
                        PreventWalls |= Cell.Walls.SE;
                    }
                    if (connectPosition == ConnectPosition.End || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.SW;
                        PreventWalls |= Cell.Walls.SW;
                    }
                    break;

                case Cell.Type.W:
                    ConnectedAreas[3] = connectedArea;
                    RequiredWalls &= ~Cell.Walls.W;
                    PreventWalls = Cell.Walls.W | Cell.Walls.S | Cell.Walls.SE | Cell.Walls.E | Cell.Walls.NE | Cell.Walls.N;
                    if (connectPosition == ConnectPosition.Begin || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.NW;
                        PreventWalls |= Cell.Walls.NW;
                    }
                    if (connectPosition == ConnectPosition.End || connectPosition == ConnectPosition.Inside)
                    {
                        RequiredWalls &= ~Cell.Walls.SW;
                        PreventWalls |= Cell.Walls.SW;
                    }
                    break;

                default:
                    Logger.Error("SetExternalConnection false");
                    break;
            }
        }

    }
}
