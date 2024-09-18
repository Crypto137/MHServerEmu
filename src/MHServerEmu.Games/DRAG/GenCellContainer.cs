using System.Collections;
using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.DRAG
{
    public class GenCellConnectivityTest
    {
        public class GenCellConnection
        {
            private readonly GenCell _origin;
            private readonly GenCell _target;

            public GenCellConnection(GenCell origin, GenCell target)
            {
                _origin = origin;
                _target = target;
            }

            public bool Test(GenCell cellA, GenCell cellB)
            {
                return cellA == _origin && cellB == _target || cellB == _origin && cellA == _target;
            }
        }

        private readonly Dictionary<GenCell, bool> _connectivity = new();

        public GenCellConnectivityTest() { }

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

        private void RunTreeWithExcludedConnection(GenCell cell, GenCell origin, GenCell target)
        {
            if (cell == null || origin == null || target == null) return;

            List<GenCellConnection> list = new()
            {
                new (origin, target)
            };
            RunTreeWithExcludedConnections(cell, list);
        }

        private static bool IsConnectionInList(List<GenCellConnection> list, GenCell cell, GenCell cellConnection)
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
            if (cell.Connections.Any() == false) return false;

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

            var connection = requiredCell.Connections.FirstOrDefault();
            if (connection == null) return false;
            RunTreeWithExcludedCell(connection, requiredCell);

            foreach (var item in _connectivity)
                if (!item.Value && item.Key != requiredCell) return true;

            return false;
        }

        public bool TestConnectionRequired(GenCellContainer container, GenCell cellA, GenCell cellB)
        {
            Reset(container);

            if (cellB.CellRef != 0 || cellA.CellRef != 0) return true;

            RunTreeWithExcludedConnection(cellA, cellA, cellB);

            foreach (var item in _connectivity)
                if (!item.Value) return true;

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

        public readonly List<GenCell> Cells = new();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<GenCell> GetEnumerator() => Cells.GetEnumerator();

        public bool CreateCell(uint id, Vector3 position, PrototypeId cellRef)
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
                ++NumCells; // From CreateCell(Index)
            }
            return true;
        }

        public bool AddStartOrDestinationCell(GenCell cell, GenCell.GenCellType type)
        {
            if (cell == null) return false;

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

        public virtual bool DestroyUnrequiredConnections(GenCell cell, GRandom random, int chance)
        {
            if (cell == null) return false;

            GenCellConnectivityTest connectivity = new();

            foreach (GenCell connection in cell.Connections)
            {
                if (connection != null)
                {
                    if (!CheckForConnectivity(cell, connection)
                        && !connectivity.TestConnectionRequired(this, cell, connection)
                        && random.NextPct(chance))
                    {
                        cell.DisconnectFrom(connection);
                        connection.DisconnectFrom(cell);
                    }
                }
            }

            return true;
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

        public virtual bool DestroyableCell(GenCell cell)
        {
            if (cell == null || cell.CellRef != 0 || cell.ExternalConnections != Cell.Type.None) return false;

            foreach (var connection in cell.Connections)
                if (connection != null && connection.CellRef != 0) return false;

            if (!CheckForConnectivity(cell)) return false;

            GenCellConnectivityTest test = new();
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

        private bool CheckForConnectivity(GenCell cellA, GenCell cellB)
        {
            if (DeadEndMax > 0)
            {
                foreach (GenCell cell in Cells)
                {
                    if (cell == null) continue;

                    int connections = 0;
                    foreach (GenCell connectedCell in cell.Connections)
                    {
                        if (!(connectedCell == cellA && cell == cellB || connectedCell == cellB && cell == cellA))
                            connections++;
                    }

                    if (connections == 1)
                    {
                        if (!CheckForConnectivityPerCell(cell, 1, DeadEndMax, null, cellA, cellB))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool CheckForConnectivityPerCell(GenCell cell, int level, int maxlevel, GenCell prev, GenCell cellA, GenCell cellB)
        {
            if (cell != null)
            {
                if (level > maxlevel) return false;

                int connections = 0;
                foreach (GenCell connection in cell.Connections)
                {
                    if (!(connection == cellA && cell == cellB
                        || connection == cellB && cell == cellA))
                        connections++;
                }

                if (connections >= 3) return true;

                if (StartCells.Contains(cell) || DestinationCells.Contains(cell)) return true;

                bool check = false;
                foreach (GenCell connection in cell.Connections)
                {
                    if (connection == prev
                        || connection == cellA && cell == cellB
                        || connection == cellB && cell == cellA)
                        continue;

                    check |= CheckForConnectivityPerCell(connection, level++, maxlevel, cell, cellA, cellB);
                }

                return check;
            }

            return false;
        }

        private bool CheckForConnectivityPerCell(GenCell cell, int level, int maxlevel, GenCell prev, GenCell checkedCell)
        {
            if (cell != null)
            {
                if (level > maxlevel) return false;

                int connections = 0;
                foreach (GenCell connection in cell.Connections)
                    if (connection != checkedCell) connections++;

                if (connections >= 3) return true;

                if (StartCells.Contains(cell) || DestinationCells.Contains(cell)) return true;

                bool check = false;
                foreach (GenCell connection in cell.Connections)
                {
                    if (connection == prev || connection == checkedCell) continue;
                    check |= CheckForConnectivityPerCell(connection, level++, maxlevel, cell, checkedCell);
                }

                return check;
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new ("[");
            foreach (GenCell cell in Cells)
            {
                if (cell!= null)
                    sb.Append((int)cell.PreventWalls).Append(", ");
                else
                    sb.Append("N, ");
            }
            if (Cells.Count > 0)
                sb.Length -= 2; 
            sb.Append("]");
            return sb.ToString();
        }

    }

}
