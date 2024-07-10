using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Regions
{
    public class SequenceRegionGenerator : RegionGenerator
    {
        public override void GenerateRegion(bool log, int randomSeed, Region region)
        {
            StartArea = null;
            SequenceRegionGeneratorPrototype regionGeneratorProto = (SequenceRegionGeneratorPrototype)GeneratorPrototype;
            SequenceStack sequenceStack = new();
            GRandom random = new(randomSeed);
            RegionSettings setting = region.Settings;

            if (regionGeneratorProto.AreaSequence.HasValue())
            {
                sequenceStack.Initialize(log, region, this, regionGeneratorProto.AreaSequence);
            }
            else if (regionGeneratorProto.EndlessThemes.HasValue())
            {
                // TODO for DangerRoom 
                int endlessLevelsTotal = 0;// TODO region.PropertyCollection.GetProperty(PropertyEnum.EndlessLevelsTotal);
                EndlessThemeEntryPrototype endlessTheme = regionGeneratorProto.GetEndlessGeneration(randomSeed, setting.EndlessLevel, endlessLevelsTotal);
                MetaStateChallengeTierEnum missionTier = region.RegionAffixGetMissionTier();
                EndlessStateEntryPrototype endlessState = endlessTheme.GetState(randomSeed, setting.EndlessLevel, missionTier);

                if (endlessState.MetaState != 0)
                {
                    // region.PropertyCollection.SetProperty(PropertyEnum.MetaStateApplyOnInit, endlessState.MetaState);
                }

                if (endlessState.RegionPOIPicker != 0)
                {
                    if (GeneratorPrototype.POIGroups.HasValue()) POIPickerCollection = new(regionGeneratorProto);
                    POIPickerCollection.RegisterPOIGroup(endlessState.RegionPOIPicker);
                }

                sequenceStack.Initialize(log, region, this, endlessTheme.AreaSequence);
                random = new(randomSeed + setting.EndlessLevel);
            }

            RegionProgressionGraph graph = region.ProgressionGraph;
            bool success = sequenceStack.ProcessSequence(random, null, graph, new());
            bool subSuccess = true;
            if (regionGeneratorProto.SubAreaSequences.HasValue())
            {
                foreach (SubGenerationPrototype subArea in regionGeneratorProto.SubAreaSequences)
                {
                    if (subArea != null && subArea.AreaSequence.HasValue())
                    {
                        GenAtPositionFunctor functor = new(log, this, region, random, subArea.AreaSequence, subArea.MinRootSeparation, subArea.Tries);
                        PositionFunctor.IterateGridPositionsInConcentricSquares(functor, subArea.MinRootSeparation);
                        if (functor.Success == false)
                        {
                            subSuccess = false;
                            break;
                        }
                    }
                }
            }

            if (success && subSuccess) CenterRegion(region);

        }
    }

    public abstract class PositionFunctor
    {
        public abstract bool Process(ref Vector3 position);

        public static void IterateGridPositionsInConcentricSquares(PositionFunctor functor, float step)
        {
            if (step <= 0.0f) return;

            int gridIndex = 1;

            while (true)
            {
                int squares = gridIndex - 1;
                int maxSquares = Math.Max(squares * 4, 1);
                int gridOffset = gridIndex / 2;

                for (int square = 0; square < maxSquares; square++)
                {
                    int side = square % 4;
                    int currentSquare = square / 4;
                    bool invert = currentSquare % 2 > 0;
                    int squareOffset = (currentSquare + 1) / 2;

                    int x = 0;
                    int y = 0;

                    switch (side)
                    {
                        case 0:
                            x = invert ? squareOffset : -squareOffset;
                            y = gridOffset;
                            break;

                        case 1:
                            x = gridOffset;
                            y = invert ? -squareOffset : squareOffset;
                            break;

                        case 2:
                            x = invert ? -squareOffset : squareOffset;
                            y = -gridOffset;
                            break;

                        case 3:
                            x = -gridOffset;
                            y = invert ? squareOffset : -squareOffset;
                            break;
                    }

                    Vector3 position = new(x * step, y * step, 0.0f);

                    if (!functor.Process(ref position)) return;
                }

                gridIndex += 2;
            }
        }

    }

    public class GenAtPositionFunctor : PositionFunctor
    {
        private int _tries;
        private float _separation;
        private SequenceRegionGenerator _generator;
        private Region _region;
        private GRandom _random;
        private AreaSequenceInfoPrototype[] _areaSequence;

        public bool Success;
        private bool _log;

        public GenAtPositionFunctor(bool log, SequenceRegionGenerator generator, Region region, GRandom random, AreaSequenceInfoPrototype[] areaSequence, float separation, int tries)
        {
            _generator = generator;
            _region = region;
            _random = random;
            _areaSequence = areaSequence;
            _separation = separation;
            _tries = tries;
            Success = false;
            _log = log;
        }

        public override bool Process(ref Vector3 position)
        {
            if (_region.GetDistanceToClosestAreaBounds(position) < _separation)
                return true;

            SequenceStack sequenceStack = new();
            sequenceStack.Initialize(_log, _region, _generator, _areaSequence);
            Success = sequenceStack.ProcessSequence(_random, null, null, position);

            return --_tries > 0 && !Success;
        }
    }


    #region SequenceStack

    public class SequenceStack
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private bool Log;

        private SequenceStackEntry _root;
        private List<SequenceStackEntry> _entries;
        public Region Region { get; private set; }
        public SequenceRegionGenerator Generator { get; private set; }
        public AreaSequenceInfoPrototype[] RootAreaInfos { get; private set; }
        public List<AreaSequenceInfoPrototype> SelectedAreaInfos { get; private set; }

        public SequenceStack()
        {
            _entries = new();
            SelectedAreaInfos = new();
        }

        public void Initialize(bool log, Region region, SequenceRegionGenerator generator, AreaSequenceInfoPrototype[] areaInfos)
        {
            Region = region;
            Log = log;
            RootAreaInfos = areaInfos;
            Generator = generator;
            SequenceStackEntry entry = AddEntry(null);
            _root = entry;
        }

        public SequenceStackEntry AddEntry(SequenceStackEntry previous)
        {
            SequenceStackEntry entry = new(previous);
            if (entry == null) return null;

            _entries.Add(entry);

            if (previous != null) previous.AddChild(entry);

            return entry;
        }

        public void RemoveEntry(SequenceStackEntry entry)
        {
            if (entry != null)
            {
                foreach (var e in _entries)
                {
                    if (e == entry)
                    {
                        _entries.Remove(e);
                        break;
                    }
                }
                if (entry.Previous != null) entry.Previous.RemoveChild(entry);
                // entry = null;
            }
        }

        public bool ProcessSequence(GRandom random, SequenceStackEntry entry, RegionProgressionGraph graph, Vector3 origin)
        {
            if (Region == null) return false;

            AreaSequenceInfoPrototype[] areaInfos;
            List<AreaSequenceInfoPrototype> selectedAreaSequenceInfos;

            if (entry == null)
            {
                entry = _root;
                if (RootAreaInfos == null) return false;

                areaInfos = RootAreaInfos;
                selectedAreaSequenceInfos = SelectedAreaInfos;
            }
            else
            {
                if (entry.Previous == null || entry.Previous.SequenceInfo == null) return false;

                areaInfos = entry.Previous.SequenceInfo.ConnectedTo;
                selectedAreaSequenceInfos = entry.Previous.SelectedAreaSequenceInfos;
            }

            if (areaInfos == null) return true;

            bool success = false;

            while (success == false)
            {
                if (!PickSequence(random, entry, areaInfos, selectedAreaSequenceInfos)) break;

                while (success == false)
                {
                    if (!PickArea(random, entry)) break;

                    while (success == false)
                    {
                        if (!PickAreaPlacement(random, entry, false, origin))
                        {
                            entry.GetAreaSequence(new());
                            if (Log) Logger.Error("Area couldn't place next to previous.");
                            break;
                        }

                        success = true;

                        if (graph != null)
                        {
                            if (entry == _root)
                                graph.SetRoot(entry.Area);
                            else
                                graph.AddLink(entry.Previous.Area, entry.Area);
                        }

                        if (entry.SequenceInfo.ConnectedTo.HasValue())
                        {
                            int picks = entry.SequenceInfo.ConnectedToPicks != 0 ? entry.SequenceInfo.ConnectedToPicks : 1;

                            for (int i = 0; i < picks && success; i++)
                            {
                                SequenceStackEntry newEntry = AddEntry(entry);
                                success &= ProcessSequence(random, newEntry, graph, origin);
                            }
                        }

                        if (success)
                        {
                            Area area = entry.Area;
                            if (area != null)
                            {
                                List<PrototypeId> areas = new();
                                entry.GetAreaSequence(areas);
                                success &= area.Generate(Generator, areas, GenerateFlag.Background);
                                if (success == false)
                                {
                                    if (Log) Logger.Error("Area failed to generate.");
                                }
                            }
                            else
                            {
                                success = false;
                            }
                        }

                        if (success)
                        {
                            return true;
                        }
                        else
                        {
                            if (entry != _root && graph != null)
                                graph.RemoveLink(entry.Previous.Area, entry.Area);

                            if (entry.HasOtherConnectionOptions())
                            {
                                CleanStackFromPoint(entry, false);
                                entry.SelectedAreaSequenceInfos.Clear();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (success == false)
                    {
                        /* TimeSpan generationTime = Generator.GetStartTime() - Region.Game.GetRealGameTime();
                         if (generationTime > TimeSpan.FromSeconds((float)SequenceRegionGenerator.MaxGenerationTimeInSec))
                         {
                             // Generation Exceeded
                             return false;
                         }*/

                        Area area = entry.Area;
                        if (area != null)
                            ClearAreaEntry(area, entry);

                        CleanStackFromPoint(entry, true);
                        break;
                    }
                }
            }

            return success;
        }

        private void ClearAreaEntry(Area area, SequenceStackEntry entry)
        {
            Generator.DereferenceFromPOI(area);

            foreach (var id in area.SubAreas)
                Region.DestroyArea(id);
            Region.DestroyArea(area.Id);
            entry.ClearArea();
        }

        private void CleanStackFromPoint(SequenceStackEntry entry, bool cleanAll)
        {
            if (entry == null) return;

            if (entry.Сhildrens.Any())
            {
                List<SequenceStackEntry> toRemove = new();

                foreach (var child in entry.Сhildrens)
                {
                    if (child != null)
                    {
                        CleanStackFromPoint(child, true);
                        toRemove.Add(child);
                    }
                }

                foreach (var child in toRemove)
                    RemoveEntry(child);
            }


            if (cleanAll)
            {
                Area area = entry.Area;
                if (area != null)
                    ClearAreaEntry(area, entry);

                entry.WeightedAreas.Clear();
                entry.WeightedArea = null;

                entry.AreaConnectionPicker = null;
            }
        }

        private bool PickAreaPlacement(GRandom random, SequenceStackEntry entry, bool report, Vector3 origin)
        {
            if (Region == null || entry == null) return false;

            WeightedAreaPrototype weightedArea = entry.WeightedArea;
            Area area = entry.Area;

            if (area != null)
            {
                ClearAreaEntry(area, entry);
            }
            else
            {
                Area testPreviousArea = Region.GetArea(weightedArea.Area);
                if (testPreviousArea != null)
                {
                    if (Log) Logger.Error("Duplicate Area found during generation");
                    return false;
                }
            }

            area = Region.CreateArea(weightedArea.Area, new());
            if (area == null) return false;

            area.RespawnOverride = weightedArea.RespawnOverride;

            bool success = false;

            if (entry.Previous == null)
            {
                area.SetOrigin(origin);
                success = true;
            }
            else
            {
                AreaSequenceInfoPrototype sequenceInfo = entry.SequenceInfo;

                if (sequenceInfo != null)
                {
                    Area previousArea = entry.Previous.Area;

                    if (previousArea != null)
                    {
                        AreaSequenceInfoPrototype previousSequenceInfo = entry.Previous.SequenceInfo;
                        Picker<ConnectionPair> picker = entry.AreaConnectionPicker;

                        if (picker == null)
                        {
                            EdgeReport nextEdge = new(area, weightedArea.ConnectOn);
                            EdgeReport prevEdge = new(previousArea, RegionDirection.NoRestriction);

                            List<ConnectionPair> possibleConnections = new();
                            EdgeReport.GetPossibleConnectionPairs(prevEdge, nextEdge, possibleConnections, weightedArea.AlignedToPrevious);

                            picker = entry.SetAreaConnectionPicker(possibleConnections, random);

                            if (report && picker == null)
                            {
                                entry.GetAreaSequence(new());
                                if (Log) Logger.Error("Area couldn't build any shared edges with previous area.");
                            }
                        }

                        ConnectionList sharedConnections = new();

                        while (picker != null && !picker.Empty() && picker.PickRemove(out ConnectionPair connectionPair))
                        {
                            Aabb localBounds = area.LocalBounds;
                            Vector3 translation = connectionPair.A - connectionPair.B;
                            translation.RoundToNearestInteger(); // Fix for 0.001
                            Aabb testBounds = localBounds.Translate(translation);

                            bool testCollision = false;

                            foreach (var testArea in Region.IterateAreas())
                            {
                                if (testArea == area) continue;

                                if (testArea.RegionBounds.ContainsXY(testBounds, -128.0f) != ContainmentType.Disjoint)
                                {
                                    testCollision = true;

                                    if (report)
                                    {
                                        entry.GetAreaSequence(new());
                                        if (Log) Logger.Error("Area collided with AREA");
                                    }

                                    break;
                                }
                            }

                            if (testCollision) continue;

                            area.SetOrigin(translation);

                            if (RegionGenerator.GetSharedConnections(sharedConnections, area, previousArea))
                            {
                                if (sequenceInfo.SharedEdgeMinimum > 0 && sharedConnections.Count < sequenceInfo.SharedEdgeMinimum)
                                {
                                    if (report)
                                    {
                                        entry.GetAreaSequence(new());
                                        if (Log) Logger.Error("Area's SharedEdgeMinimum prevented placement.");
                                    }

                                    continue;
                                }

                                if (previousSequenceInfo.ConnectAllShared)
                                {
                                    RegionGenerator.GetSharedConnections(sharedConnections, area, previousArea);
                                    RegionGenerator.SetSharedConnections(sharedConnections, area, previousArea);
                                }
                                else
                                {
                                    Area.CreateConnection(area, previousArea, connectionPair.A, ConnectPosition.One);
                                }

                                success = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (success)
            {
                entry.Area = area;
                return true;
            }

            ClearAreaEntry(area, entry);

            return false;
        }

        private bool PickArea(GRandom random, SequenceStackEntry entry)
        {
            if (entry == null) return false;

            AreaSequenceInfoPrototype sequenceInfo = entry.SequenceInfo;
            if (sequenceInfo == null) return false;

            WeightedAreaPrototype[] areaChoices = sequenceInfo.AreaChoices;
            if (areaChoices == null || areaChoices.Length == 0) return false;

            List<WeightedAreaPrototype> weightedAreas = entry.WeightedAreas;
            if (areaChoices.Length <= weightedAreas.Count) return false;

            Picker<WeightedAreaPrototype> picker = new(random);
            foreach (var areaChoice in areaChoices)
            {
                if (areaChoice == null || areaChoice.Area == 0) continue;

                bool skip = false;
                foreach (var weightedArea in weightedAreas)
                {
                    if (weightedArea == areaChoice)
                        skip = true;
                }

                if (!skip && Region.GetArea(areaChoice.Area) != null)
                {
                    if (Log) Logger.Error("Duplicate Area found during generation");
                    skip = true;
                }

                if (skip) continue;

                picker.Add(areaChoice, areaChoice.Weight);
            }

            WeightedAreaPrototype pick = null;
            if (!picker.Empty())
            {
                picker.Pick(out pick);
                weightedAreas.Add(pick);
            }

            entry.WeightedArea = pick;

            return pick != null;
        }

        private static bool PickSequence(GRandom random, SequenceStackEntry entry, AreaSequenceInfoPrototype[] areaInfos, List<AreaSequenceInfoPrototype> selectedAreaSequenceInfos)
        {
            if (entry == null || areaInfos.IsNullOrEmpty() || selectedAreaSequenceInfos == null) return false;

            entry.Reset();

            if (areaInfos.Length <= selectedAreaSequenceInfos.Count) return false;

            Picker<AreaSequenceInfoPrototype> picker = new(random);
            foreach (var info in areaInfos)
            {
                if (info == null || info.AreaChoices.IsNullOrEmpty()) continue;

                bool skip = false;
                foreach (var pickedInfo in selectedAreaSequenceInfos)
                {
                    if (pickedInfo == info)
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip) continue;

                picker.Add(info, info.Weight);
            }

            AreaSequenceInfoPrototype pick = null;
            if (!picker.Empty())
            {
                picker.Pick(out pick);
                selectedAreaSequenceInfos.Add(pick);
            }

            entry.SequenceInfo = pick;

            return pick != null;
        }
    }

    public struct ConnectionPair
    {
        public Vector3 A;
        public Vector3 B;

        public ConnectionPair(Vector3 a, Vector3 b) { A = a; B = b; }
        public ConnectionPair(Vector3 a) { A = a; B = a; }
        public ConnectionPair(ConnectionPair pair) { A = pair.A; B = pair.B; }
    }

    public struct EdgePair
    {
        public Cell.Type A;
        public Cell.Type B;

        public EdgePair(Cell.Type a, Cell.Type b) { A = a; B = b; }
    }

    public class SequenceStackEntry
    {
        public AreaSequenceInfoPrototype SequenceInfo { get; set; }
        public List<AreaSequenceInfoPrototype> SelectedAreaSequenceInfos { get; }
        public WeightedAreaPrototype WeightedArea { get; set; }
        public List<WeightedAreaPrototype> WeightedAreas { get; }
        public List<SequenceStackEntry> Сhildrens { get; }
        public SequenceStackEntry Previous { get; }
        public Picker<ConnectionPair> AreaConnectionPicker { get; set; }
        public Area Area { get; set; }

        public SequenceStackEntry(SequenceStackEntry previous)
        {
            Сhildrens = new();
            SelectedAreaSequenceInfos = new();
            WeightedAreas = new();

            Previous = previous;
        }

        public Picker<ConnectionPair> SetAreaConnectionPicker(List<ConnectionPair> pairs, GRandom random)
        {
            if (WeightedArea == null || AreaConnectionPicker != null || pairs.Count == 0) return null;

            AreaConnectionPicker = new(random);

            foreach (var pair in pairs)
                AreaConnectionPicker.Add(pair);

            return AreaConnectionPicker;
        }

        public void Reset()
        {
            SequenceInfo = null;
            SelectedAreaSequenceInfos.Clear();
            WeightedArea = null;
            WeightedAreas.Clear();
            Area = null;
        }

        public bool GetAreaSequence(List<PrototypeId> areas)
        {
            if (Previous != null) Previous.GetAreaSequence(areas);

            PrototypeId area = WeightedArea.Area;
            areas.Add(area);
            return areas.Any();
        }

        public void AddChild(SequenceStackEntry entry)
        {
            if (entry != null) Сhildrens.Add(entry);
        }

        public void RemoveChild(SequenceStackEntry entry)
        {
            if (entry != null) Сhildrens.Remove(entry);
        }

        public bool HasOtherConnectionOptions()
        {
            if (WeightedArea != null && AreaConnectionPicker != null) return !AreaConnectionPicker.Empty();
            return false;
        }

        public void ClearArea() { Area = null; }
    }

    public class ConnectionList : List<Vector3> { public ConnectionList() { } }

    public class AreaEdge
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Area Area;
        public Cell.Type Type;
        public Segment Edge;
        public ConnectionList ConnectionList;

        public AreaEdge(Area area, Cell.Type type, Vector3 a, Vector3 b)
        {
            Area = area;
            Type = type;
            Edge = new(a, b);
            ConnectionList = new();

            if (a.X == b.X)
            {
                Edge.Start = a.Y < b.Y ? a : b;
                Edge.End = a.Y < b.Y ? b : a;
                area.GetPossibleAreaConnections(ConnectionList, Edge);
            }
            else if (a.Y == b.Y)
            {
                Edge.Start = a.X < b.X ? a : b;
                Edge.End = a.X < b.X ? b : a;
                area.GetPossibleAreaConnections(ConnectionList, Edge);
            }
            else
            {
                Logger.Error("AreaEdge A != B");
            }

        }

        public bool IsValid() => Edge.Length > 0 && ConnectionList.Any();
        public float GetLength() => Edge.Length;

    }

    public class EdgeReport
    {
        public Area Area { get; }
        public List<AreaEdge> Edges { get; }
        public Cell.Type EdgeType { get; private set; }
        public RegionDirection Direction { get; }

        public EdgeReport(Area area, RegionDirection direction)
        {
            Area = area;
            Edges = new();
            EdgeType = Cell.Type.None;
            Direction = direction;

            BuildEdgeList();
        }

        public bool BuildEdgeList()
        {
            Aabb bounds = Area.RegionBounds;
            Vector3 min = bounds.Min;
            Vector3 max = bounds.Max;

            AreaEdge edge;

            if (Direction == RegionDirection.NoRestriction || Direction.HasFlag(RegionDirection.West))
            {
                edge = new(Area, Cell.Type.W, new Vector3(min.X, min.Y, 0), new Vector3(max.X, min.Y, 0));
                PushOrCleanEdge(edge);
            }

            if (Direction == RegionDirection.NoRestriction || Direction.HasFlag(RegionDirection.East))
            {
                edge = new(Area, Cell.Type.E, new Vector3(min.X, max.Y, 0), new Vector3(max.X, max.Y, 0));
                PushOrCleanEdge(edge);
            }

            if (Direction == RegionDirection.NoRestriction || Direction.HasFlag(RegionDirection.North))
            {
                edge = new(Area, Cell.Type.N, new Vector3(max.X, min.Y, 0), new Vector3(max.X, max.Y, 0));
                PushOrCleanEdge(edge);
            }

            if (Direction == RegionDirection.NoRestriction || Direction.HasFlag(RegionDirection.South))
            {
                edge = new(Area, Cell.Type.S, new Vector3(min.X, min.Y, 0), new Vector3(min.X, max.Y, 0));
                PushOrCleanEdge(edge);
            }

            if (Edges.Any()) return true;

            return false;
        }

        public bool HasEdge(Cell.Type side) => (EdgeType & side) != 0;

        private void PushOrCleanEdge(AreaEdge edge)
        {
            if (edge != null && edge.IsValid())
            {
                Edges.Add(edge);
                EdgeType |= edge.Type;
            }
        }

        public static bool GetPossibleConnectionPairs(EdgeReport edgeReportA, EdgeReport edgeReportB, List<ConnectionPair> possibleConnections, bool aligned)
        {
            List<EdgePair> possibleEdges = new();
            if (GetPossibleConnectionEdges(edgeReportA, edgeReportB, possibleEdges))
            {
                foreach (var possibleEdge in possibleEdges)
                {
                    AreaEdge edgeA = edgeReportA.GetEdge(possibleEdge.A);
                    AreaEdge edgeB = edgeReportB.GetEdge(possibleEdge.B);

                    if (edgeA != null && edgeB != null)
                    {
                        foreach (var pointA in edgeA.ConnectionList)
                        {
                            foreach (var pointB in edgeB.ConnectionList)
                            {
                                ConnectionPair pair = new(pointA, pointB);
                                if (aligned == false || CheckAlignment(edgeA, edgeB, pair)) possibleConnections.Add(pair);
                            }
                        }
                    }
                }
            }
            return false;
        }

        public AreaEdge GetEdge(Cell.Type type)
        {
            foreach (var edge in Edges)
                if (edge.Type == type) return edge;

            return null;
        }

        private static bool CheckAlignment(AreaEdge edgeA, AreaEdge edgeB, ConnectionPair pair)
        {
            if (edgeA == null || edgeB == null) return false;

            Segment segmentA = edgeA.Edge;
            Segment segmentB = edgeB.Edge;

            Vector3 translation = pair.A - pair.B;
            Segment translatedB = new(segmentB.Start + translation, segmentB.End + translation);

            Cell.Type type = edgeA.Type | edgeB.Type;

            if (type == Cell.Type.EW)
            {
                bool hasAinB = segmentA.Start.X <= translatedB.Start.X && segmentA.End.X >= translatedB.End.X;
                bool hasBinA = translatedB.Start.X <= segmentA.Start.X && translatedB.End.X >= segmentA.End.X;

                return hasAinB || hasBinA;
            }
            else if (type == Cell.Type.NS)
            {
                bool hasAinB = segmentA.Start.Y <= translatedB.Start.Y && segmentA.End.Y >= translatedB.End.Y;
                bool hasBinA = translatedB.Start.Y <= segmentA.Start.Y && translatedB.End.Y >= segmentA.End.Y;

                return hasAinB || hasBinA;
            }
            else
            {
                return false;
            }
        }

        private static bool GetPossibleConnectionEdges(EdgeReport edgeReportA, EdgeReport edgeReportB, List<EdgePair> possibleEdges)
        {
            if (edgeReportA.HasEdge(Cell.Type.W) && edgeReportB.HasEdge(Cell.Type.E))
                possibleEdges.Add(new(Cell.Type.W, Cell.Type.E));

            if (edgeReportA.HasEdge(Cell.Type.E) && edgeReportB.HasEdge(Cell.Type.W))
                possibleEdges.Add(new(Cell.Type.E, Cell.Type.W));

            if (edgeReportA.HasEdge(Cell.Type.N) && edgeReportB.HasEdge(Cell.Type.S))
                possibleEdges.Add(new(Cell.Type.N, Cell.Type.S));

            if (edgeReportA.HasEdge(Cell.Type.S) && edgeReportB.HasEdge(Cell.Type.N))
                possibleEdges.Add(new(Cell.Type.S, Cell.Type.N));

            if (possibleEdges.Any()) return true;

            return false;
        }
    }

    #endregion
}
