using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.DRAG.Generators.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Areas
{
    public class CellGridGenerator : BaseGridAreaGenerator
    {
        private enum ProcessEnum
        {
            Initialize,
            Generate
        }

        public class CellDeterminationMap : SortedDictionary<int, List<Point2>> { }

        public override bool InitializeContainer()
        {
            if (!base.InitializeContainer()) return false;
            if (Area.AreaPrototype.Generator is not GridAreaGeneratorPrototype proto) return false;
            if (proto.Behaviors.HasValue())
                RunBehaviors(null, proto.Behaviors, ProcessEnum.Initialize);

            return true;
        }

        private void RunBehaviors(GRandom random, CellGridBehaviorPrototype[] behaviors, ProcessEnum process)
        {
            if (behaviors == null) return;

            foreach (var behaviorProto in behaviors)
            {
                if (behaviorProto == null) continue;
                DetermineAndRunBehavior(random, behaviorProto, process);
            }
        }

        private void DetermineAndRunBehavior(GRandom random, CellGridBehaviorPrototype behaviorProto, ProcessEnum process)
        {
            if (behaviorProto == null) return;

            string behaviorIdString = GameDatabase.GetAssetName(behaviorProto.BehaviorId);

            if (behaviorIdString.Equals("CellGridBlacklistBehavior"))
            {
                if (process == ProcessEnum.Initialize)
                {
                    if (behaviorProto is CellGridBlacklistBehaviorPrototype behavior)
                    {
                        if (!CellGridBlacklistBehavior(behavior))
                            if (Log) Logger.Error("DetermineAndRunBehavior CellGridBlacklistBehavior");
                    }
                }
            }
            else if (behaviorIdString.Equals("CellGridRampBehavior"))
            {
                if (process == ProcessEnum.Initialize)
                {
                    if (behaviorProto is CellGridRampBehaviorPrototype behavior)
                    {
                        if (!CellGridRampBehavior(behavior))
                            if (Log) Logger.Error("DetermineAndRunBehavior CellGridRampBehavior");
                    }
                }
            }
        }

        private bool CellGridRampBehavior(CellGridRampBehaviorPrototype behavior)
        {
            if (CellContainer == null) return false;

            Cell.Type startEdge = GetEdgeFromAssetName(behavior.EdgeStart);
            Cell.Type endEdge = GetEdgeFromAssetName(behavior.EdgeEnd);

            if (startEdge == Cell.Type.W && endEdge == Cell.Type.E)
            {
                IncrementZ = 0.0f;
                IncrementX = 0.0f;
                IncrementY = behavior.Increment;
                return true;
            }
            if (startEdge == Cell.Type.S && endEdge == Cell.Type.N)
            {
                IncrementZ = 0.0f;
                IncrementX = behavior.Increment;
                IncrementY = 0.0f;
                return true;
            }
            if (startEdge == Cell.Type.E && endEdge == Cell.Type.W)
            {
                IncrementZ = behavior.Increment * (CellContainer.Height - 1);
                IncrementX = 0.0f;
                IncrementY = -behavior.Increment;
                return true;
            }
            if (startEdge == Cell.Type.N && endEdge == Cell.Type.S)
            {
                IncrementZ = behavior.Increment * (CellContainer.Width - 1);
                IncrementX = -behavior.Increment;
                IncrementY = 0.0f;
                return true;
            }

            return false;
        }

        private static Cell.Type GetEdgeFromAssetName(AssetId edge)
        {
            if (edge != 0 && Enum.TryParse(GameDatabase.GetAssetName(edge), out Cell.Type edgeType))
                return edgeType;

            return Cell.Type.None;
        }

        private bool CellGridBlacklistBehavior(CellGridBlacklistBehaviorPrototype behavior)
        {
            if (CellContainer == null || behavior.Blacklist.IsNullOrEmpty()) return false;

            bool success = true;
            foreach (var cell in behavior.Blacklist)
            {
                if (cell != null
                    && CellContainer.DestroyableCell(cell.X, cell.Y)
                    && CellContainer.DestroyCell(cell.X, cell.Y))
                    success = false;
            }

            return success;
        }

        public override bool Initialize(Area area)
        {
            CellContainer = new();
            return base.Initialize(area);
        }

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<PrototypeId> areas)
        {
            if (CellContainer == null) return false;
            if (Area.AreaPrototype.Generator is not GridAreaGeneratorPrototype proto) return false;

            bool success = false;
            int tries = 10;

            while (!success && --tries > 0)
            {
                success = InitializeContainer()
                    && EstablishExternalConnections()
                    && GenerateRandomInstanceLinks(random)
                    && CreateRequiredCells(random, regionGenerator, areas);
            }

            if (!success)
            {
                if (Log) Logger.Trace($"GridAreaGenerator failed after {10 - tries} attempts\nregion: {Region}\narea: {Area}");
                return false;
            }

            RunBehaviors(random, proto.Behaviors, ProcessEnum.Generate);

            ProcessDeleteExtraneousCells(random, (int)proto.RoomKillChancePct);
            // Logger.Warn($"[DeleteConnections]{CellContainer} ChancePct = {proto.RoomKillChancePct}");
            ProcessDeleteExtraneousConnections(random, (int)proto.ConnectionKillChancePct);
            ProcessRegionConnectionsAndDepth();
            ProcessAssignUniqueCellIds();
            ProcessCellPositions(proto.CellSize);

            return ProcessCellTypes(random);
        }

        private void ProcessDeleteExtraneousConnections(GRandom random, int chance)
        {
            if (CellContainer == null) return;

            foreach (GenCell cell in CellContainer)
                if (cell != null) CellContainer.DestroyUnrequiredConnections(cell, random, chance);
        }


        private bool ProcessCellTypes(GRandom random)
        {
            if (CellContainer == null) return false;
            if (Area.AreaPrototype.Generator is not GridAreaGeneratorPrototype proto) return false;

            int randomSeed = Area.RandomSeed;

            if (!BuildCellDeterminationMap(out CellDeterminationMap cellMap)) return false;

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
                            Cell.Type cellType = CellContainer.DetermineType(point.X, point.Y);
                            PrototypeId cellRef = 0;

                            List<PrototypeId> excludedList = new();
                            BuildExcludedListLikeCells(point.X, point.Y, cellType, excludedList);

                            if (CellSetRegistry.HasCellOfType(cellType))
                            {
                                cellRef = CellSetRegistry.GetCellSetAssetPicked(random, cellType, excludedList);
                            }

                            if (cellRef == 0)
                            {
                                if (Log) Logger.Trace($"Generator for Area {Area} tried to pick cell of type {cellType}, none were available. Region: {Region}");
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
                        PlaceFillerRoom(random, point.X, point.Y, new(point.X * proto.CellSize, point.Y * proto.CellSize, 0.0f));
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
                    Cell.Type type = CellContainer.DetermineType(x, y);

                    if (cellMap.TryGetValue((int)type, out List<Point2> pointList))
                    {
                        pointList.Add(new(x, y));
                    }
                    else
                    {
                        pointList = new() { new(x, y) };
                        cellMap[(int)type] = pointList;
                    }
                }
            }
            return true;
        }

        public static void CleanCellDeterminationMap(ref CellDeterminationMap cellMap)
        {
            if (cellMap != null)
            {
                foreach (var entry in cellMap)
                    entry.Value.Clear();

                cellMap.Clear();
                cellMap = null;
            }
        }

        public static bool CellGridBorderBehavior(Area area)
        {
            if (area == null) return false;

            CellGridBorderBehaviorPrototype borderBehaviorProto = null;
            GeneratorPrototype generatorProto = area.AreaPrototype.Generator;
            GridAreaGeneratorPrototype gridAreaGeneratorProto = generatorProto as GridAreaGeneratorPrototype;

            if (gridAreaGeneratorProto != null && gridAreaGeneratorProto.Behaviors.HasValue())
                borderBehaviorProto = gridAreaGeneratorProto.Behaviors.Last() as CellGridBorderBehaviorPrototype;

            if (borderBehaviorProto == null || gridAreaGeneratorProto.CellSets.IsNullOrEmpty()) return true;

            CellSetRegistry registry = new();
            registry.Initialize(true, area.Log);
            foreach (var cellSetEntry in gridAreaGeneratorProto.CellSets)
            {
                if (cellSetEntry == null) continue;
                registry.LoadDirectory(cellSetEntry.CellSet, cellSetEntry, cellSetEntry.Weight, cellSetEntry.Unique);
            }

            return DoBorderBehavior(area, borderBehaviorProto.BorderWidth, registry, gridAreaGeneratorProto.CellSize, gridAreaGeneratorProto.CellsX, gridAreaGeneratorProto.CellsY);
        }

    }
}
