using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using System.Globalization;
using System.Text;

namespace MHServerEmu.Games.Navi
{
    public class NaviCdt // Constrained Delaunay Triangulation
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public const float NaviSectorSize = 1024.0f;
        public const float SplitEpsilonSq = 6.25f;
        public int TriangleCount { get; private set; }
        public Aabb Bounds { get; private set; }
        private float _extentsSize;

        public TriangleList TriangleList { get; private set; }
        public uint Serial { get; set; }

        private NaviSystem _navi;
        private int _sectorSize;
        private NaviTriangle[] _sectors;
        private NaviVertexLookupCache _vertexLookupCache;
        private NaviTriangle _lastTriangle;

        public NaviCdt(NaviSystem navi, NaviVertexLookupCache naviVertexLookupCache)
        {
            _navi = navi;
            TriangleCount = 0;
            Bounds = Aabb.Zero;
            TriangleList = new();
            _vertexLookupCache = naviVertexLookupCache;
            _sectors = Array.Empty<NaviTriangle>();
        }

        public void Create(in Aabb bounds)
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

        public void AddTriangle(NaviTriangle triangle)
        {
            _lastTriangle ??= triangle;

            TriangleList.AddBack(triangle);
            TriangleCount++;

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
            TriangleCount--;

            if (_lastTriangle == triangle)
                _lastTriangle = TriangleList.Head;
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
                    NaviTriangle endTriangle = nextTriangle;
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
                    } while (nextTriangle != endTriangle);

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

        public NaviTriangle FindTriangleContainingVertex(NaviPoint point)
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

        public NaviTriangle FindTriangleAtPoint(Vector3 pos)
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
                return false;
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

            Span<bool> degenerates = stackalloc bool[3];
            Span<bool> splits = stackalloc bool[3];

            for (int i = 0; i < 3; i++)
            {
                var pi0 = points[(i + 0) % 3];
                var pi1 = points[(i + 1) % 3];
                degenerates[i] = Pred.IsDegenerate(point, pi0, pi1, 1.0);

                var edge = triangle.Edges[i];
                bool edgeFull = edge.Triangles[0] != null && edge.Triangles[1] != null;

                splits[i] = edgeFull && edge.TestFlag(NaviEdgeFlags.Constraint) &&                                                                 
                    (degenerates[i] || (Pred.RobustLinePointDistanceSq2D(edge.Point(0), edge.Point(1), point.Pos) < SplitEpsilonSq));

                if (split == false && splits[i]) return false;

                if (edge.TestFlag(NaviEdgeFlags.Door))
                {
                    _navi.LogError("SplitTriangle: Cannot split door edges!", point);
                    return false;
                }
            }

            Stack<NaviTriangle> triStack = new ();

            NaviEdge[] pointEdges = {
                new (point, p0, NaviEdgeFlags.None),
                new (point, p1, NaviEdgeFlags.None),
                new (point, p2, NaviEdgeFlags.None)
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
                        var dep = new NaviEdge(point, degTriangle.OpposedVertex(edge), NaviEdgeFlags.None);

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
                    if (Segment.SegmentPointDistanceSq2D(edge.Points[0].Pos, edge.Points[1].Pos, point.Pos) < SplitEpsilonSq)
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

            NaviEdge newEdge = new (t0.OpposedVertex(edge), t1.OpposedVertex(edge), edge.EdgeFlags);

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

        public void AddEdge(NaviEdge edge)
        {          
            FixedDeque<NaviEdge> edges = new (256);
            edges.PushBack(edge);

            while (!edges.Empty)
            {
                edge = edges.PopFront();
                AddEdge(edge, edges);
            }
        }

        private void AddEdge(NaviEdge edge, FixedDeque<NaviEdge> edges)
        {
            NaviPoint p0 = edge.Points[0];
            NaviPoint p1 = edge.Points[1];

            if (p0 == p1) return;

            NaviTriangle triContainingPoint = FindTriangleContainingVertex(p0);
            if (triContainingPoint == null) return;

            Vector3 edgeDir = Vector3.Normalize2D(p1.Pos - p0.Pos);

            NaviTriangle triangle = null;
            NaviTriangle nextTriangle = triContainingPoint;
            NaviTriangle endTriangle = triContainingPoint;

            NaviPoint splitPoint;
            NaviEdge splitEdge = null;
            float splitEdgeDot = 0.0f;

            do
            {
                if (nextTriangle.Contains(p1))
                {
                    NaviEdge maskEdge = nextTriangle.FindEdge(p0, p1);
                    bool flip = (maskEdge.Points[0] != edge.Points[0]);
                    maskEdge.PathingFlags.Merge(edge.PathingFlags, flip);
                    maskEdge.SetFlag(edge.EdgeFlags & NaviEdgeFlags.Mask);
                    return;
                }

                int oppoEdgeIndex = nextTriangle.OpposedEdgeIndex(p0);
                NaviEdge nextEdge = nextTriangle.EdgeMod(oppoEdgeIndex + 1);
                Vector3 nextDir = Vector3.Normalize2D(nextEdge.OpposedPoint(p0).Pos - p0.Pos);
                float edgeDot = Vector3.Dot(edgeDir, nextDir);
                if (edgeDot > splitEdgeDot)
                {
                    splitPoint = nextEdge.OpposedPoint(p0);
                    if (Segment.SegmentPointDistanceSq2D(p0.Pos, p1.Pos, splitPoint.Pos) < SplitEpsilonSq)
                    {
                        splitEdge = nextEdge;
                        splitEdgeDot = edgeDot;
                    }
                }

                if (triangle == null)
                {
                    NaviEdge oppoEdge = nextTriangle.Edges[oppoEdgeIndex];
                    if (Segment.SegmentsIntersect2D(p0.Pos, p1.Pos, oppoEdge.Points[0].Pos, oppoEdge.Points[1].Pos))
                    {
                        triangle = nextTriangle;
                    }
                }

                nextTriangle = nextTriangle.NextTriangleSharingPoint(p0);

            } while (nextTriangle != endTriangle);

            if (splitEdge != null)
            {
                splitPoint = splitEdge.OpposedPoint(p0);
                edges.PushBack(new(p0, splitPoint, edge.EdgeFlags, edge.PathingFlags));
                edges.PushBack(new(splitPoint, p1, edge.EdgeFlags, edge.PathingFlags));
                return;
            }

            if (triangle == null)
            {
                _navi.LogError("Failed to find triangle containing edge for AddEdge operation. This can happen when degenerate triangles are in the mesh.", edge);
                return;
            }

            List<NaviEdge> pseudoList0 = new ();
            List<NaviEdge> pseudoList1 = new ();

            NaviPoint sidePoint0, sidePoint1;
            NaviPoint point = p0;

            NaviTriangleState triangleState = new (triangle);

            splitEdge = triangle.OpposedEdge(p0);

            if (splitEdge.TestFlag(NaviEdgeFlags.Constraint))
            {
                SplitEdge(splitEdge, edge, edges);
                return;
            }

            bool side = Pred.LineSide2D(p0, p1, splitEdge.Points[0].Pos) == false;
            
            sidePoint0 = splitEdge.Points[side ? 0 : 1];
            pseudoList0.Add(triangle.FindEdge(p0, sidePoint0));

            sidePoint1 = splitEdge.Points[side ? 1 : 0];
            pseudoList1.Insert(0, triangle.FindEdge(p0, sidePoint1));            

            Stack<NaviTriangle> triStack = new ();

            while (triangle.Contains(p1) == false)
            {
                var triOppo = triangle.OpposedTriangle(point, out splitEdge);

                if (splitEdge.TestFlag(NaviEdgeFlags.Constraint))
                {
                    SplitEdge(splitEdge, edge, edges);
                    return;
                }

                splitPoint = triangle.OpposedVertex(triOppo);

                if (splitPoint == p1)
                {
                    side = Pred.LineSide2D(p0, p1, splitEdge.Points[0].Pos) == false;

                    sidePoint0 = splitEdge.Points[side ? 0 : 1];
                    pseudoList0.Add(triOppo.FindEdge(p1, sidePoint0));

                    sidePoint1 = splitEdge.Points[side ? 1 : 0];
                    pseudoList1.Insert(0, triOppo.FindEdge(p1, sidePoint1));

                    triStack.Push(triangle);
                    triStack.Push(triOppo);
                    break;
                }

                if (Segment.SegmentPointDistanceSq2D(p0.Pos, p1.Pos, splitPoint.Pos) < SplitEpsilonSq)
                {
                    edges.PushBack(new(p0, splitPoint, edge.EdgeFlags, edge.PathingFlags));
                    edges.PushBack(new(splitPoint, p1, edge.EdgeFlags, edge.PathingFlags));
                    return;
                }

                side = Pred.LineSide2D(p0, p1, splitPoint.Pos) == false;
                if (side)
                {
                    pseudoList0.Add(triOppo.FindEdge(sidePoint0, splitPoint));
                    sidePoint0 = splitPoint;
                }
                else
                {
                    pseudoList1.Insert(0, triOppo.FindEdge(sidePoint1, splitPoint));
                    sidePoint1 = splitPoint;
                }

                if (Pred.LineSide2D(p0, p1, splitEdge.Points[0].Pos))
                    point = splitEdge.Points[side ? 1 : 0];
                else
                    point = splitEdge.Points[side ? 0 : 1];

                triStack.Push(triangle);
                triangle = triOppo;
            }

            while (triStack.Count > 0) RemoveTriangle(triStack.Pop());

            TriangulatepseudopolygonDelaunay(pseudoList0, p0, p1, edge, triangleState);
            TriangulatepseudopolygonDelaunay(pseudoList1, p1, p0, edge, triangleState);
        }

        private NaviEdge TriangulatepseudopolygonDelaunay(List<NaviEdge> pseudoList, NaviPoint p0, NaviPoint p1, NaviEdge edge, NaviTriangleState triangleState)
        {
            NaviEdge edge0 = null;
            NaviEdge edge1 = null;

            if (pseudoList.Count > 2)
            {
                NaviEdge edgeC = pseudoList[0];                
                NaviPoint pointC = (p0 != edgeC.Points[0]) ? edgeC.Points[0] : edgeC.Points[1];

                int indexC = 0;
                int count = pseudoList.Count;
                NaviPoint pointI = pointC;
                for (int i = 1; i < count - 1; i++)
                {
                    NaviEdge edgeI = pseudoList[i];
                    pointI = (pointI != edgeI.Points[0]) ? edgeI.Points[0] : edgeI.Points[1];

                    if (CircumcircleContainsPoint2D(p0, pointC, p1, pointI))
                    {
                        pointC = pointI;
                        indexC = i;
                    }
                }

                List<NaviEdge> pseudoList0 = new(pseudoList.GetRange(0, indexC + 1));
                pseudoList.RemoveRange(0, indexC + 1);

                List<NaviEdge> pseudoList1 = new(pseudoList);
                pseudoList.Clear();

                edge0 = TriangulatepseudopolygonDelaunay(pseudoList0, p0, pointC, null, triangleState);
                edge1 = TriangulatepseudopolygonDelaunay(pseudoList1, pointC, p1, null, triangleState);
            }

            if (pseudoList.Count == 2)
            {
                edge ??= new(p0, p1, 0);

                edge0 = pseudoList[0];
                pseudoList.RemoveAt(0);

                edge1 = pseudoList[0];
                pseudoList.RemoveAt(0);

                NaviTriangle triangle = new(edge, edge0, edge1);
                triangleState.RestoreState(triangle);
                AddTriangle(triangle);
            }
            else if (pseudoList.Count == 1)
            {
                edge = pseudoList[0];
                pseudoList.RemoveAt(0);
            }
            else if (pseudoList.Count == 0)
            {
                edge ??= new(p0, p1, 0);
                NaviTriangle triangle = new(edge, edge0, edge1);
                triangleState.RestoreState(triangle);
                AddTriangle(triangle);
            }

            return edge;
        }

        private void SplitEdge(NaviEdge splitEdge, NaviEdge edge, FixedDeque<NaviEdge> edges)
        {
            if (splitEdge.TestFlag(NaviEdgeFlags.Door))
                _navi.LogError("SplitEdge: Cannot split door edges!", edge);

            NaviPoint p0 = edge.Points[0];
            NaviPoint p1 = edge.Points[1];

            if(Segment.LineLineIntersect2D(splitEdge.Points[0].Pos, splitEdge.Points[1].Pos, p0.Pos, p1.Pos, out Vector3 intersectionPos) == false) return;

            var splitPoint = _vertexLookupCache.CacheVertex(intersectionPos, out _);

            if (splitPoint.TestFlag(NaviPointFlags.Attached) == false)
            {
                SplitTriangle(splitEdge.Triangles[0], splitPoint);
            }
            else if (splitEdge.Contains(splitPoint) == false)
            {
                edges.PushBack(new(splitEdge.Points[0], splitPoint, splitEdge.EdgeFlags, splitEdge.PathingFlags));
                edges.PushBack(new(splitPoint, splitEdge.Points[1], splitEdge.EdgeFlags, splitEdge.PathingFlags));
                RemoveEdge(splitEdge, false);
            }

            if (p0 != splitPoint) edges.PushBack(new(p0, splitPoint, edge.EdgeFlags, edge.PathingFlags));
            if (splitPoint != p1) edges.PushBack(new(splitPoint, p1, edge.EdgeFlags, edge.PathingFlags));
        }

        public void RemoveEdge(NaviEdge edge, bool check = true)
        {
            if (edge.TestFlag(NaviEdgeFlags.Door))
            {
                _navi.LogError("RemoveEdge: Cannot remove door edges!", edge);
                return;
            }

            edge.ClearFlag(NaviEdgeFlags.Constraint);
            edge.PathingFlags.Clear();

            var p0 = edge.Points[0];
            var p1 = edge.Points[1];

            Stack<NaviEdge> edgeStack = new ();
            edge.SetFlag(NaviEdgeFlags.Delaunay);
            edgeStack.Push(edge);

            CheckDelaunaySwap(edgeStack);

            if (check)
            {
                var t0 = FindTriangleContainingVertex(p0);
                if (t0 != null && NaviUtil.IsPointConstraint(p0, t0) == false)
                    RemovePoint(p0, t0);

                var t1 = FindTriangleContainingVertex(p1);
                if (t1 != null && NaviUtil.IsPointConstraint(p1, t1) == false)
                    RemovePoint(p1, t1);
            }
        }

        private void CheckDelaunaySwap(Stack<NaviEdge> edgeStack)
        {
            while (edgeStack.Count > 0)
            {
                NaviEdge edge = edgeStack.Pop();
                edge.ClearFlag(NaviEdgeFlags.Delaunay);
                if (edge.TestFlag(NaviEdgeFlags.Constraint)) continue;

                var t0 = edge.Triangles[0];
                var t1 = edge.Triangles[1];
                float at0 = Segment.SignedDoubleTriangleArea2D(t0.PointCW(0).Pos, t0.PointCW(1).Pos, t0.PointCW(2).Pos);
                float at1 = Segment.SignedDoubleTriangleArea2D(t1.PointCW(0).Pos, t1.PointCW(1).Pos, t1.PointCW(2).Pos);
                var triangle = (at0 > at1) ? t0 : t1;
                var oppoTriangle = edge.OpposedTriangle(triangle);
                NaviPoint checkPoint = oppoTriangle.OpposedVertex(edge);

                if (CircumcircleContainsPoint2D(triangle.PointCW(0), triangle.PointCW(1), triangle.PointCW(2), checkPoint))
                {
                    int edgeIndex0 = t0.EdgeIndex(edge);
                    int edgeIndex1 = t1.EdgeIndex(edge);

                    NaviEdge[] edges = {
                        t0.EdgeMod(edgeIndex0 + 1), t0.EdgeMod(edgeIndex0 + 2),
                        t1.EdgeMod(edgeIndex1 + 1), t1.EdgeMod(edgeIndex1 + 2)
                    };

                    foreach (var e in edges)
                        if (e.TestFlag(NaviEdgeFlags.Delaunay) == false)
                        {
                            e.SetFlag(NaviEdgeFlags.Delaunay);
                            edgeStack.Push(e);
                        }

                    SwapEdge(edge, out _, out _);
                }
            }
        }

        public void RemovePoint(NaviPoint point, NaviTriangle triangle)
        {
            NaviTriangleState triangleState = new (triangle);

            List<NaviEar> listEar = new ();
            FixedPriorityQueue<NaviEar> queueEar = new (512);

            NaviTriangle it = triangle;
            NaviTriangle nextTriangle;
            do
            {
                int oppoEdgeIndex = it.OpposedEdgeIndex(point);
                NaviEar ear = new()
                {
                    Point = it.EdgePointCW(oppoEdgeIndex, 0),
                    Edge = it.Edges[oppoEdgeIndex],
                    PrevIndex = listEar.Count - 1,
                    NextIndex = listEar.Count + 1
                };
                listEar.Add(ear);

                nextTriangle = it.NextTriangleSharingPoint(point);
                RemoveTriangle(it);
                it = nextTriangle;
            } while (it != null);

            listEar[0].PrevIndex = listEar.Count - 1;
            listEar[^1].NextIndex = 0;

            foreach (var ear in listEar)
            {
                ear.CalcPower(listEar, point.Pos);
                queueEar.Push(ear);
            }

            while (queueEar.Count > 3)
            {
                var ear = queueEar.Top;
                queueEar.Pop();

                var prevEar = listEar[ear.PrevIndex];
                var nextEar = listEar[ear.NextIndex];

                NaviEdge prevEdge = new(prevEar.Point, nextEar.Point, 0);
                NaviTriangle prevTri = new(prevEar.Edge, ear.Edge, prevEdge);
                triangleState.RestoreState(prevTri);
                AddTriangle(prevTri);

                prevEar.NextIndex = ear.NextIndex;
                nextEar.PrevIndex = ear.PrevIndex;

                prevEar.Edge = prevEdge;

                prevEar.CalcPower(listEar, point.Pos);
                nextEar.CalcPower(listEar, point.Pos);

                queueEar.Heapify();
            }

            var lastEar = queueEar.Top;
            var prevLastEar = listEar[lastEar.PrevIndex];
            var nextLastEar = listEar[lastEar.NextIndex];

            NaviTriangle lastTriangle = new(prevLastEar.Edge, lastEar.Edge, nextLastEar.Edge);
            triangleState.RestoreState(lastTriangle);
            AddTriangle(lastTriangle);

            point.ClearFlag(NaviPointFlags.Attached);
        }

        public void SaveHashTriangles(string fileName)
        {
            StringBuilder hashes = new();
            int id = 0;
            foreach (var triangle in TriangleList.Iterate())
                hashes.AppendLine($"[{id++}] {triangle.ToHashString()}");
            FileHelper.SaveTextFileToRoot(fileName, hashes.ToString());
        }

        public void SaveHashTriangles2(string fileName)
        {
            StringBuilder hashes = new();
            int id = 0;
            foreach (var triangle in TriangleList.Iterate())
                hashes.AppendLine($"[{id++}] {triangle.ToHashString2()}");
            FileHelper.SaveTextFileToRoot(fileName, hashes.ToString());
        }

        public void MeshToSvg(string name)
        {
            NaviSvgHelper svg = new(this);
            Stack<NaviPoint> influences = new();
            foreach (var triangle in TriangleList.Iterate())
                foreach (var edge in triangle.Edges)
                    foreach (var point in edge.Points)
                        if (point.InfluenceRadius > 0 && influences.Contains(point) == false)
                            influences.Push(point);
            foreach (var triangle in TriangleList.Iterate())
                svg.AddTriangle(triangle);
            foreach (var point in influences)
                svg.AddCircle(point.Pos, point.InfluenceRadius);
                svg.SaveToFile($"{name}-{DateTime.Now:mm-ss-fff}.svg");
        }

        public string MeshToObj(PathFlags filterFlags = PathFlags.Walk)
        {
            StringBuilder sb = new();
            Dictionary<ulong, int> idMap = new();
            
            // Vertices
            int newId = 1;
            foreach (var triangle in TriangleList.Iterate())
                if (triangle.PathingFlags.HasFlag(filterFlags))
                    foreach (var edge in triangle.Edges)
                        foreach (var point in edge.Points)
                            if (idMap.ContainsKey(point.Id) == false)
                            {
                                idMap.Add(point.Id, newId++);
                                sb.AppendLine($"v {point.Pos.Y.ToString(CultureInfo.InvariantCulture)} " +
                                                  $"{point.Pos.X.ToString(CultureInfo.InvariantCulture)} " +
                                                  $"{point.Pos.Z.ToString(CultureInfo.InvariantCulture)}");
                            }

            // Faces
            foreach (var triangle in TriangleList.Iterate())
                if (triangle.PathingFlags.HasFlag(filterFlags))
                {
                    var p0 = triangle.PointCW(0);
                    var p1 = triangle.PointCW(1);
                    var p2 = triangle.PointCW(2);
                    bool flip = Pred.SortInputs(ref p0, ref p1, ref p2);
                    if (!flip)
                        sb.AppendLine($"f {idMap[p2.Id]} " +
                                          $"{idMap[p1.Id]} " +
                                          $"{idMap[p0.Id]}");
                    else
                        sb.AppendLine($"f {idMap[p0.Id]} " +
                                          $"{idMap[p1.Id]} " +
                                          $"{idMap[p2.Id]}");
                }

            return sb.ToString();
        }

        public NaviPoint FindCachedPointAtPoint(Vector3 position)
        {
            return _vertexLookupCache.FindVertex(position);
        }

        public bool AttemptCheapVertexPositionUpdate(NaviTriangle triangle, NaviPoint point, Vector3 position)
        {
            var oldPos = point.Pos;
            point.Pos = position;
            bool checkFail = false;
            
            var nextTriangle = triangle;
            do
            {
                nextTriangle.OpposedTriangle(point, out NaviTriangle oppoTriangle, out NaviEdge oppoEdge);
                if (oppoTriangle != null)
                {
                    var checkPoint = oppoTriangle.OpposedVertex(oppoEdge);
                    var p0 = nextTriangle.PointCW(0);
                    var p1 = nextTriangle.PointCW(1);
                    var p2 = nextTriangle.PointCW(2);

                    if (Pred.IsDegenerate(p0, p1, p2) || Pred.CircumcircleContainsPoint(p0, p1, p2, checkPoint))
                    {
                        checkFail = true;
                        break;
                    }
                }
                nextTriangle = nextTriangle.NextTriangleSharingPoint(point);
            } while (nextTriangle != triangle);            

            if (checkFail)
            {
                point.Pos = oldPos;
                return false;
            }
            else
            {
                point.Pos = oldPos;                
                nextTriangle = triangle;
                do
                {
                    var sectorIndex = PointToSectorIndex(nextTriangle.Centroid());
                    if (sectorIndex != -1 && nextTriangle == _sectors[sectorIndex])
                        _sectors[sectorIndex] = null;
                    nextTriangle = nextTriangle.NextTriangleSharingPoint(point);
                } while (nextTriangle != triangle);

                _vertexLookupCache.UpdateVertex(point, NaviUtil.ProjectToPlane(triangle, position));                
                nextTriangle = triangle;
                do
                {
                    AddTriangleFastLookupRef(nextTriangle);
                    nextTriangle = nextTriangle.NextTriangleSharingPoint(point);
                } while (nextTriangle != triangle);                

                return true;
            }
        }

        public NaviPoint FindAttachedPointAtPoint(Vector3 position)
        {
            NaviPoint point = _vertexLookupCache.FindVertex(position);
            return point != null && point.TestFlag(NaviPointFlags.Attached) ? point : null;
        }

    }
}