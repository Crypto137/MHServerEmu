using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Navi
{
    public class NaviPathGenerator
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private NaviMesh _naviMesh;
        private float _radius;
        private float _width;
        private PathFlags _pathFlags;
        private Vector3 _startPosition;
        private Vector3 _goalPosition;
        private PathGenerationFlags _pathGenerationFlags;
        private float _incompleteDistance;
        private NaviTriangle _startTriangle;
        private NaviTriangle _goalTriangle;
        private NaviPoint _goalPoint;
        private readonly FixedPriorityQueue<NaviPathSearchState> _searchStateQueue;

        public NaviPathGenerator(NaviMesh naviMesh)
        {
            // _navi = naviMesh.NaviSystem; // not used
            _naviMesh = naviMesh;
            _searchStateQueue = new(128);
        }

        public static void GenerateDirectMove(Vector3 startPosition, Vector3 goalPosition, List<NaviPathNode> pathNodes)
        {
            AddPathNodeBack(pathNodes, startPosition, NaviSide.Point, 0f, 0f);
            AddPathNodeBack(pathNodes, goalPosition, NaviSide.Point, 0f, 0f);
        }

        private static void AddPathNodeBack(List<NaviPathNode> pathNodes, Vector3 position, NaviSide side, float radius, float influenceRadius)
        {
            if (pathNodes.Count < 256) // max_size
            {
                NaviPathNode pathNode = new ( position, side,
                    side != NaviSide.Point ? radius + influenceRadius : 0f, 
                    influenceRadius > 0f);
                pathNodes.Add(pathNode);
            }
        }

        public NaviPathResult GeneratePath(Vector3 startPosition, Vector3 goalPosition, float radius, PathFlags pathFlags, List<NaviPathNode> pathNodes, bool skipGen, PathGenerationFlags pathGenerationFlags, float incompleteDistance)
        {
            if (skipGen && pathGenerationFlags.HasFlag(PathGenerationFlags.IncompletedPath))
            {
                Logger.Debug("Path testing and incomplete path generation flags are incompatible.");
                return NaviPathResult.Failed;
            }
            _radius = radius;
            _width = 2.0f * radius;
            _pathFlags = pathFlags;
            _startPosition = new(startPosition);
            _goalPosition = new(goalPosition);
            _pathGenerationFlags = pathGenerationFlags;
            _incompleteDistance = incompleteDistance;
            if (!_naviMesh.IsMeshValid)  
                return NaviPathResult.FailedNaviMesh;

            var naviCDT = _naviMesh.NaviCdt;
            _startTriangle = naviCDT.FindTriangleAtPoint(startPosition);
            _goalTriangle = naviCDT.FindTriangleAtPoint(goalPosition);
            _goalPoint = naviCDT.FindAttachedPointAtPoint(goalPosition);
            NaviPathResult result = FixInvalidGoalPosition();
            if (result != NaviPathResult.Success)
                return result;

            return GeneratePathInternal(pathNodes, skipGen);
        }

        private NaviPathResult GeneratePathInternal(List<NaviPathNode> outPathNodes, bool skipGen)
        {
            if (_startTriangle == null || _goalTriangle == null) return NaviPathResult.FailedTriangle;

            _startPosition = NaviUtil.ProjectToPlane(_startTriangle, _startPosition);
            _goalPosition = NaviUtil.ProjectToPlane(_goalTriangle, _goalPosition);
            _searchStateQueue.Clear();

            if (_goalTriangle.TestPathFlags(_pathFlags) == false) return NaviPathResult.FailedTriangle;

            float influenceRadius = 0.0f;
            if (_goalPoint != null)
            {
                influenceRadius = _goalPoint.InfluenceRadius;
                _goalPoint.InfluenceRadius = 0.0f;
            }

            bool pathFound = false;
            bool incompletePath = false;
            List<NaviPathNode> tempPath;
            bool startTriangleIsGoal = (_startTriangle == _goalTriangle) || (_goalPoint != null && _startTriangle.Contains(_goalPoint));
            if (startTriangleIsGoal == false || CanCrossTriangle(_startTriangle, _startPosition, _goalPosition, _width) == false)
            {
                NaviPathSearchState state = new()
                {
                    Triangle = _startTriangle,
                    DistDone = 0,
                    DistLeft = Vector3.Distance2D(_goalPosition, _startPosition)
                };
                state.Distance = state.DistDone + state.DistLeft;
                _searchStateQueue.Push(state);

                NaviPathSearchState closestPathState = (_incompleteDistance == 0.0f || state.DistLeft <= _incompleteDistance) ? state : null;

                int maxAttempts = 1;
                int attempt = 0;
                int steps = 0;
                float shortestPathDistance = -1.0f;

                while (GeneratePathStep(out NaviPathSearchState genPathState) && _searchStateQueue.Count < 128 && ++steps < 256)
                {
                    if (genPathState != null)
                    {
                        pathFound = true;
                        if (skipGen) break;
                        if (shortestPathDistance < 0.0f)
                        {
                            NaviPathChannel shortestPathChannel = new (256);
                            CopySearchStateToPathChannel(genPathState, shortestPathChannel);
                            AddPathNodeBack(outPathNodes, _startPosition, NaviSide.Point, _radius, 0.0f);
                            if (FunnelStep(shortestPathChannel, outPathNodes) == false)
                                throw new InvalidOperationException("FunnelStep failed.");
                            shortestPathDistance = NaviPath.CalcAccurateDistance(outPathNodes);
                            maxAttempts = steps <= 5 ? 1 : steps <= 50 ? 3 : 5;
                        }
                        else
                        {
                            NaviPathChannel tempPathChannel = new (256);
                            CopySearchStateToPathChannel(genPathState, tempPathChannel);
                            tempPath = new(256);
                            AddPathNodeBack(tempPath, _startPosition, NaviSide.Point, _radius, 0.0f);
                            if (FunnelStep(tempPathChannel, tempPath) == false)
                                throw new InvalidOperationException("FunnelStep failed.");
                            float tempPathDistance = NaviPath.CalcAccurateDistance(tempPath);
                            if (tempPathDistance < shortestPathDistance)
                            {
                                shortestPathDistance = tempPathDistance;
                                outPathNodes.Clear();
                                outPathNodes.AddRange(tempPath);
                            }
                        }
                        steps = 0;
                        if (++attempt == maxAttempts) break;
                    }
                    else if (_pathGenerationFlags.HasFlag(PathGenerationFlags.IncompletedPath) && !_searchStateQueue.Empty)
                    {
                        NaviPathSearchState topState = _searchStateQueue.Top;
                        const float weight = 4.0f;
                        if (_incompleteDistance == 0.0f)
                        {
                            float weightDist = topState.DistLeft * weight + topState.DistDone;
                            float closestWeightDist = closestPathState.DistLeft * weight + closestPathState.DistDone;
                            if (weightDist < closestWeightDist)
                                closestPathState = topState;
                        }
                        else if (topState.DistLeft <= _incompleteDistance)
                        {
                            if (closestPathState != null)
                            {
                                float weightDist = topState.DistLeft * weight + topState.DistDone;
                                float closestWeightDist = closestPathState.DistLeft * weight + closestPathState.DistDone;
                                if (weightDist < closestWeightDist)
                                    closestPathState = topState;
                            }
                            else
                                closestPathState = topState;
                        }
                    }
                }

                if (_pathGenerationFlags.HasFlag(PathGenerationFlags.IncompletedPath) && !pathFound && closestPathState != null)
                {
                    NaviPathChannel tempPathChannel = new(256);
                    if (closestPathState.ParentState != null)
                        CopySearchStateToPathChannel(closestPathState, tempPathChannel);
                    AddPathNodeBack(outPathNodes, _startPosition, NaviSide.Point, _radius, 0.0f);
                    if (FunnelStep(tempPathChannel, outPathNodes) == false)
                        throw new InvalidOperationException("FunnelStep failed.");

                    int nodes = outPathNodes.Count;
                    if (nodes >= 2)
                    {
                        int index0 = nodes - 2;
                        int index1 = nodes - 1;
                        while (index0 > 0 && outPathNodes[index0].HasInfluence)
                            index0--;

                        Segment pathSegment = NaviPath.GetPathSegment(outPathNodes[index0], outPathNodes[index1]);
                        Vector3 resultPosition = new();
                        Vector3 resultNormal = null;
                        if (_pathGenerationFlags.HasFlag(PathGenerationFlags.IgnoreSweep) 
                            || _naviMesh.Sweep(pathSegment.Start, pathSegment.End, Math.Max(0.0f, _radius - 0.1f), _pathFlags,
                            ref resultPosition, ref resultNormal) == SweepResult.Success)
                            incompletePath = true;
                        else
                            outPathNodes.Clear();
                    }
                }
            }
            else
            {
                pathFound = true;
                if (skipGen == false)
                    GenerateDirectMove(_startPosition, _goalPosition, outPathNodes);
            }

            if (_goalPoint != null) _goalPoint.InfluenceRadius = influenceRadius;

            if (pathFound) return NaviPathResult.Success;
            else if (incompletePath) return NaviPathResult.IncompletedPath;
            else return NaviPathResult.FailedNoPathFound;
        }

        private bool GeneratePathStep(out NaviPathSearchState resultPathState)
        {
            resultPathState = null;
            if (_searchStateQueue.Empty) return false;

            NaviPathSearchState topState = _searchStateQueue.Top;
            _searchStateQueue.Pop();

            NaviTriangle triangle;
            for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
            {
                NaviEdge edge = topState.Triangle.Edges[edgeIndex];
                if (topState.Edge == edge) continue;

                triangle = edge.OpposedTriangle(topState.Triangle);
                if (triangle == null || !triangle.TestPathFlags(_pathFlags) || NaviEdge.IsBlockingDoorEdge(edge, _pathFlags)) 
                    continue;

                bool isGoalTriangle = (triangle == _goalTriangle) || (_goalPoint != null && triangle.Contains(_goalPoint));
                if (!isGoalTriangle && topState.IsAncestor(triangle)) continue;

                float edgeWidth = edge.Length2D() - edge.Points[0].InfluenceRadius - edge.Points[1].InfluenceRadius;
                if (_width >= edgeWidth) continue;

                if (topState.Edge != null)
                {
                    float triangleWidth = topState.Triangle.CalculateWidth(topState.Edge, edge);
                    if (_width >= triangleWidth) continue;
                }
                else
                {
                    if (CanCrossTriangle(_startTriangle, edgeIndex, edge, _startPosition, _width) == false) 
                        continue;
                }

                NaviPathSearchState state = new()
                {
                    ParentState = topState,
                    Triangle = triangle,
                    Edge = edge
                };

                Vector3 closestPoint = Segment.SegmentPointClosestPoint(edge.Points[0].Pos, edge.Points[1].Pos, _goalPosition);
                state.DistLeft = Vector3.Length2D(_goalPosition - closestPoint);

                float dist1 = Vector3.Length2D(_startPosition - closestPoint);
                float arcDist = topState.Edge != null ? NaviUtil.TriangleArcPathDistance(topState.Edge, edge, _radius) : 0.0f;
                float dist2 = topState.DistDone + arcDist;
                float dist3 = topState.DistDone + (topState.DistLeft - state.DistLeft);
                state.DistDone = Math.Max(dist1, Math.Max(dist2, dist3));
                state.Distance = state.DistDone + state.DistLeft;

                if (isGoalTriangle && CanCrossTriangle(triangle, triangle.EdgeIndex(edge), edge, _goalPosition, _width))
                {
                    resultPathState = state;
                    break;
                }
                else if (isGoalTriangle == false)
                    _searchStateQueue.Push(state);
            }
            return true;
        }

        private static bool CanCrossTriangle(NaviTriangle triangle, Vector3 startPosition, Vector3 goalPosition, float width)
        {
            var vert = new Vector3[3];
            for (int index = 0; index < 3; index++)
                vert[index] = triangle.PointCW(index).Pos;

            var isObtuse = new bool[3];
            isObtuse[0] = NaviUtil.IsAngleObtuse2D(vert[2], vert[0], vert[1]);
            isObtuse[1] = NaviUtil.IsAngleObtuse2D(vert[0], vert[1], vert[2]);
            isObtuse[2] = NaviUtil.IsAngleObtuse2D(vert[1], vert[2], vert[0]);

            for (int index = 0; index < 3; index++)
            {
                int i0 = index;
                int i1 = (index + 2) % 3;
                int i2 = (index + 1) % 3;
                var pos0 = vert[i0];
                var pos1 = vert[i1];
                var pos2 = vert[i2];

                if (!isObtuse[i1] && !isObtuse[i2])
                {
                    Vector3 projectPoint = Vector3.Project(pos1, pos2, pos0);
                    bool startSide = Segment.LinePointSide2D(pos0, projectPoint, startPosition) < 0.0f;
                    bool goalSide = Segment.LinePointSide2D(pos0, projectPoint, goalPosition) < 0.0f;
                    if (startSide != goalSide)
                    {
                        float ignoreWidth = triangle.CalculateWidthIgnoreInitialEdges(triangle.EdgeMod(index + 2), triangle.Edges[index]);
                        if (width >= ignoreWidth)
                            return false;
                    }
                }
            }
            return true;
        }

        private static bool CanCrossTriangle(NaviTriangle triangle, int edgeIndex, NaviEdge edge, Vector3 startPosition, float width)
        {
            var pos2 = triangle.OpposedVertex(edge).Pos;
            var pos0 = triangle.EdgePointCW(edgeIndex, 0).Pos;
            var pos1 = triangle.EdgePointCW(edgeIndex, 1).Pos;

            bool isObtuse0 = NaviUtil.IsAngleObtuse2D(pos2, pos0, pos1);
            bool isObtuse1 = NaviUtil.IsAngleObtuse2D(pos0, pos1, pos2);
            bool isObtuse2 = NaviUtil.IsAngleObtuse2D(pos1, pos2, pos0);

            if (!isObtuse2 && !isObtuse1)
            {
                Vector3 projectPoint = Vector3.Project(pos1, pos2, pos0);
                bool startSide = Segment.LinePointSide2D(pos0, projectPoint, startPosition) < 0.0f;
                bool pointSide1 = Segment.LinePointSide2D(pos0, projectPoint, pos1) < 0.0f;
                if (startSide != pointSide1)
                {
                    float ignoreWidth = triangle.CalculateWidthIgnoreInitialEdges(triangle.EdgeMod(edgeIndex + 2), edge);
                    if (width >= ignoreWidth)
                        return false;
                }
            }
            else if (!isObtuse2 && !isObtuse0)
            {
                Vector3 projectPoint = Vector3.Project(pos0, pos2, pos1);
                bool startSide = Segment.LinePointSide2D(pos1, projectPoint, startPosition) < 0.0f;
                bool pointSide0 = Segment.LinePointSide2D(pos1, projectPoint, pos0) < 0.0f;
                if (startSide != pointSide0)
                {
                    float ignoreWidth = triangle.CalculateWidthIgnoreInitialEdges(triangle.EdgeMod(edgeIndex + 1), edge);
                    if (width >= ignoreWidth)
                        return false;
                }
            }

            return true;
        }

        private bool FunnelStep(NaviPathChannel pathChannel, List<NaviPathNode> outPathNodes)
        {
            NaviPoint startPoint = new(_startPosition);
            NaviPoint goalPoint = new(_goalPosition);
            NaviSide vertexSide;
            float radiusSq = _radius * _radius;
            NaviFunnel funnel = new (startPoint);

            if (pathChannel.Count > 0)
            {
                int index = pathChannel.Count - 1;
                NaviChannelEdge firstChannelEdge = pathChannel[index];

                NaviPoint leftEnd = firstChannelEdge.LeftEndPoint();
                vertexSide = SimpleTestFunnelVertexClearOfObstacles(leftEnd, firstChannelEdge.Edge.Triangles[0], radiusSq, _pathFlags) ? NaviSide.Left : NaviSide.Point;
                FunnelAddSide(funnel, leftEnd, NaviSide.Left, vertexSide, outPathNodes);

                NaviPoint rightEnd = firstChannelEdge.RightEndPoint();
                vertexSide = SimpleTestFunnelVertexClearOfObstacles(rightEnd, firstChannelEdge.Edge.Triangles[0], radiusSq, _pathFlags) ? NaviSide.Point : NaviSide.Right;
                FunnelAddSide(funnel, rightEnd, NaviSide.Right, vertexSide, outPathNodes);

                while (--index >= 0)
                {
                    NaviChannelEdge channelEdge = pathChannel[index];
                    NaviPoint leftNext = channelEdge.LeftEndPoint();
                    NaviPoint rightNext = channelEdge.RightEndPoint();

                    if (leftNext == leftEnd)
                    {
                        rightEnd = rightNext;
                        vertexSide = SimpleTestFunnelVertexClearOfObstacles(rightEnd, channelEdge.Edge.Triangles[0], radiusSq, _pathFlags) ? NaviSide.Point : NaviSide.Right;
                        FunnelAddSide(funnel, rightEnd, NaviSide.Right, vertexSide, outPathNodes);
                    }
                    else
                    {
                        leftEnd = leftNext;
                        vertexSide = SimpleTestFunnelVertexClearOfObstacles(leftEnd, channelEdge.Edge.Triangles[0], radiusSq, _pathFlags) ? NaviSide.Left : NaviSide.Point;
                        FunnelAddSide(funnel, leftEnd, NaviSide.Left, vertexSide, outPathNodes);
                    }
                }
            }

            FunnelAddSide(funnel, goalPoint, NaviSide.Right, NaviSide.Point, outPathNodes);

            if (funnel.IsEmpty) return false;
            while (funnel.Left != funnel.Apex) funnel.PopLeft();
            while (funnel.Size > 0)
            {
                FunnelAddApexToPath(funnel, outPathNodes);
                funnel.PopApexLeft();
            }

            return true;
        }

        private void FunnelAddApexToPath(NaviFunnel funnel, List<NaviPathNode> outPathNodes)
        {
            if (funnel.IsApexPathStart == false)
                AddPathNodeBack(outPathNodes, funnel.Apex.Pos, funnel.ApexSide, _radius, funnel.Apex.InfluenceRadius);
        }

        private static bool SimpleTestFunnelVertexClearOfObstacles(NaviPoint point, NaviTriangle triangle, float radiusSq, PathFlags pathFlags)
        {
            if (point.InfluenceRef > 0) return false;
            NaviTriangle nextTriangle = triangle;
            do
            {
                if (nextTriangle == null || nextTriangle.TestPathFlags(pathFlags) == false) return false;
                var edge = nextTriangle.OpposedEdge(point);
                float distanceSq = Segment.SegmentPointDistanceSq(edge.Points[0].Pos, edge.Points[1].Pos, point.Pos);
                if (distanceSq < radiusSq) return false;
                nextTriangle = nextTriangle.NextTriangleSharingPoint(point);
            } while (nextTriangle != triangle);

            return true;
        }

        private void FunnelAddSide(NaviFunnel funnel, NaviPoint point, NaviSide funnelSide, NaviSide vertexSide, List<NaviPathNode> outPathNodes)
        {
            Vector3 prevPoint, cutPoint;

            while (funnel.IsEmpty == false)
            {
                if (funnel.Left == funnel.Right)
                {
                    funnel.AddVertex(funnelSide, point, vertexSide);
                    break;
                }

                if (funnel.Vertex(funnelSide) == funnel.Apex)
                {
                    cutPoint = GetOffsetSegment(funnel.Vertex(funnelSide), funnel.ApexSide, point, vertexSide, _radius);
                    NaviSide invertedSide = (funnelSide == NaviSide.Left) ? NaviSide.Right : NaviSide.Left;
                    prevPoint = GetOffsetSegment(funnel.Vertex(funnelSide), funnel.ApexSide, funnel.VertexPrev(funnelSide), invertedSide, _radius);
                }
                else if (funnel.VertexPrev(funnelSide) == funnel.Apex)
                {
                    cutPoint = GetOffsetSegment(funnel.Vertex(funnelSide), funnelSide, point, vertexSide, _radius);
                    prevPoint = GetOffsetSegment(funnel.VertexPrev(funnelSide), funnel.ApexSide, funnel.Vertex(funnelSide), funnelSide, _radius);
                }
                else
                {
                    cutPoint = GetOffsetSegment(funnel.Vertex(funnelSide), funnelSide, point, vertexSide, _radius);
                    prevPoint = GetOffsetSegment(funnel.VertexPrev(funnelSide), funnelSide, funnel.Vertex(funnelSide), funnelSide, _radius);
                }

                bool cross = Segment.Cross2D(prevPoint, cutPoint) < 0.0f;
                if (cross ^ (funnelSide == NaviSide.Left))
                {
                    funnel.AddVertex(funnelSide, point, vertexSide);
                    break;
                }

                if (funnel.Vertex(funnelSide) == funnel.Apex)
                {
                    FunnelAddApexToPath(funnel, outPathNodes);
                    var distSide = Vector3.DistanceSquared2D(funnel.Vertex(funnelSide).Pos, funnel.VertexPrev(funnelSide).Pos);
                    if (Vector3.DistanceSquared2D(funnel.Apex.Pos, point.Pos) < distSide)
                    {
                        funnel.AddApex(funnelSide, point, vertexSide);
                        break;
                    }
                    else
                        funnel.PopApex(funnelSide);
                }
                else
                    funnel.PopVertex(funnelSide);
            }
        }

        private static Vector3 GetOffsetSegment(NaviPoint point0, NaviSide vertexSide0, NaviPoint point1, NaviSide vertexSide1, float radius)
        {
            Segment offset = GetOffsetSegmentPts(point0, vertexSide0, point1, vertexSide1, radius);
            return offset.Direction;
        }

        private static Segment GetOffsetSegmentPts(NaviPoint point0, NaviSide vertexSide0, NaviPoint point1, NaviSide vertexSide1, float radius)
        {
            Vector3 perpVect = Vector3.Normalize(Vector3.Perp2D(point1.Pos - point0.Pos));
            Vector3 perpVect0 = perpVect * (radius + point0.InfluenceRadius);
            Vector3 perpVect1 = perpVect * (radius + point1.InfluenceRadius);
            
            Vector3 offset0 = null;
            switch (vertexSide0)
            {
                case NaviSide.Point: offset0 = point0.Pos; break;
                case NaviSide.Left: offset0 = point0.Pos + perpVect0; break;
                case NaviSide.Right: offset0 = point0.Pos - perpVect0; break;
            };

            Vector3 offset1 = null;
            switch (vertexSide1)
            {
                case NaviSide.Point: offset1 = point1.Pos; break;
                case NaviSide.Left: offset1 = point1.Pos + perpVect1; break;
                case NaviSide.Right: offset1 = point1.Pos - perpVect1; break;
            };

            return new(offset0, offset1);
        }

        private static void CopySearchStateToPathChannel(NaviPathSearchState searchState, NaviPathChannel pathChannel)
        {
            pathChannel.Clear();
            NaviPoint point0 = null;
            NaviPoint point1 = null;
            if (searchState != null)
            {
                NaviTriangle triangle = searchState.Triangle;
                NaviEdge edge = searchState.Edge;
                int edgeIndex = triangle.EdgeIndex(edge);
                point0 = triangle.EdgePointCW(edgeIndex, 0);
                point1 = triangle.EdgePointCW(edgeIndex, 1);
                NaviChannelEdge channelEdge = new()
                {
                    Edge = edge,
                    Flip = edge.Points[0] != point0
                };
                pathChannel.Add(channelEdge);
                searchState = searchState.ParentState;
            }

            while (searchState != null && searchState.Edge != null)
            {
                NaviChannelEdge channelEdge = new ();
                NaviEdge edge = searchState.Edge;
                channelEdge.Edge = edge;
                if (edge.Points[0] == point0)
                {
                    point1 = edge.Points[1];
                    channelEdge.Flip = false;
                }
                else if (edge.Points[1] == point0)
                {
                    point1 = edge.Points[0];
                    channelEdge.Flip = true;
                }
                else if (edge.Points[1] == point1)
                {
                    point0 = edge.Points[0];
                    channelEdge.Flip = false;
                }
                else
                {
                    point0 = edge.Points[1];
                    channelEdge.Flip = true;
                }
                pathChannel.Add(channelEdge);
                searchState = searchState.ParentState;
            }
        }

        private NaviPathResult FixInvalidGoalPosition()
        {
            if (_startTriangle == null || _goalTriangle == null) 
                return NaviPathResult.FailedTriangle;

            if (_goalTriangle.TestPathFlags(_pathFlags) == false)
            {
                Vector3 rayStart = _goalPosition;
                Segment.SafeNormalAndLength2D(_startPosition - _goalPosition, out Vector3 rayDirection, out float distance, Vector3.XAxis);
                NaviTriangle triangle = _goalTriangle;
                while (triangle != null)
                {
                    int edgeIndex = -1;
                    float minDistance = float.MaxValue;
                    for (int index = 0; index < 3; index++)
                    {
                        float relation = triangle.GetRelationOfEdgeToPoint(_startPosition, index);
                        if (relation > 0.0f)
                        {
                            NaviEdge edge = triangle.Edges[index];
                            if (Segment.RayLineIntersect2D(rayStart, rayDirection, edge.Points[0].Pos, edge.Points[1].Pos - edge.Points[0].Pos,
                                out float rayDistance, out _) && (rayDistance >= 0.0f) && (rayDistance < minDistance))
                            {
                                edgeIndex = index;
                                minDistance = rayDistance;
                            }
                        }
                    }

                    if (edgeIndex != -1 && (minDistance < distance))
                    {
                        NaviEdge edge = triangle.Edges[edgeIndex];
                        triangle = edge.OpposedTriangle(triangle);
                        if (triangle != null && triangle.TestPathFlags(_pathFlags))
                        {
                            _goalPosition = rayStart + rayDirection * (minDistance + 0.1f);
                            _goalTriangle = _naviMesh.NaviCdt.FindTriangleAtPoint(_goalPosition);
                            break;
                        }
                    }
                    else
                        break;
                }
            }
            return NaviPathResult.Success;
        }
    }

    public class NaviPathChannel : List<NaviChannelEdge>
    {
        public NaviPathChannel(int capacity) : base(capacity) { }
    }

    public struct NaviChannelEdge
    {
        public NaviEdge Edge;
        public bool Flip;
        
        public readonly NaviPoint LeftEndPoint() => Edge.Points[Flip ? 1 : 0];
        public readonly NaviPoint RightEndPoint() => Edge.Points[Flip ? 0 : 1];
    }

    public enum PathGenerationFlags
    {
        Default = 0,
        IncompletedPath = 1 << 1,
        IgnoreSweep = 1 << 2,
    }

    public class NaviPathSearchState : IComparable<NaviPathSearchState>
    {
        public NaviPathSearchState ParentState;
        public NaviTriangle Triangle;
        public NaviEdge Edge;
        public float DistDone;
        public float DistLeft;
        public float Distance;

        public NaviPathSearchState() {}

        public int CompareTo(NaviPathSearchState other)
        {
            return Distance.CompareTo(other.Distance);
        }

        public bool IsAncestor(NaviTriangle triangle)
        {
            NaviPathSearchState parent = ParentState;
            while (parent != null)
            {
                if (parent.Triangle == triangle)
                    return true;
                parent = parent.ParentState;
            }
            return false;
        }

    }
}
