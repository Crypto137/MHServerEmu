using System.Reflection;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using static MHServerEmu.Games.DRAG.Generators.Areas.CellGridGenerator;
using static MHServerEmu.Games.Regions.Cell;
using MHServerEmu.Games.DRAG.Generators.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Areas
{
    public class WideGridAreaGenerator : BaseGridAreaGenerator
    {
        public override bool Initialize(Area area)
        {
            CellContainer = new WideGenCellGridContainer();
            return base.Initialize(area);
        }

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<PrototypeId> areas)
        {
            if (Area.AreaPrototype.Generator is not WideGridAreaGeneratorPrototype proto) return false;

            Region region = Region;
            if (region == null) return false;

            bool success = false;
            int tries = 10;

            while (!success && --tries > 0)
            {
                success = InitializeContainer()
                    && EstablishExternalConnections()
                    && GenerateRandomInstanceLinks(random)
                    && CreateRequiredCells(random, regionGenerator, areas)
                    && GenerateRoads(random, proto.Roads);
            }

            if (!success)
            {
                if (Log) Logger.Warn($"WideGridAreaGenerator failed after {10 - tries} attempts | region: {Region} | area: {Area}");
                return false;
            }

            if (proto.ProceduralSuperCells)
            {
                if (!CreateProceduralSuperCells(random))
                    if (Log) Logger.Error("CreateProceduralSuperCells false");
            }

            ProcessDeleteExtraneousCells(random, (int)proto.RoomKillChancePct);
            ProcessRegionConnectionsAndDepth();
            ProcessAssignUniqueCellIds();
            ProcessCellPositions(proto.CellSize);

            return ProcessCellTypes(random);
        }

        bool CreateProceduralSuperCells(GRandom random)
        {
            if (CellContainer is not WideGenCellGridContainer container) return false;
            if (LogDebug) Logger.Debug($"[{MethodBase.GetCurrentMethod().Name}] => {random}");
            List<PatternHit> hits = new();
            Walls[] patternWide = {
                (Walls) WallGroup.WideNES, (Walls) WallGroup.WideNES,
                Walls.None, Walls.None
            };

            Walls[] newPatternWide = {
                (Walls) WallGroup.WideES,  (Walls) WallGroup.WideNE,
                Walls.NW, Walls.SW
            };

            Square patternWideBounds = new(new(0, 0), new(1, 1));
            FindWallPattern(container, patternWide, newPatternWide, patternWideBounds, hits);

            Walls[] pattern = {
                Walls.None, Walls.None,
                Walls.None, Walls.None
            };

            Walls[] newPattern = {
                Walls.NE, Walls.SE,
                Walls.NW, Walls.SW
            };

            Square patternBounds = new(new(0, 0), new(1, 1));
            FindWallPattern(container, pattern, newPattern, patternBounds, hits);

            int count = hits.Count;
            while (count > 0)
            {
                int randomIndex = random.Next(0, count);
                PatternHit hit = hits[randomIndex];

                Cell.Type direction = FindPatternAtPoint(container, hit.X, hit.Y, hit.Pattern, hit.NewPattern, hit.Bounds);
                if (direction.HasFlag((Cell.Type)(1 << hit.Rotation)))
                {
                    for (int hx = 0; hx < hit.Bounds.Height; hx++)
                    {
                        for (int hy = 0; hy < hit.Bounds.Width; hy++)
                        {
                            int x = hit.X;
                            int y = hit.Y;
                            GenCell cell = null;

                            switch (hit.Rotation)
                            {
                                case 0:
                                    x += hy;
                                    y += hx;
                                    cell = container.GetCell(x, y);
                                    break;
                                case 1:
                                    x += hit.Bounds.Height - 1 - hx;
                                    y += hy;
                                    cell = container.GetCell(x, y);
                                    break;
                                case 2:
                                    x += hit.Bounds.Width - 1 - hy;
                                    y += hit.Bounds.Height - 1 - hx;
                                    cell = container.GetCell(x, y);
                                    break;
                                case 3:
                                    x += hx;
                                    y += hit.Bounds.Width - 1 - hy;
                                    cell = container.GetCell(x, y);
                                    break;
                            }

                            if (cell != null)
                            {
                                Walls wallsPattern = GetPointInPattern(hit.NewPattern, hy, hx, hit.Bounds, hit.Rotation);
                                if (container.ReservableWideCell(x, y, wallsPattern))
                                {
                                    container.ModifyNormalCell(x, y, BuildTypeFromWalls(wallsPattern));
                                    container.ModifyWideCell(x, y, wallsPattern);
                                }
                            }
                        }
                    }
                }

                hits[randomIndex] = hits[count - 1];
                count--;
            }

            return true;
        }

        private static Walls GetPointInPattern(Walls[] pattern, int px, int py, in Square bounds, int rotation)
        {
            if (rotation < 0 || rotation >= 4) return 0;
            return WallsRotate(pattern[py * bounds.Width + px], rotation * 2);
        }

        private static Cell.Type FindPatternAtPoint(GenCellGridContainer container, int x, int y, Walls[] pattern, Walls[] newPattern, in Square bounds)
        {
            Cell.Type type = Cell.Type.NESW;

            for (int py = 0; py < bounds.Height; py++)
            {
                for (int px = 0; px < bounds.Width; px++)
                {
                    for (int rotation = 0; rotation < 4; rotation++)
                    {
                        if (type.HasFlag((Cell.Type)(1 << rotation))) // BitTest
                        {
                            int cx, cy;
                            switch (rotation)
                            {
                                case 0:
                                    cx = x + px;
                                    cy = y + py;
                                    break;
                                case 1:
                                    cx = x + (bounds.Height - 1 - py);
                                    cy = y + px;
                                    break;
                                case 2:
                                    cx = x + (bounds.Width - 1 - px);
                                    cy = y + (bounds.Height - 1 - py);
                                    break;
                                case 3:
                                    cx = x + py;
                                    cy = y + (bounds.Width - 1 - px);
                                    break;
                                default:
                                    continue;
                            }

                            if (!CheckPoint(container, cx, cy, pattern, newPattern, px, py, bounds, rotation))
                            {
                                type &= ~(Cell.Type)(1 << rotation); // BitClear
                            }
                        }
                    }
                }
            }

            return type & Cell.Type.NESW;
        }

        private static bool CheckPoint(GenCellGridContainer container, int cx, int cy, Walls[] pattern, Walls[] newPattern, int px, int py, in Square bounds, int rotation)
        {
            if (rotation < 0 || rotation >= 4 || pattern == null || newPattern == null) return false;
            GenCell cell = container.GetCell(cx, cy, false);

            if (cell != null && cell.CellRef == 0)
            {
                Walls walls = GetPointInPattern(pattern, px, py, bounds, rotation);
                Walls newWalls = GetPointInPattern(newPattern, px, py, bounds, rotation);
                Walls requiredWalls = cell.RequiredWalls;
                Walls preventWalls = cell.PreventWalls;

                if (walls == requiredWalls && (newWalls & preventWalls) == 0) return true;
            }

            return false;
        }

        private static bool FindWallPattern(GenCellGridContainer container, Walls[] pattern, Walls[] newPattern, in Square bounds, List<PatternHit> hits)
        {
            for (int y = 0; y < container.Height; y++)
            {
                for (int x = 0; x < container.Width; x++)
                {
                    Cell.Type direction = FindPatternAtPoint(container, x, y, pattern, newPattern, bounds);
                    if (direction > 0)
                    {
                        for (int rotation = 0; rotation < 4; rotation++)
                        {
                            if (direction.HasFlag((Cell.Type)(1 << rotation)))
                            {
                                PatternHit hit = new()
                                {
                                    X = x,
                                    Y = y,
                                    Rotation = rotation,
                                    Pattern = pattern,
                                    NewPattern = newPattern,
                                    Bounds = bounds
                                };

                                hits.Add(hit);
                            }
                        }
                    }
                }
            }
            return hits.Any();
        }

        private bool ProcessCellTypes(GRandom random)
        {
            if (CellContainer == null) return false;
            if (Area.AreaPrototype.Generator is not WideGridAreaGeneratorPrototype proto) return false;

            int randomSeed = Area.RandomSeed;

            if (!BuildCellDeterminationMap(out CellDeterminationMap cellMap)) return false;
            if (LogDebug) Logger.Debug($"[{MethodBase.GetCurrentMethod().Name}] => {random}");
            foreach (var item in cellMap)
            {
                List<Point2> coordsOfType = item.Value;
                if (coordsOfType == null) continue;

                Picker<Point2> picker = new(random);
                foreach (Point2 coords in coordsOfType)
                    picker.Add(coords);

                while (!picker.Empty() && picker.PickRemove(out Point2 point))
                {
                    GenCell genCell = CellContainer.GetCell(point.X, point.Y);
                    if (genCell != null)
                    {
                        List<uint> connectedCells = new();
                        CreateConnectedCellList(genCell, connectedCells);

                        if (genCell.CellRef != 0)
                        {
                            CellSettings cellSettings = new()
                            {
                                CellRef = genCell.CellRef,
                                PositionInArea = genCell.Position,
                                Seed = ++randomSeed,
                                ConnectedCells = connectedCells,
                                PopulationThemeOverrideRef = genCell.PopulationThemeOverrideRef
                            };

                            Area.AddCell(genCell.Id, cellSettings); // Verify
                            continue;
                        }
                        else
                        {
                            Walls wallType = genCell.RequiredWalls;
                            PrototypeId cellRef = 0;

                            List<PrototypeId> excludedList = new();

                            if (CellSetRegistry.HasCellWithWalls(wallType))
                            {
                                cellRef = CellSetRegistry.GetCellSetAssetPickedByWall(random, wallType, excludedList);
                            }

                            if (cellRef == 0)
                            {
                                if (Log) Logger.Trace($"Generator for Area {Area} tried to pick cell of type {wallType}, none were available. Region: {Region}");
                                return false;
                            }

                            CellSettings cellSettings = new()
                            {
                                CellRef = cellRef,
                                PositionInArea = genCell.Position,
                                Seed = ++randomSeed,
                                ConnectedCells = connectedCells,
                                PopulationThemeOverrideRef = genCell.PopulationThemeOverrideRef
                            };

                            Area.AddCell(genCell.Id, cellSettings); // Verify
                            genCell.SetCellRef(cellRef);
                        }
                    }
                    else
                    {
                        PlaceFillerRoom(random, new(point.X * proto.CellSize, point.Y * proto.CellSize, 0.0f));
                    }
                }
            }

            CleanCellDeterminationMap(ref cellMap);

            return true;
        }

        private bool BuildCellDeterminationMap(out CellDeterminationMap cellMap)
        {
            cellMap = new();
            for (int y = 0; y < CellContainer.Height; y++)
            {
                for (int x = 0; x < CellContainer.Width; x++)
                {
                    GenCell cell = CellContainer.GetCell(x, y);
                    Walls wallType = Walls.None;
                    if (cell != null) wallType = cell.RequiredWalls;

                    if (cellMap.TryGetValue((int)wallType, out List<Point2> pointList))
                    {
                        pointList.Add(new(x, y));
                    }
                    else
                    {
                        pointList = new() { new(x, y) };
                        cellMap[(int)wallType] = pointList;
                    }
                }
            }
            return true;
        }

        private bool PlaceFillerRoom(GRandom random, Vector3 position)
        {
            if (CellSetRegistry.HasCellWithWalls(Walls.All))
            {
                PrototypeId cellRef = CellSetRegistry.GetCellSetAssetPickedByWall(random, Walls.All);
                CellSettings cellSettings = new()
                {
                    CellRef = cellRef,
                    PositionInArea = position
                };
                return Area.AddCell(AllocateCellId(), cellSettings) != null;
            }
            return false;
        }

        public static bool CellGridBorderBehavior(Area area)
        {
            if (area == null) return false;

            GeneratorPrototype generatorProto = area.AreaPrototype.Generator;
            WideGridAreaGeneratorPrototype gridAreaGeneratorProto = generatorProto as WideGridAreaGeneratorPrototype;

            if (gridAreaGeneratorProto != null && gridAreaGeneratorProto.BorderBehavior != null && gridAreaGeneratorProto.CellSets.HasValue())
            {
                CellSetRegistry registry = new();
                registry.Initialize(true, area.Log);
                foreach (CellSetEntryPrototype cellSetEntry in gridAreaGeneratorProto.CellSets)
                {
                    if (cellSetEntry == null) continue;
                    registry.LoadDirectory(cellSetEntry.CellSet, cellSetEntry, cellSetEntry.Weight, cellSetEntry.Unique);
                }

                return DoBorderBehavior(area, gridAreaGeneratorProto.BorderBehavior.BorderWidth, registry, gridAreaGeneratorProto.CellSize, gridAreaGeneratorProto.CellsX, gridAreaGeneratorProto.CellsY);
            }

            return true;
        }
    }

    public class PatternHit
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Rotation { get; set; }
        public Walls[] Pattern { get; set; }
        public Walls[] NewPattern { get; set; }
        public Square Bounds { get; set; }
    }

    public class WideGenCellGridContainer : GenCellGridContainer
    {
        public enum Dir
        {
            N, NE, E, ES, S, SW, W, NW
        }

        private readonly int[,] _offsets = {
            { 1, 0 },  // N
            { 1, 1 },  // NE
            { 0, 1 },  // E
            { -1, 1 }, // SE
            { -1, 0 }, // S
            { -1, -1 },// SW
            { 0, -1 }, // W
            { 1, -1 }  // NW
        };

        private static readonly Dictionary<Cell.Type, Dir> _typeToIndex = new()
        {
            { Cell.Type.N,  Dir.N },
            { Cell.Type.NE, Dir.NE },
            { Cell.Type.E,  Dir.E },
            { Cell.Type.ES, Dir.ES },
            { Cell.Type.S,  Dir.S },
            { Cell.Type.SW, Dir.SW },
            { Cell.Type.W,  Dir.W },
            { Cell.Type.NW, Dir.NW }
        };

        private static readonly Walls[] _wallMasks = new Walls[]
        {
            Walls.SW | Walls.S | Walls.SE,
            Walls.SW,
            Walls.SW | Walls.W | Walls.NW,
            Walls.NW,
            Walls.NW | Walls.N | Walls.NE,
            Walls.NE,
            Walls.NE | Walls.E | Walls.SE,
            Walls.SE
        };

        private static Walls GetWallMask(Walls walls, Dir dir)
        {
            switch (dir)
            {
                case Dir.N:
                    return (walls.HasFlag(Walls.NW) ? Walls.SW : Walls.None) |
                                    (walls.HasFlag(Walls.N) ? Walls.S : Walls.None) |
                                    (walls.HasFlag(Walls.NE) ? Walls.SE : Walls.None);
                case Dir.NE: return walls.HasFlag(Walls.NE) ? Walls.SW : Walls.None;
                case Dir.E:
                    return (walls.HasFlag(Walls.SE) ? Walls.SW : Walls.None) |
                                    (walls.HasFlag(Walls.E) ? Walls.W : Walls.None) |
                                    (walls.HasFlag(Walls.NE) ? Walls.NW : Walls.None);
                case Dir.ES: return walls.HasFlag(Walls.SE) ? Walls.NW : Walls.None;
                case Dir.S:
                    return (walls.HasFlag(Walls.SW) ? Walls.NW : Walls.None) |
                                    (walls.HasFlag(Walls.S) ? Walls.N : Walls.None) |
                                    (walls.HasFlag(Walls.SE) ? Walls.NE : Walls.None);
                case Dir.SW: return walls.HasFlag(Walls.SW) ? Walls.NE : Walls.None;
                case Dir.W:
                    return (walls.HasFlag(Walls.NW) ? Walls.NE : Walls.None) |
                                    (walls.HasFlag(Walls.W) ? Walls.E : Walls.None) |
                                    (walls.HasFlag(Walls.SW) ? Walls.SE : Walls.None);
                case Dir.NW: return walls.HasFlag(Walls.NW) ? Walls.SE : Walls.None;
                default: return Walls.None;
            }
        }

        public override bool Initialize(int iX, int iY, CellSetRegistry registry, int deadEndMax)
        {
            if (!base.Initialize(iX, iY, registry, deadEndMax)) return false;

            for (int x = 0; x < iX; ++x)
            {
                GetCell(x, iY - 1).MaskRequiredWalls(_wallMasks[(int)Dir.W]);
                GetCell(x, 0).MaskRequiredWalls(_wallMasks[(int)Dir.E]);
            }

            for (int y = 0; y < iY; ++y)
            {
                GetCell(iX - 1, y).MaskRequiredWalls(_wallMasks[(int)Dir.S]);
                GetCell(0, y).MaskRequiredWalls(_wallMasks[(int)Dir.N]);
            }

            return true;
        }

        public bool ModifyWideCell(int x, int y, Walls walls)
        {
            GenCell cell = GetCell(x, y);
            cell.MaskRequiredWalls(walls);

            for (int i = 0; i < _offsets.GetLength(0); i++)
            {
                int offsetX = _offsets[i, 0];
                int offsetY = _offsets[i, 1];

                cell = GetCell(x + offsetX, y + offsetY, false);
                if (cell != null) cell.MaskRequiredWalls(GetWallMask(walls, (Dir)i));
            }
            return true;
        }

        public bool ReservableWideCell(int x, int y, Walls walls)
        {
            GenCell cell = GetCell(x, y);
            if ((~walls & Walls.All & cell.RequiredWalls) != 0) return false;
            if ((walls & cell.PreventWalls) != 0) return false;

            for (int i = 0; i < _offsets.GetLength(0); i++)
            {
                int offsetX = _offsets[i, 0];
                int offsetY = _offsets[i, 1];

                cell = GetCell(x + offsetX, y + offsetY, false);
                if (cell != null && !cell.CheckWallMask(GetWallMask(walls, (Dir)i), CellSetRegistry))
                    return false;
            }
            return true;
        }

        public override bool DestroyableCell(int x, int y)
        {
            GenCell cell = GetCell(x, y);
            if (cell == null || cell.CellRef != PrototypeId.Invalid) return false;

            for (int i = 0; i < _offsets.GetLength(0); i++)
            {
                int offsetX = _offsets[i, 0];
                int offsetY = _offsets[i, 1];

                GenCell checkCell = GetCell(x + offsetX, y + offsetY, false);
                if (checkCell != null && !checkCell.CheckWallMask(_wallMasks[i], CellSetRegistry))
                    return false;
            }

            return DestroyableCell(cell);
        }

        public override bool DestroyCell(int x, int y)
        {
            for (int i = 0; i < _offsets.GetLength(0); i++)
            {
                int offsetX = _offsets[i, 0];
                int offsetY = _offsets[i, 1];

                GenCell checkCell = GetCell(x + offsetX, y + offsetY, false);
                if (checkCell != null)
                    checkCell.MaskRequiredWalls(_wallMasks[i]);
            }

            return base.DestroyCell(x, y);
        }

        private bool GetCellToDirection(out GenCell cell, int x, int y, Cell.Type direction)
        {
            if (_typeToIndex.TryGetValue(direction, out var i))
            {
                x += _offsets[(int)i, 0];
                y += _offsets[(int)i, 1];

                if (TestCoord(x, y))
                {
                    cell = GetCell(x, y);
                    return true;
                }
            }
            cell = null;
            return false;
        }

        public override bool DestroyableCell(GenCell cell)
        {
            bool destroyable = base.DestroyableCell(cell);
            if (destroyable)
            {
                foreach (var corner in cell.Corners)
                {
                    if (corner != null && corner.HasDotCorner()) // Never true
                    {
                        destroyable = false;
                        break;
                    }
                }
            }
            return destroyable;
        }

        public override bool ReservableCell(int x, int y, PrototypeId cellRef)
        {
            if (!VerifyCoord(x, y)) return false;

            GenCell reserveCell = GetCell(x, y);
            if (reserveCell == null || reserveCell.CellRef != PrototypeId.Invalid)
                return false;

            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cellProto == null) return false;

            Walls walls = cellProto.Walls;

            bool CheckRoad(Cell.Type roads, Cell.Type roadToCheck, Cell.Type oppositeRoad)
            {
                if (roads.HasFlag(roadToCheck))
                {
                    if (!GetCellToDirection(out GenCell cell, x, y, roadToCheck)) return false;

                    var cellProto = GameDatabase.GetPrototype<CellPrototype>(cell.CellRef);
                    if (cellProto == null && !cell.CheckWallMask(Walls.None, null) ||
                        cellProto != null && !cellProto.RoadConnections.HasFlag(oppositeRoad)) return false;
                }
                return true;
            }

            Cell.Type roads = cellProto.RoadConnections;
            if (roads != Cell.Type.None)
            {
                if (!CheckRoad(roads, Cell.Type.N, Cell.Type.S) ||
                    !CheckRoad(roads, Cell.Type.E, Cell.Type.W) ||
                    !CheckRoad(roads, Cell.Type.S, Cell.Type.N) ||
                    !CheckRoad(roads, Cell.Type.W, Cell.Type.E)) return false;
            }

            return ReservableWideCell(x, y, walls);
        }

        public override bool ReserveCell(int x, int y, PrototypeId cellRef, GenCell.GenCellType type)
        {
            if (!ReservableCell(x, y, cellRef)) return false;
            if (!base.ReserveCell(x, y, cellRef, type)) return false;

            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cellProto == null) return false;

            return ModifyWideCell(x, y, cellProto.Walls);
        }

    }

}
