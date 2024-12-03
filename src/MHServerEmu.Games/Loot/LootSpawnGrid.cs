using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    public class LootSpawnGrid
    {
        private enum CellState
        {
            Blocked = -1,
            Unoccupied = 0,
            // Values > 0 are cells occupied by instanced loot belonging to specific players
        }

        public const int GridSize = 128;
        public const int GridCenter = GridSize / 2;
        public const float CellRadius = 32f;
        public const float CellDiameter = CellRadius * 2f;
        public const float MaxSpiralRadius = 960f;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;
        private readonly CellState[] _cells = new CellState[GridSize * GridSize];

        private Context _context;

        public LootSpawnGrid(Game game)
        {
            _game = game;
        }

        public bool SetContext(Region region, Vector3 position, WorldEntity sourceEntity = null)
        {
            if (region == null) return Logger.WarnReturn(false, "SetContext(): region == null");

            // Make sure the new context is different
            Context newContext = new(region, position, sourceEntity);
            if (newContext.Equals(_context))
                return true;

            _context = newContext;

            // Construct a new loot grid for the current context
            Array.Clear(_cells);

            List<WorldEntity> entityList = ListPool<WorldEntity>.Instance.Rent();
            _context.Region.GetEntitiesInVolume(entityList, new Sphere(_context.Position, MaxSpiralRadius), new());

            foreach (WorldEntity entity in entityList)
            {
                Bounds bounds = entity.Bounds;
                BoundsPrototype boundsProto = entity.WorldEntityPrototype?.Bounds;

                // Skip entities that can't block loot
                if (bounds.CollisionType != BoundsCollisionType.Blocking && boundsProto?.BlocksSpawns != true)
                    continue;

                // Calculate how many cells this entity occupies within our grid (at least 1)
                Vector3 entityPosition = bounds.Center;
                int cellRadiusWithinGrid = (int)Math.Ceiling(Math.Max(CellDiameter, bounds.Radius - CellDiameter) / CellDiameter);

                int xOffsetStart = 0;
                int xOffsetEnd = 0;
                int yOffsetStart = 0;
                int yOffsetEnd = 0;

                if (cellRadiusWithinGrid > 1)
                {
                    xOffsetStart = -cellRadiusWithinGrid;
                    xOffsetEnd = cellRadiusWithinGrid;
                    yOffsetStart = -cellRadiusWithinGrid;
                    yOffsetEnd = cellRadiusWithinGrid;

                    if (_context.Position.X < entityPosition.X)
                        xOffsetStart++;
                    else if (_context.Position.X > entityPosition.X)
                        xOffsetEnd--;

                    if (_context.Position.Y < entityPosition.Y)
                        yOffsetStart++;
                    else if (_context.Position.Y > entityPosition.Y)
                        yOffsetEnd--;
                }

                // Block all cells occupied by this entity
                Point2 entityGridPosition = GetGridPositionFromWorldOffset(entityPosition - _context.Position);
                for (int xOffset = xOffsetStart; xOffset <= xOffsetEnd; xOffset++)
                {
                    for (int yOffset = yOffsetStart; yOffset <= yOffsetEnd; yOffset++)
                    {
                        Point2 gridPosition = new(entityGridPosition.X + xOffset, entityGridPosition.Y + yOffset);

                        if (bounds.Geometry == GeometryType.Capsule ||
                            bounds.Geometry == GeometryType.Sphere ||
                            bounds.Contains(GetWorldPositionFromGridPosition(gridPosition)))
                        {
                            SetGridCellState(gridPosition, CellState.Blocked);
                        }
                    }
                }
            }

            ListPool<WorldEntity>.Instance.Return(entityList);
            return true;
        }

        public bool TryGetDropPosition(Vector3 offset, WorldEntityPrototype dropEntityProto, int recipientId, float height, out Vector3 dropPosition)
        {
            dropPosition = default;

            Point2 gridPosition = GetGridPositionFromWorldOffset(offset);

            // Make sure this cell is unoccipied
            CellState cellState = GetGridCellState(gridPosition);
            if (cellState == CellState.Blocked || cellState == (CellState)recipientId)
                return false;

            // Convert grid position back to world position to snap it to the grid
            dropPosition = GetWorldPositionFromGridPosition(gridPosition);

            // Make sure the position is walkable
            if (_context.Region.NaviMesh.Contains(dropPosition, CellRadius, new DefaultContainsPathFlagsCheck(PathFlags.Walk)) == false)
            {
                // Cache blocked state for future checks
                SetGridCellState(gridPosition, CellState.Blocked);
                return false;
            }

            // Snap the position to the floor using the cached value from context
            dropPosition.Z = _context.FloorHeight;

            // Do a line of sight check to make sure this position is reachable
            if (_context.Region.LineOfSightTo(_context.Position, null, dropPosition, Entity.InvalidId, 0f, 0f, height) == false)
            {
                // Cache blocked state again
                SetGridCellState(gridPosition, CellState.Blocked);
                return false;
            }

            // Mark the cell is occupied
            CellState newState = dropEntityProto.Properties[PropertyEnum.RestrictedToPlayer] ? (CellState)recipientId : CellState.Blocked;
            SetGridCellState(gridPosition, newState);

            return true;
        }

        private Point2 GetGridPositionFromWorldOffset(Vector3 offset)
        {
            int x = (int)(offset.X / CellDiameter);
            int y = (int)(offset.Y / CellDiameter);
            return new(x, y);
        }

        private Vector3 GetWorldPositionFromGridPosition(Point2 gridPosition)
        {
            float x = gridPosition.X * CellDiameter;
            float y = gridPosition.Y * CellDiameter;
            Vector3 offset = new(x, y, 0f);
            return _context.Position + offset;
        }

        private CellState GetGridCellState(Point2 gridPosition)
        {
            int index = GetGridCellIndex(gridPosition);
            return index >= 0 ? _cells[index] : CellState.Blocked;
        }

        private void SetGridCellState(Point2 gridPosition, CellState state)
        {
            int index = GetGridCellIndex(gridPosition);
            if (index < 0) return;
            _cells[index] = state;
        }

        private int GetGridCellIndex(Point2 gridPosition)
        {
            Point2 centeredPosition = new(GridCenter + gridPosition.X, GridCenter + gridPosition.Y);

            // Make sure the centered position is within the bounds of this grid
            if (centeredPosition.X < 0 || centeredPosition.X >= GridSize || centeredPosition.Y < 0 || centeredPosition.Y >= GridSize)
                return -1;

            return centeredPosition.X * GridSize + centeredPosition.Y;
        }

        private readonly struct Context : IEquatable<Context>
        {
            public readonly Region Region;
            public readonly Vector3 Position;
            public readonly WorldEntity SourceEntity;
            public readonly TimeSpan? Time;
            public readonly float FloorHeight;
        
            public Context(Region region, Vector3 position, WorldEntity sourceEntity)
            {
                Region = region;
                Position = position;
                SourceEntity = sourceEntity;

                Time = Game.Current?.CurrentTime;
                FloorHeight = RegionLocation.ProjectToFloor(region, position).Z;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Region, Position, SourceEntity, Time);
            }

            public override bool Equals(object obj)
            {
                if (obj is not Context other)
                    return false;

                return Equals(other);
            }

            public bool Equals(Context other)
            {
                // Do the cheapest and the most likeable to fail checks first
                return Time == other.Time &&
                       SourceEntity == other.SourceEntity &&
                       Region == other.Region &&
                       Segment.EpsilonTest(Position.X, other.Position.X) &&
                       Segment.EpsilonTest(Position.Y, other.Position.Y) &&
                       Segment.EpsilonTest(Position.Z, other.Position.Z);
            }
        }
    }
}
