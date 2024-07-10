using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Regions
{
    public class StaticRegionGenerator : RegionGenerator
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private bool Log;
        public override void GenerateRegion(bool log, int randomSeed, Region region)
        {
            Log = log;
            StartArea = null;
            GRandom random = new(randomSeed);
            StaticRegionGeneratorPrototype regionGeneratorProto = (StaticRegionGeneratorPrototype)GeneratorPrototype;
            StaticAreaPrototype[] staticAreas = regionGeneratorProto.StaticAreas;
            PrototypeId areaRef = region.RegionPrototype.GetDefaultArea(region);

            foreach (StaticAreaPrototype staticAreaProto in staticAreas)
            {
                Vector3 areaOrigin = new(staticAreaProto.X, staticAreaProto.Y, staticAreaProto.Z);
                Area area = region.CreateArea(staticAreaProto.Area, areaOrigin);
                if (area != null)
                {
                    AddAreaToMap(staticAreaProto.Area, area);
                    if (staticAreaProto.Area == areaRef)
                        StartArea = area;
                }
            }
            if (staticAreas.HasValue())
                DoConnection(random, region, staticAreas, regionGeneratorProto);

        }

        private void DoConnection(GRandom random, Region region, StaticAreaPrototype[] staticAreas, StaticRegionGeneratorPrototype regionGeneratorProto)
        {
            RegionProgressionGraph graph = region.ProgressionGraph;
            if (StartArea != null && graph.GetRoot() == null) graph.SetRoot(StartArea);

            if (staticAreas.Length > 1)
            {
                if (regionGeneratorProto.Connections.HasValue())
                {
                    List<AreaConnectionPrototype> workingConnectionList = new(regionGeneratorProto.Connections);

                    if (workingConnectionList.Count == 0)
                    {
                        if (Log) Logger.Error("Calligraphy Error: More than one area in region but there are no connections specified.");
                        return;
                    }

                    List<PrototypeId> nextConnections = new();
                    List<PrototypeId> prevConnections = new()
                    {
                        StartArea.PrototypeDataRef
                    };
                    ConnectNextAreas(random, workingConnectionList, prevConnections, nextConnections, graph);
                }
            }
        }

        public static bool ConnectionListContainsArea(List<PrototypeId> connections, PrototypeId area)
        {
            return connections.Contains(area);
        }

        public static bool GenerateConnectionFromQueriedPoints(GRandom random, out Vector3 connection, Area areaA, Area areaB)
        {
            ConnectionList sharedConnections = new();
            connection = default;

            if (!GetSharedConnections(sharedConnections, areaA, areaB)) return false;

            Picker<Vector3> picker = new(random);
            foreach (var point in sharedConnections)
                picker.Add(point);

            if (picker.Empty()) return false;
            if (!picker.Pick(out connection)) return false;

            return true;
        }

        public void ConnectNextAreas(GRandom random, List<AreaConnectionPrototype> workingConnectionList, List<PrototypeId> prevConnections, List<PrototypeId> nextConnections, RegionProgressionGraph graph)
        {
            int failout = 100;
            foreach (var areaConnectProto in workingConnectionList.TakeWhile(_ => failout-- > 0))
            {
                if (areaConnectProto == null) continue;

                Area areaA = GetAreaFromPrototypeRef(areaConnectProto.AreaA);
                Area areaB = GetAreaFromPrototypeRef(areaConnectProto.AreaB);

                if (areaA == null && areaB == null) continue;

                if (ConnectionListContainsArea(prevConnections, areaConnectProto.AreaA))
                {
                    if (areaConnectProto.ConnectAllShared)
                    {
                        ConnectionList sharedConnections = new();
                        GetSharedConnections(sharedConnections, areaA, areaB);
                        SetSharedConnections(sharedConnections, areaA, areaB);
                    }
                    else
                    {
                        if (GenerateConnectionFromQueriedPoints(random, out Vector3 connection, areaA, areaB) == false) continue;
                        Area.CreateConnection(areaA, areaB, connection, ConnectPosition.One);
                    }

                    graph.AddLink(areaA, areaB);
                    nextConnections.Add(areaConnectProto.AreaB);
                    continue;
                }

                if (ConnectionListContainsArea(prevConnections, areaConnectProto.AreaB))
                {
                    if (areaConnectProto.ConnectAllShared)
                    {
                        ConnectionList sharedConnections = new();
                        GetSharedConnections(sharedConnections, areaA, areaB);
                        SetSharedConnections(sharedConnections, areaB, areaA);
                    }
                    else
                    {
                        if (GenerateConnectionFromQueriedPoints(random, out Vector3 connection, areaA, areaB) == false) continue;
                        Area.CreateConnection(areaB, areaA, connection, ConnectPosition.One);
                    }

                    graph.AddLink(areaB, areaA);
                    nextConnections.Add(areaConnectProto.AreaA);
                    continue;
                }
            }

            if (nextConnections.Any())
            {
                prevConnections.Clear();
                prevConnections.AddRange(nextConnections);
                nextConnections.Clear();
                ConnectNextAreas(random, workingConnectionList, prevConnections, nextConnections, graph);
            }

            if (failout == 0)
                if (Log) Logger.Error("We overstayed our welcome trying to connect areas.");

            return;
        }

    }
}
