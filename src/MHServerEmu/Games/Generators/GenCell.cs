using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators
{
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
                    PreventWalls = Cell.Walls.W | Cell.Walls.SW | Cell.Walls.S | Cell.Walls.SE | Cell.Walls.E | Cell.Walls.N;
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
                    PreventWalls = Cell.Walls.NW | Cell.Walls.W | Cell.Walls.SW | Cell.Walls.S | Cell.Walls.E | Cell.Walls.N;
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
            if (AtoB == BtoA) return AtoB && BtoA;
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
                    PreventWalls = ~cellProto.Walls & Cell.Walls.All;
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
