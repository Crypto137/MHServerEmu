using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Navi
{
    public class NaviCdt // Constrained Delaunay Triangulation
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public const float NaviSectorSize = 1024.0f;
        public int TriangleCount { get; private set; }
        public Aabb Bounds { get; private set; }
        private float _extentsSize;

        public InvasiveList<NaviTriangle> TriangleList { get; private set; }
        public uint Serial { get; set; }

        private NaviSystem _navi;
        private int _sectorSize;
        private NaviTriangle[] _sectors;
        private NaviVertexLookupCache _vertexLookupCache;
        private NaviTriangle _lastTriangle;
        private int _triangleCount;

        public NaviCdt(NaviSystem navi, NaviVertexLookupCache naviVertexLookupCache)
        {
            _navi = navi;
            TriangleCount = 0;
            Bounds = Aabb.Zero;
            TriangleList = new(1);
            _vertexLookupCache = naviVertexLookupCache;
            _sectors = Array.Empty<NaviTriangle>();
        }

        public void Create(Aabb bounds)
        {
            Release();
            Bounds = bounds;
            _extentsSize = Bounds.Extents.MaxElem();
            
            Vector3 boundsSize = Bounds.SizeVec;
            int sectorsX = (int)(boundsSize.X / NaviSectorSize) + 1;
            int sectorsY = (int)(boundsSize.Y / NaviSectorSize) + 1;

            _sectorSize = sectorsX;
            _sectors = new NaviTriangle[sectorsX * sectorsY];
            if (_navi.Log) Logger.Trace($"Navi Fast Triangle Lookup: {sectorsX}x{sectorsY} = {_sectors.Length} sectors");
        }

        public void Release()
        {
            foreach (var triangle in TriangleList.Iterate())
                RemoveTriangle(triangle);

            _lastTriangle = null;
            Serial = 0;
            _sectors = null;
            _sectorSize = 0;
        }

        internal void AddTriangle(NaviTriangle triangle)
        {
            _lastTriangle ??= triangle;

            TriangleList.AddBack(triangle);
            _triangleCount++;

            AddTriangleFastLookupRef(triangle);
        }

        private void AddTriangleFastLookupRef(NaviTriangle triangle)
        {
            int sectorIndex = PointToSectorIndex(triangle.Centroid());
            if (sectorIndex != -1) _sectors[sectorIndex] = triangle;
        }

        public void RemoveTriangle(NaviTriangle triangle)
        {
            RemoveTriangleFastLookupRef(triangle);
            triangle.Detach();

            TriangleList.Remove(triangle);
            _triangleCount--;

            if (_lastTriangle == triangle)
                _lastTriangle = TriangleList.Head();
        }

        private void RemoveTriangleFastLookupRef(NaviTriangle triangle)
        {
            int sectorIndex = PointToSectorIndex(triangle.Centroid());
            if (sectorIndex != -1 && triangle == _sectors[sectorIndex])
            {
                _sectors[sectorIndex] = null;
                for (int edgeIndex = 0; edgeIndex < 3; ++edgeIndex)
                {
                    NaviTriangle opposed = triangle.Edge(edgeIndex).OpposedTriangle(triangle);
                    if (opposed != null && PointToSectorIndex(opposed.Centroid()) == sectorIndex)
                    {
                        _sectors[sectorIndex] = opposed;
                        break;
                    }
                }
            }
        }

        private int PointToSectorIndex(Vector3 point)
        {
            Vector3 pos = point - Bounds.Min;
            int sectorIndex = (int)(pos.X / NaviSectorSize) + (int)(pos.Y / NaviSectorSize) * _sectorSize;
            if (sectorIndex >= 0 && sectorIndex < _sectors.Length)
                return sectorIndex;
            else
                return -1;
        }

        public void RemoveCollinearEdges()
        {
            List<NaviEdge> checkedEdges = new ();
            List<NaviEdge> collinearEdges = new ();
            var naviSerialCheck = new NaviSerialCheck(this);

            foreach (var triangle in TriangleList.Iterate())
                foreach (var edge in triangle.Edges)
                {
                    if (edge.Triangles[0] == null || edge.Triangles[1] == null) continue;
                    if (edge.TestOperationSerial(naviSerialCheck) == false) continue;
                    collinearEdges.Clear();
                    if (FindCollinearEdges(edge, collinearEdges, out NaviEdge addEdge))
                    {
                        foreach (var collinearEdge in collinearEdges)
                            collinearEdge.TestOperationSerial(naviSerialCheck);

                        checkedEdges.Add(edge);
                    }
                }

            foreach (var edge in checkedEdges)
            {
                if (edge.IsAttached == false) continue;
                collinearEdges.Clear();
                if (FindCollinearEdges(edge, collinearEdges, out NaviEdge addEdge))
                {
                    foreach (var collinearEdge in collinearEdges)
                        RemoveEdge(collinearEdge, true);

                    if (FindTriangleContainingVertex(addEdge.Points[0]) != null && FindTriangleContainingVertex(addEdge.Points[1]) != null)
                        AddEdge(addEdge);
                }
            }
        }

        private bool FindCollinearEdges(NaviEdge edge, List<NaviEdge> collinearEdges, out NaviEdge outEdge)
        {
            outEdge = null;

            if (edge.EdgeFlags.HasFlag(NaviEdgeFlags.Const) == false) return false;

            var points = new NaviPoint[2];
            var dir = Vector3.Normalize2D(edge.Point(1) - edge.Point(0));

            for (int i = 0; i < 2; i++)
            {
                NaviEdge itEdge = edge; 
                if (i == 1) dir = -dir;

                NaviPoint point = edge.Points[i];
                points[i] = point;

                while (itEdge != null)
                {
                    NaviEdge collinearEdge = null;
                    NaviTriangle nextTriangle = itEdge.Triangles[0];
                    NaviTriangle itEnd = nextTriangle;
                    bool found = false;
                    do
                    {
                        int oppIndex = nextTriangle.OpposedEdgeIndex(point);
                        NaviEdge nextEdge = nextTriangle.Edges[(oppIndex + 1) % 3];

                        if ((nextEdge != itEdge) && nextEdge.EdgeFlags.HasFlag(NaviEdgeFlags.Const))
                        {
                            if (found)
                            {
                                collinearEdge = null;
                                break;
                            }
                            else
                            {
                                found = true;
                            }

                            var testDir = Vector3.Normalize2D(point.Pos - nextEdge.OpposedPoint(point).Pos);
                            if (Vector3.Dot(dir, testDir) > 0.996f)
                                collinearEdge = nextEdge;
                        }

                        nextTriangle = nextTriangle.NextTriangleSharingPoint(point);
                    } while (nextTriangle != itEnd);

                    if (collinearEdge != null)
                    {
                        point = collinearEdge.OpposedPoint(point);
                        points[i] = point;
                        collinearEdges.Add(collinearEdge);
                    }

                    itEdge = collinearEdge;
                }
            }

            if (collinearEdges.Count > 0)
            {
                collinearEdges.Add(edge);
                outEdge = new NaviEdge (points[0], points[1], NaviEdgeFlags.Const, edge.PathingFlags);
                return true;
            }
            else
            {
                return false;
            }
        }

        private NaviTriangle FindTriangleContainingVertex(NaviPoint point)
        {
            var triangle = FindTriangleAtPoint(point.Pos);
            if (triangle != null)
            {
                if (triangle.Contains(point)) 
                    return triangle;
                else
                    foreach (var edge in triangle.Edges)
                    {
                        var oppoTriangle = edge.OpposedTriangle(triangle);
                        if (oppoTriangle != null && oppoTriangle.Contains(point))
                            return oppoTriangle;
                    }
            }
            return null;
        }

        private NaviTriangle FindTriangleAtPoint(Vector3 pos)
        {
            if (Vector3.IsFinite(pos) == false) return null;

            NaviTriangle triangle = _lastTriangle;
            var sectorIndex = PointToSectorIndex(pos);
            NaviTriangle sector = (sectorIndex != -1) ? _sectors[sectorIndex] : null;
            if (sector != null && sector.Flags.HasFlag(NaviTriangleFlags.Attached))
                triangle = sector;

            int loopCount = 50000;
            while (triangle != null && FindNextTriangleTowards(triangle, pos, ref triangle) && loopCount-- > 0)
            {
            }

            if (loopCount <= 0)
            {
                if (_navi.Log)
                {
                    Logger.Error($"FindTriangleAtPoint stuck in infinite loop. p={pos}");
                    loopCount = 20;
                    int loopCountLog = 1;
                    while (triangle != null && FindNextTriangleTowards(triangle, pos, ref triangle) && loopCount-- > 0)
                    {
                        Logger.Error($"{loopCountLog++}: t={triangle.ToStringWithIntegrity()}");
                    }
                }
                return null;
            }
            return triangle;
        }

        private static bool FindNextTriangleTowards(NaviTriangle triangle, Vector3 pos, ref NaviTriangle outTriangle)
        {
            NaviPoint p0 = triangle.PointCW(0);
            NaviPoint p1 = triangle.PointCW(1);
            NaviPoint p2 = triangle.PointCW(2);

            double l0 = Pred.LineRelationship2D(p0, p1, pos);
            double l1 = Pred.LineRelationship2D(p1, p2, pos);
            double l2 = Pred.LineRelationship2D(p2, p0, pos);

            if (NaviUtil.FindMaxValue(l0, l1, l2, out int edgeIndex) > 0.0)
            {
                NaviEdge edge = triangle.Edge(edgeIndex);
                outTriangle = edge.OpposedTriangle(triangle);
                return true;
            }
            else
            {
                outTriangle = null;
                return false;
            }
        }

        internal void AddEdge(NaviEdge e)
        {
            throw new NotImplementedException();
        }

        internal NaviPoint AddPointProjZ(Vector3 pos1)
        {
            throw new NotImplementedException();
        }

        internal NaviPoint AddPoint(Vector3 pos1)
        {
            throw new NotImplementedException();
        }

        internal void RemoveEdge(NaviEdge edge, bool check = true)
        {
            throw new NotImplementedException();
        }
    }
}