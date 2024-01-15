using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Regions
{
    public class RegionGenerator
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Area StartArea { get; set; }
        public Dictionary<ulong, Area> AreaMap { get; private set; }
        public RegionGeneratorPrototype GeneratorPrototype { get; private set; }
        public RegionPOIPickerCollection POIPickerCollection { get; set; }

        public void Initialize(RegionGeneratorPrototype generatorPrototype) 
        {
            GeneratorPrototype = generatorPrototype;
            AreaMap = new();

            if (GeneratorPrototype.POIGroups != null)
                POIPickerCollection = new(generatorPrototype);
        }

        public virtual void GenerateRegion(int randomSeed, Region region) { }

        public void AddAreaToMap(ulong areaProtoId, Area area)
        {
            if (areaProtoId != 0)
                AreaMap.Add(areaProtoId, area);
        }

        public Area GetAreaFromPrototypeRef(ulong dataRef)
        {
            if (dataRef != 0 && AreaMap.TryGetValue(dataRef, out Area area)) return area;
            return null;
        }

        public void DereferenceFromPOI(Area area)
        {
            if (area != null && POIPickerCollection != null) 
                POIPickerCollection.DereferenceArea(area);
        }

        public static void CenterRegion(Region region)
        {
            Aabb bound = region.CalculateBound();
            Vector3 center = bound.Center;

            foreach (Area area in region.IterateAreas())  
                 area.SetOrigin(area.Origin - center);
        }

        public static bool GetSharedConnections(ConnectionList sharedConnections, Area areaA, Area areaB)
        {
            if (areaA == null || areaB == null) return false;

            sharedConnections.Clear();
            
            if (GetSharedEdgeSegment(out Segment sharedEdge, areaA, areaB) == false)
            {
                Logger.Error("Calligraphy Error: Do not share a common edge");
                return false;
            }

            ConnectionList connectionsA = new();
            bool hasA = areaA.GetPossibleAreaConnections(connectionsA, sharedEdge);

            ConnectionList connectionsB = new();
            bool hasB = areaB.GetPossibleAreaConnections(connectionsB, sharedEdge);

            bool connectionsFound = false;

            if (hasA && hasB)
            {
                foreach (var connectionA in connectionsA)
                {
                    foreach (var connectionB in connectionsB)
                    {
                        if (Vector3.EpsilonSphereTest(connectionA, connectionB, 10.0f))
                        {
                            sharedConnections.Add(connectionA);
                            connectionsFound = true;
                        }
                    }
                }
            }

            if (connectionsFound == false)
                Logger.Error($"No connection found between: AreaA: {areaA.PrototypeId} AreaB: {areaB.PrototypeId}"); 

            return connectionsFound;
        }

        private static bool GetSharedEdgeSegment(out Segment sharedEdge, Area areaA, Area areaB)
        {
            sharedEdge = new();

            Aabb boundsA = areaA.RegionBounds;
            Aabb boundsB = areaB.RegionBounds;

            if (Segment.EpsilonTest(boundsA.Max.X, boundsB.Min.X, 10.0f))
            {
                float x = boundsA.Max.X;
                float maxY = Math.Min(boundsA.Max.Y, boundsB.Max.Y);
                float minY = Math.Max(boundsA.Min.Y, boundsB.Min.Y);

                sharedEdge.Start.Set(x, minY, 0.0f);
                sharedEdge.End.Set(x, maxY, 0.0f);
                return true;
            }
            else if (Segment.EpsilonTest(boundsA.Max.Y, boundsB.Min.Y, 10.0f))
            {
                float y = boundsA.Max.Y;
                float maxX = Math.Min(boundsA.Max.X, boundsB.Max.X);
                float minX = Math.Max(boundsA.Min.X, boundsB.Min.X);

                sharedEdge.Start.Set(minX, y, 0.0f);
                sharedEdge.End.Set(maxX, y, 0.0f);
                return true;
            }
            else if (Segment.EpsilonTest(boundsA.Min.X, boundsB.Max.X, 10.0f))
            {
                float x = boundsA.Min.X;
                float maxY = Math.Min(boundsA.Max.Y, boundsB.Max.Y);
                float minY = Math.Max(boundsA.Min.Y, boundsB.Min.Y);

                sharedEdge.Start.Set(x, minY, 0.0f);
                sharedEdge.End.Set(x, maxY, 0.0f);
                return true;
            }
            else if (Segment.EpsilonTest(boundsA.Min.Y, boundsB.Max.Y, 10.0f))
            {
                float y = boundsA.Min.Y;
                float maxX = Math.Min(boundsA.Max.X, boundsB.Max.X);
                float minX = Math.Max(boundsA.Min.X, boundsB.Min.X);

                sharedEdge.Start.Set(minX, y, 0.0f);
                sharedEdge.End.Set(maxX, y, 0.0f);
                return true;
            }
            return false;
        }

        public static void SetSharedConnections(ConnectionList sharedConnections, Area areaA, Area areaB)
        {
            if (sharedConnections.Count == 1)
            {
                Area.CreateConnection(areaA, areaB, sharedConnections.First(), ConnectPosition.One);
            }
            else
            {
                for (int connectionIndex = 0; connectionIndex < sharedConnections.Count; connectionIndex++)
                {
                    ConnectPosition connectionPoint;

                    if (connectionIndex == 0)
                        connectionPoint = ConnectPosition.Begin;
                    else if (connectionIndex == sharedConnections.Count - 1)
                        connectionPoint = ConnectPosition.End;
                    else
                        connectionPoint = ConnectPosition.Inside;

                    Area.CreateConnection(areaA, areaB, sharedConnections[connectionIndex], connectionPoint);
                }
            }
        }

        public bool GetRequiredPOICellsForArea(Area area, GRandom random, out List<Prototype> list)
        {
            list = new ();
            if (area == null) return false;
            bool success = true;
            if (POIPickerCollection != null)
            {
                POIPickerCollection.DereferenceArea(area);
                success &= POIPickerCollection.GetCellsForArea(area, random, list);
            }

            return success;
        }

    }
}
