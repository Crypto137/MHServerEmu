using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using System.Collections;

namespace MHServerEmu.Games.Generators.Areas
{
    public class GenCellGridContainer : GenCellContainer
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public CellSetRegistry CellSetRegistry { get; private set; }
        public GenCellGridContainer() { }

        public int GetIndex(int x, int y)
        {
            return Width * y + x;
        }

        public GenCell GetCell(int x, int y, bool verify = true)
        {
            if (verify) { if (!VerifyCoord(x, y)) return null; }
            else if (!TestCoord(x, y)) return null;

            return GetCell(GetIndex(x, y));
        }

        private bool TestCoord(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        private bool VerifyCoord(int x, int y)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   VerifyIndex(GetIndex(x, y));
        }

        public bool Initialize(int x, int y, CellSetRegistry cellSetRegistry, int deadEndMax)
        {            
            if (cellSetRegistry == null) return false;

            CellSetRegistry = cellSetRegistry;
            Width = x;
            Height = y;

            Initialize(x * y);

            DeadEndMax = deadEndMax;
            return true;
        }

        public void ConnectAll()
        {
            GenCell cellA;
            GenCell cellB;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    bool isXConnect = x + 1 < Width;
                    bool isYConnect = y + 1 < Height;

                    if (isXConnect)
                    {
                        cellA = GetCell(x, y);
                        cellB = GetCell(x + 1, y);
                        GenCell.Connect(cellA, cellB);
                    }

                    if (isYConnect)
                    {
                        cellA = GetCell(x, y);
                        cellB = GetCell(x, y + 1);
                        GenCell.Connect(cellA, cellB);
                    }

                    if (isYConnect && isXConnect)
                    {
                        cellA = GetCell(x, y);
                        cellB = GetCell(x + 1, y + 1);
                        GenCell.Corner(cellA, cellB);
                    }

                    if (isXConnect && y - 1 >= 0)
                    {
                        cellA = GetCell(x, y);
                        cellB = GetCell(x + 1, y - 1);
                        GenCell.Corner(cellA, cellB);
                    }
                }
            }
        }

        public int DetermineCellDepthsAndShortestPath()
        {
            GenCell startCell = StartCells.FirstOrDefault();
            if (startCell == null) return int.MaxValue;

            foreach (GenCell cell in this)
                if (cell != null) cell.ClearDepthValues();

            GenCell destinationCell = DestinationCells.FirstOrDefault();
            startCell.SetDepthValues(0, null);

            int depth = 0;
            while (RunDepth(depth++))

            if (destinationCell != null)
            {
                GenCell path = destinationCell;
                while (path != null)
                {
                    path.PartOfShortestPath = true;
                    path = path.Prev;
                }

                return destinationCell.Depth;
            }

            return int.MaxValue;
        }

        private bool RunDepth(int depth)
        {
            if (depth == int.MaxValue) return false;
            bool success = false;

            foreach (var cell in this)
            {
                if (cell == null) continue;
                if (cell.Depth == depth)
                {
                    foreach (var cellConnection in cell.Connections)
                    {
                        if (cellConnection == null) continue;
                        cellConnection.SetDepthValues(cell.Depth + 1, cell);
                        success = true;
                    }

                    cell.Visited = true;
                }
            }
            return success;
        }


        public bool DestroyableCell(int x, int y)
        { 
            return VerifyCoord(x, y) && DestroyableCell(GetIndex(x, y));
        }

        public bool DestroyCell(int x, int y)
        {
            return VerifyCoord(x, y) && DestroyCell(GetIndex(x, y));
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

        public bool ReserveCell(int x, int y, ulong cellRef, GenCell.GenCellType genCellType)
        {
            if (!ReservableCell(x, y, cellRef)) return false;

            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);

            Cell.Type type = cellProto.Type;
            if (type == Cell.Type.None) type = Cell.BuildTypeFromWalls(cellProto.Walls);
            if (type == Cell.Type.None) return false;

            if (!ModifyNormalCell(x, y, type)) return false;

            GenCell cell = GetCell(x, y);
            cell.SetCellRef(cellRef);

            if (genCellType != GenCell.GenCellType.None)
                AddStartOrDestinationCell(cell, genCellType);

            return true;
        }

        private bool ModifyNormalCell(int x, int y, Cell.Type type)
        {
            GenCell cellA = GetCell(x, y);
            GenCell cellB;

            if (!type.HasFlag(Cell.Type.E) && TestCoord(x, y + 1))
            {
                cellB = GetCell(x, y + 1);
                if (GenCell.ShareConnection(cellA, cellB)) GenCell.Disconnect(cellA, cellB);
            }
            if (!type.HasFlag(Cell.Type.N) && TestCoord(x + 1, y))
            {
                cellB = GetCell(x + 1, y);
                if (GenCell.ShareConnection(cellA, cellB)) GenCell.Disconnect(cellA, cellB);
            }
            if (!type.HasFlag(Cell.Type.W) && TestCoord(x, y - 1))
            {
                cellB = GetCell(x, y - 1);
                if (GenCell.ShareConnection(cellA, cellB)) GenCell.Disconnect(cellA, cellB);
            }
            if (!type.HasFlag(Cell.Type.S) && TestCoord(x - 1, y))
            {
                cellB = GetCell(x - 1, y);
                if (GenCell.ShareConnection(cellA, cellB)) GenCell.Disconnect(cellA, cellB);
            }

            GenCellConnectivityTest test = new ();
            if (!test.TestCellConnected(this, cellA))
            {
                // Print3(PrintEnum.0);
                Logger.Trace($"x: {x} y: {y}");
                return false;
            }

            return true;
        }

        public bool ReservableCell(int x, int y, ulong cellRef)
        {
            if (!VerifyCoord(x, y)) return false;
            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cellProto == null) return false;
            return ReservableNormalCell(x, y, cellProto.Type);
        }

        private bool ReservableNormalCell(int x, int y, Cell.Type type)
        {
            GenCell cell = GetCell(x, y);
            if (cell == null) return false;

            Cell.Type determineType = DetermineType(x, y);
            Cell.Type externalConnections = cell.ExternalConnections;

            if (cell.CellRef != 0) return false;

            if (!type.HasFlag(externalConnections)) return false;

            if (!type.HasFlag(externalConnections | determineType)) return false;

            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.E)) return false;
            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.N)) return false;
            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.W)) return false;
            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.S)) return false;

            Cell.Type testType = type & ~externalConnections; 
            GenCellConnectivityTest ConnectivityTest = new ();
            List<GenCellConnectivityTest.GenCellConnection> list = new ();

            if (!testType.HasFlag(Cell.Type.E) && TestCoord(x, y + 1))
                list.Add(new (cell, GetCell(x, y + 1)));
            if (!testType.HasFlag(Cell.Type.N) && TestCoord(x + 1, y))
                list.Add(new (cell, GetCell(x + 1, y)));
            if (!testType.HasFlag(Cell.Type.W) && TestCoord(x, y - 1))
                list.Add(new (cell, GetCell(x, y - 1)));
            if (!testType.HasFlag(Cell.Type.S) && TestCoord(x - 1, y))
                list.Add(new (cell, GetCell(x - 1, y)));

            if (ConnectivityTest.TestConnectionsRequired(this, cell, list)) return false;

            return true;
        }

        private bool TestTypeConnection(int x, int y, GenCell cell, Cell.Type determineType, Cell.Type type, Cell.Type side)
        {
            Cell.Type externalConnections = cell.ExternalConnections;
            if (!type.HasFlag(externalConnections)) return false;
            if (side.HasFlag(externalConnections)) return true;
            if (determineType.HasFlag(side))
            {
                GenCell other;
                switch (side)
                {
                    case Cell.Type.E:
                        if (y + 1 >= Height) return false;
                        other = GetCell(x, y + 1);
                        break;
                    case Cell.Type.N:
                        if (x + 1 >= Width) return false;
                        other = GetCell(x + 1, y);
                        break;
                    case Cell.Type.W:
                        if (y - 1 < 0) return false;
                        other = GetCell(x, y - 1);
                        break;
                    case Cell.Type.S:
                        if (x - 1 < 0) return false;
                        other = GetCell(x - 1, y);
                        break;
                    default:
                        Logger.Error("TestTypeConnection false");
                        return false;
                }

                if (type.HasFlag(side))
                    if (!cell.IsConnected(other)) return false;
                else
                    if (cell.IsConnected(other) && (other.CellRef != 0)) return false;
            }
            return true;
        }

        private Cell.Type DetermineType(int x, int y)
        {
            Cell.Type type = Cell.Type.None;
            GenCell cell = GetCell(x, y);
            if (cell == null) return type;

            if (cell.ExternalConnections != Cell.Type.None) type |= cell.ExternalConnections;

            if ((y + 1 < Height) && cell.IsConnected(GetCell(x, y + 1)))  type |= Cell.Type.E;
            if ((x + 1 < Width) && cell.IsConnected(GetCell(x + 1, y))) type |= Cell.Type.N;
            if ((y - 1 >= 0) && cell.IsConnected(GetCell(x, y - 1))) type |= Cell.Type.W;
            if ((x - 1 >= 0) && cell.IsConnected(GetCell(x - 1, y))) type |= Cell.Type.S;

            if (cell.HasDotCorner()) type |= cell.DotCorner; // always None
            return type;
        }

    }
    public class GenCellConnectivityTest
    {
        public class GenCellConnection
        {
            private readonly GenCell _cellA;
            private readonly GenCell _cellB;

            public GenCellConnection(GenCell cellA, GenCell cellB)
            {
                _cellA = cellA;
                _cellB = cellB;
            }

            public bool Test(GenCell cellA, GenCell cellB)
            {
                return (cellA == _cellA && cellB == _cellB) || (cellB == _cellA && cellA == _cellB);
            }
        }

        private readonly Dictionary<GenCell, bool> _connectivity = new();

        public GenCellConnectivityTest(){}

        public bool TestConnectionsRequired(GenCellContainer container, GenCell cell, List<GenCellConnection> list)
        {
            Reset(container);
            RunTreeWithExcludedConnections(cell, list);

            foreach (var item in _connectivity)
                if (!item.Value) return true;

            return false;
        }

        private void RunTreeWithExcludedConnections(GenCell cell, List<GenCellConnection> list)
        {
            if (cell == null) return;
            foreach (GenCell connection in cell.Connections)
            {
                if (connection == null) continue;
                if (!IsConnectionInList(list, cell, connection) && !_connectivity[connection])
                {
                    _connectivity[connection] = true;
                    RunTreeWithExcludedConnections(connection, list);
                }
            }
        }

        public static bool IsConnectionInList(List<GenCellConnection> list, GenCell cell, GenCell cellConnection)
        {
            foreach (GenCellConnection connection in list)
                if (connection.Test(cell, cellConnection)) return true;

            return false;
        }

        private void Reset(GenCellContainer container)
        {
            _connectivity.Clear();
            foreach (GenCell cell in container)
                if (cell != null) _connectivity[cell] = false;
        }

        public bool TestCellConnected(GenCellContainer container, GenCell cell)
        {
            if (cell.Connections.First() == cell.Connections.Last()) return false;

            Reset(container);
            RunTreeWithExcludedCell(cell, null);

            foreach (var item in _connectivity)
                if (!item.Value) return false;

            return true;
        }

        private void RunTreeWithExcludedCell(GenCell cell, GenCell excludedCell)
        {
            if (cell == null) return; // Internal Generation Error
            foreach (GenCell connection in cell.Connections)
            {
                if (connection == excludedCell) continue;
                if (!_connectivity[connection])
                {
                    _connectivity[connection] = true;
                    RunTreeWithExcludedCell(connection, excludedCell);
                }
            }
        }

        public bool TestCellRequired(GenCellContainer container, GenCell requiredCell)
        {
            if (requiredCell == null) return false;
            Reset(container);

            foreach (GenCell connection in requiredCell.Connections)
            {
                if (connection == null) continue;
                RunTreeWithExcludedCell(connection, requiredCell);
            }

            foreach (var item in _connectivity)
                if (!item.Value && item.Key != requiredCell) return true;

            return false;
        }

    }

    public class GenCellContainer : IEnumerable<GenCell>
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        public List<GenCell> StartCells = new();
        public List<GenCell> DestinationCells = new();
        public int DeadEndMax { get; set; }

        public int NumCells { get; private set; }

        private readonly List<GenCell> Cells = new ();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<GenCell> GetEnumerator() => Cells.GetEnumerator();

        public bool CreateCell(uint id, Vector3 position, ulong cellRef)
        {
            GenCell cell = new(id, position, cellRef);
            Cells.Add(cell);
            ++NumCells;
            return true;
        }

        public bool Initialize(int size = 0)
        {
            DestroyAllCells();
            for (int i = 0; i < size; i++)
            {
                Cells.Add(new());
                ++NumCells;
            }
            return true;
        }

        private void DestroyAllCells()
        {
            for (int i = 0; i < Cells.Count; i++)
                DestroyCell(i);

            Cells.Clear();
            StartCells.Clear();
            DestinationCells.Clear();
            NumCells = 0;
        }

        public bool DestroyCell(int index)
        {
            if (index < Cells.Count)
            {
                DestroyCell(Cells[index]);
                Cells[index] = null;
                return true;
            }
            return false;
        }

        private bool DestroyCell(GenCell cell)
        {
            if (cell != null)
            {
                cell.DisconnectFromAll();
                --NumCells;
                return true;
            }
            return false;
        }

        public GenCell GetCell(int index)
        {
            if (index < Cells.Count) return Cells[index];
            return null;
        }

        public bool VerifyIndex(int index)
        {
            return index < Cells.Count; 
        }

        public bool DestroyableCell(int index)
        {
            if (index < Cells.Count)
            {
                GenCell cell = Cells[index];
                if (cell != null) return DestroyableCell(cell);
            }
            return false;
        }

        private bool DestroyableCell(GenCell cell)
        {
            if (cell == null) return false;
            if (cell.CellRef != 0) return false;
            if (cell.ExternalConnections != Cell.Type.None) return false;

            foreach (var connection in cell.Connections)
                if (connection != null && connection.CellRef != 0) return false;

            if (!CheckForConnectivity(cell)) return false;

            GenCellConnectivityTest test = new ();
            return !test.TestCellRequired(this, cell);
        }

        private bool CheckForConnectivity(GenCell checkedCell)
        {
            if (DeadEndMax > 0)
            {
                foreach (GenCell cell in Cells)
                {
                    if (cell == null) continue;
                    int connected = 0;
                    foreach (GenCell connection in cell.Connections)
                        if (connection != checkedCell) connected++;

                    if (cell != null && connected == 1 
                        && !CheckForConnectivityPerCell(cell, 1, DeadEndMax, null, checkedCell)) return false;
                }
            }
            return true;
        }

        private bool CheckForConnectivityPerCell(GenCell cell, int level, int maxlevel, GenCell prev, GenCell checkedCell)
        {
            if (cell != null)
            {
                if (level > maxlevel) return false;

                int connections = 0;
                foreach (GenCell connection in cell.Connections)
                    if (connection != checkedCell) connections++;

                if (connections >= 3)  return true;

                foreach (GenCell startCell in StartCells)
                    if (cell == startCell) return true;

                foreach (GenCell destinationCell in DestinationCells)
                    if (cell == destinationCell)  return true;


                bool check = false;
                foreach (GenCell connection in cell.Connections)
                {
                    if (connection == prev || connection == checkedCell)  continue;
                    check |= CheckForConnectivityPerCell(connection, level++, maxlevel, cell, checkedCell);
                }

                return check;
            }
            return false;
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
        public Cell.Type DotCorner { get; private set; }
        public GenCell Prev { get; private set; }
        public bool PartOfShortestPath { get; set; }
        public bool Visited { get; set; }

        public readonly List<GenCell> Connections = new();

        private readonly Area[] ConnectedAreas = new Area[4];
        private readonly List<GenCell> Corners = new();

        public GenCell() { Position = new(); }
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

        public void DisconnectFromAll()
        {
            foreach (GenCell cell in Connections)
                if (cell != null) cell.DisconnectFrom(this);
            Connections.Clear();
        }

        public void DisconnectFrom(GenCell cell)
        {
            Connections.Remove(cell);
        }

        public static void Connect(GenCell cellA, GenCell cellB)
        {
            cellA.ConnectTo(cellB);
            cellB.ConnectTo(cellA);
        }

        private void ConnectTo(GenCell cell)
        {
           if (!IsConnected(cell)) Connections.Add(cell);
        }

        public bool IsConnected(GenCell cell)
        {
            return cell != null && Connections.Contains(cell);
        }

        public static void Corner(GenCell cellA, GenCell cellB)
        {
            cellA.CornerTo(cellB);
            cellB.CornerTo(cellA);
        }

        private void CornerTo(GenCell cell)
        {
            if (!IsCorner(cell)) Corners.Add(cell);
        }

        private bool IsCorner(GenCell cell)
        {
            return cell != null && Corners.Contains(cell);
        }

        public static bool ShareConnection(GenCell cellA, GenCell cellB)
        {
            bool AtoB = cellA.IsConnected(cellB);
            bool BtoA = cellB.IsConnected(cellA);
            if (AtoB == BtoA) return (AtoB && BtoA);
            return false;
        }

        public static void Disconnect(GenCell cellA, GenCell cellB)
        {
            if (cellA != null && cellB != null
                && cellA.IsConnected(cellB) && cellB.IsConnected(cellA))
            {
                cellA.DisconnectFrom(cellB);
                cellB.DisconnectFrom(cellA);
            }
        }

        public void SetDepthValues(int depth, GenCell prev)
        {
            if (depth < Depth)
            {
                Depth = Math.Min(Depth, depth);
                Prev = prev;
            }
        }

        public void ClearDepthValues()
        {
            Visited = false;
            PartOfShortestPath = false;
            Depth = int.MaxValue; 
            Prev = null;
        }

        public void SetCellRef(ulong cellRef)
        {
            if (cellRef != 0)
            {
                CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
                if (cellProto != null)
                {
                    PreventWalls = (~cellProto.Walls) & Cell.Walls.All;
                    RequiredWalls = cellProto.Walls;
                }
            }
            CellRef = cellRef;
        }

        public bool HasDotCorner()
        {
            return (DotCorner & Cell.Type.DotMask) != Cell.Type.None;
        }

    }
}
