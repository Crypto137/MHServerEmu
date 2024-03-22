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
                    NaviTriangle opposed = triangle.Edges[edgeIndex].OpposedTriangle(triangle);
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

            if (edge.TestFlag(NaviEdgeFlags.Constraint) == false) return false;

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
                        NaviEdge nextEdge = nextTriangle.EdgeMod(oppIndex + 1);

                        if ((nextEdge != itEdge) && nextEdge.TestFlag(NaviEdgeFlags.Constraint))
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
                outEdge = new NaviEdge (points[0], points[1], NaviEdgeFlags.Constraint, edge.PathingFlags);
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
            if (sector != null && sector.TestFlag(NaviTriangleFlags.Attached))
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
                NaviEdge edge = triangle.Edges[edgeIndex];
                outTriangle = edge.OpposedTriangle(triangle);
                return true;
            }
            else
            {
                outTriangle = null;
                return false;
            }
        }

        public NaviPoint AddPointProjZ(Vector3 pos, bool split = true)
        {
            NaviPoint point = _vertexLookupCache.CacheVertex(pos, out bool _);
            if (point.TestFlag(NaviPointFlags.Attached) == false)
            {
                NaviTriangle triangle = FindTriangleAtPoint(point.Pos);
                if (triangle == null)
                {
                    _navi.LogError("AddPointProjZ: Failed to find mesh feature at point (likely point is out of bounds).", point);
                    return null;
                }
                point.Pos = NaviUtil.ProjectToPlane(triangle, point.Pos);
                if (SplitTriangle(triangle, point, split) == false) return null;
            }
            return point;
        }

        public NaviPoint AddPoint(Vector3 pos)
        {
            NaviPoint point = _vertexLookupCache.CacheVertex(pos, out bool _);
            if (point.TestFlag(NaviPointFlags.Attached) == false)
            {
                NaviTriangle triangle = FindTriangleAtPoint(point.Pos);
                if (triangle == null)
                {
                    _navi.LogError("AddPoint: Failed to find mesh feature at point (likely point is out of bounds).", point);
                    return null;
                }
                if (SplitTriangle(triangle, point) == false) return null;
            }
            return point;
        }

        public bool SplitTriangle(NaviTriangle triangle, NaviPoint point, bool split = true)
        {
            var p0 = triangle.PointCW(0);
            var p1 = triangle.PointCW(1);
            var p2 = triangle.PointCW(2);
            NaviPoint[] points = { p0, p1, p2 };

            bool[] degenerates = new bool[3];
            bool[] splits = new bool[3];

            for (int i = 0; i < 3; i++)
            {
                var pi0 = points[(i + 0) % 3];
                var pi1 = points[(i + 1) % 3];
                degenerates[i] = Pred.IsDegenerate(point, pi0, pi1, 1.0);

                var edge = triangle.Edges[i];
                bool edgeFull = edge.Triangles[0] != null && edge.Triangles[1] != null;

                splits[i] = edgeFull && edge.TestFlag(NaviEdgeFlags.Constraint) &&                                                                 
                    (degenerates[i] || (Pred.RobustLinePointDistanceSq2D(edge.Point(0), edge.Point(1), point.Pos) < 6.25f));

                if (split == false && splits[i]) return false;

                if (edge.TestFlag(NaviEdgeFlags.Door))
                {
                    _navi.LogError("SplitTriangle: Cannot split door edges!", point);
                    return false;
                }
            }

            Stack<NaviTriangle> triStack = new ();

            NaviEdge[] pointEdges = {
                new (point, p0, 0, new()),
                new (point, p1, 0, new()),
                new (point, p2, 0, new())
            };

            NaviEdge[] triangleEdges = { triangle.Edges[0], triangle.Edges[1], triangle.Edges[2] };

            NaviTriangleState triangleState = new (triangle);
            RemoveTriangle(triangle);

            void PushStateTriangle(NaviTriangleState state, NaviEdge e0, NaviEdge e1, NaviEdge e2)
            {
                NaviTriangle tri = new(e0, e1, e2);
                state.RestoreState(tri);
                AddTriangle(tri);
                triStack.Push(tri);
            }

            for (int i = 0; i < 3; ++i)
            {
                int i0 = (i + 0) % 3;
                int i1 = (i + 1) % 3;

                var pe0 = pointEdges[i0];
                var pe1 = pointEdges[i1];
                var edge = triangleEdges[i];
                if (splits[i])
                {
                    pe0.ConstraintMerge(edge);
                    pe1.ConstraintMerge(edge);
                    edge.ClearFlag(NaviEdgeFlags.Constraint);
                    edge.PathingFlags.Clear();
                }

                if (degenerates[i])
                {
                    NaviTriangle degTriangle = edge.Triangles[0] ?? edge.Triangles[1];
                    var oppoPoint = degTriangle.OpposedVertex(edge);

                    if (Pred.Clockwise2D(point, points[i0], oppoPoint) && 
                        Pred.Clockwise2D(point, oppoPoint, points[i1]))
                    {
                        int edgeIndex = degTriangle.EdgeIndex(edge);
                        var de1 = degTriangle.EdgeMod(edgeIndex + 1);
                        var de2 = degTriangle.EdgeMod(edgeIndex + 2);
                        var dep = new NaviEdge(point, degTriangle.OpposedVertex(edge), 0, new());

                        NaviTriangleState triangleStateDeg = new (degTriangle);
                        RemoveTriangle(degTriangle);

                        PushStateTriangle(triangleStateDeg, pe0, de1, dep);
                        PushStateTriangle(triangleStateDeg, pe1, dep, de2);
                    }
                    else
                        PushStateTriangle(triangleState, pe0, edge, pe1);
                }
                else
                    PushStateTriangle(triangleState, pe0, edge, pe1);
            }

            point.SetFlag(NaviPointFlags.Attached);
            CheckDelaunaySwap(triStack, point, split);

            return true;
        }

        private void CheckDelaunaySwap(Stack<NaviTriangle> triStack, NaviPoint point, bool split)
        {
            while (triStack.Count > 0)
            {
                var triangle = triStack.Pop();

                var oppoTriangle = triangle.OpposedTriangle(point, out NaviEdge edge);
                if (oppoTriangle == null) continue;

                if (split && edge.TestFlag(NaviEdgeFlags.Constraint))
                {
                    if (Segment.SegmentPointDistanceSq2D(edge.Points[0].Pos, edge.Points[1].Pos, point.Pos) < 6.25f)
                    {
                        int edgeIndex = triangle.EdgeIndex(edge);
                        var e2 = triangle.EdgeMod(edgeIndex + 2);
                        var e1 = triangle.EdgeMod(edgeIndex + 1);

                        e2.ConstraintMerge(edge);
                        e1.ConstraintMerge(edge);
                        edge.ClearFlag(NaviEdgeFlags.Constraint);
                        edge.PathingFlags.Clear();
                    }
                }

                if (edge.TestFlag(NaviEdgeFlags.Constraint) == false)
                {
                    NaviPoint checkPoint = oppoTriangle.OpposedVertex(edge);
                    if (CircumcircleContainsPoint2D(triangle.PointCW(0), triangle.PointCW(1), triangle.PointCW(2), checkPoint))
                    {
                        SwapEdge(edge, out NaviTriangle t0, out NaviTriangle t1);

                        triStack.Push(t0);
                        triStack.Push(t1);
                    }
                }
            }
        }

        private static bool CircumcircleContainsPoint2D(NaviPoint p0, NaviPoint p1, NaviPoint p2, NaviPoint checkPoint)
        {
            return Pred.CircumcircleContainsPoint(p0, p1, p2, checkPoint);
        }

        private void SwapEdge(NaviEdge edge, out NaviTriangle outTri0, out NaviTriangle outTri1)
        {
            NaviTriangle t0 = edge.Triangles[0];
            NaviTriangle t1 = edge.Triangles[1];

            NaviEdge newEdge = new (t0.OpposedVertex(edge), t1.OpposedVertex(edge), edge.EdgeFlags, new());

            int edgeIndex0 = t0.EdgeIndex(edge);
            int edgeIndex1 = t1.EdgeIndex(edge);

            var t0e1 = t0.EdgeMod(edgeIndex0 + 1);
            var t0e2 = t0.EdgeMod(edgeIndex0 + 2);
            var t1e1 = t1.EdgeMod(edgeIndex1 + 1);
            var t1e2 = t1.EdgeMod(edgeIndex1 + 2);

            NaviTriangleState state0 = new (t0);
            NaviTriangleState state1 = new (t1);

            RemoveTriangle(t0);
            RemoveTriangle(t1);

            outTri0 = new (newEdge, t0e2, t1e1);
            state0.RestoreState(outTri0);

            outTri1 = new(newEdge, t1e2, t0e1);
            state1.RestoreState(outTri1);

            AddTriangle(outTri0);
            AddTriangle(outTri1);
        }

        internal void AddEdge(NaviEdge edge)
        {
            throw new NotImplementedException();
        }

        internal void RemoveEdge(NaviEdge edge, bool check = true)
        {
            throw new NotImplementedException();
        }
    }
}