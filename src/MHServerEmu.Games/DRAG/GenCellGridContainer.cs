using MHServerEmu.Core;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.DRAG
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

        public bool TestCoord(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool VerifyCoord(int x, int y)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   VerifyIndex(GetIndex(x, y));
        }

        public virtual bool Initialize(int x, int y, CellSetRegistry cellSetRegistry, int deadEndMax)
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

        public virtual bool DestroyableCell(int x, int y)
        {
            return VerifyCoord(x, y) && DestroyableCell(GetIndex(x, y));
        }

        public virtual bool DestroyCell(int x, int y)
        {
            return VerifyCoord(x, y) && DestroyCell(GetIndex(x, y));
        }

        public virtual bool ReserveCell(int x, int y, PrototypeId cellRef, GenCell.GenCellType genCellType)
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

        public bool ModifyNormalCell(int x, int y, Cell.Type type)
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

            GenCellConnectivityTest connectivity = new();
            if (!connectivity.TestCellConnected(this, cellA))
            {
                // Print3(PrintEnum.0);
                Logger.Trace($"x: {x} y: {y}");
                return false;
            }

            return true;
        }

        public virtual bool ReservableCell(int x, int y, PrototypeId cellRef)
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
            if ((type & externalConnections) != externalConnections) return false;
            if ((type & (externalConnections | determineType)) != type) return false;

            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.E)) return false;
            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.N)) return false;
            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.W)) return false;
            if (!TestTypeConnection(x, y, cell, determineType, type, Cell.Type.S)) return false;

            Cell.Type testType = type & ~externalConnections;
            GenCellConnectivityTest connectivity = new();
            List<GenCellConnectivityTest.GenCellConnection> list = new();

            if (!testType.HasFlag(Cell.Type.E) && TestCoord(x, y + 1))
                list.Add(new(cell, GetCell(x, y + 1)));
            if (!testType.HasFlag(Cell.Type.N) && TestCoord(x + 1, y))
                list.Add(new(cell, GetCell(x + 1, y)));
            if (!testType.HasFlag(Cell.Type.W) && TestCoord(x, y - 1))
                list.Add(new(cell, GetCell(x, y - 1)));
            if (!testType.HasFlag(Cell.Type.S) && TestCoord(x - 1, y))
                list.Add(new(cell, GetCell(x - 1, y)));

            if (connectivity.TestConnectionsRequired(this, cell, list)) return false;

            return true;
        }

        private bool TestTypeConnection(int x, int y, GenCell cell, Cell.Type determineType, Cell.Type type, Cell.Type side)
        {
            Cell.Type externalConnections = cell.ExternalConnections;
            if (externalConnections != (type & externalConnections)) return false;
            if ((side & externalConnections) != 0) return true;
            if ((determineType & side) != 0)
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

                if ((type & side) != 0)
                {
                    if (!cell.IsConnected(other)) return false;
                }
                else
                {
                    if (cell.IsConnected(other) && other.CellRef != 0) return false;
                }
            }
            return true;
        }

        public Cell.Type DetermineType(int x, int y)
        {
            Cell.Type type = Cell.Type.None;
            GenCell cell = GetCell(x, y);
            if (cell == null) return type;

            type = cell.ExternalConnections; // None |= ExternalConnections

            if (y + 1 < Height && cell.IsConnected(GetCell(x, y + 1)))  type |= Cell.Type.E;
            if (x + 1 < Width && cell.IsConnected(GetCell(x + 1, y)))   type |= Cell.Type.N;
            if (y - 1 >= 0 && cell.IsConnected(GetCell(x, y - 1)))      type |= Cell.Type.W;
            if (x - 1 >= 0 && cell.IsConnected(GetCell(x - 1, y)))      type |= Cell.Type.S;

            if (cell.HasDotCorner()) type |= cell.DotCorner; // Never true
            return type;
        }

        public bool CheckCoord(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height && VerifyIndex(GetIndex(x, y));
        }

        public void Print3()
        {
            // not used
        }


    }

}
