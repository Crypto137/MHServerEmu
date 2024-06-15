using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Locomotion;

namespace MHServerEmu.Games.Navi
{
    public class NaviPath
    {
        public bool IsValid { get =>  _pathNodes.Count > 0 ; }

        private List<NaviPathNode> _pathNodes;
        private NaviPathNode _currentNode;
        private float _approxTotalDistance;
        private bool _hasAccurateDistance;
        private PathFlags _pathFlags;
        private float _radius;
        private float _radiusSq;
        private float _width;

        public List<NaviPathNode> PathNodeList { get => _pathNodes; }
        public bool IsComplete { get => IsValid == false || _pathNodes.IndexOf(GetCurrentGoalNode()) == -1; }
        public bool IsCurrentGoalNodeLastNode 
        { 
            get
            {
                if (IsValid == false) return false;
                int currentNodeIndex = _pathNodes.IndexOf(_currentNode);
                return currentNodeIndex != -1 && currentNodeIndex + 1 == _pathNodes.Count - 1;
            }
        }

        public NaviPath()
        {
            _pathNodes = new();
            Clear();
        }

        public void Init(float radius, PathFlags pathFlags, List<NaviPathNode> pathNodes)
        {
            _radius = radius;
            _radiusSq = radius * radius;
            _width = 2.0f * radius;
            _pathFlags = pathFlags;
            _approxTotalDistance = 0.0f;
            _hasAccurateDistance = false;
            _pathNodes.Clear();
            _currentNode = default; //_pathNodes.FirstOrDefault();

            if (pathNodes != null && pathNodes.Count > 0) Append(pathNodes, 0);
        }

        public void Clear()
        {
            _radius = _radiusSq = _width = 0.0f;
            _pathFlags = 0;
            _approxTotalDistance = 0.0f;
            _hasAccurateDistance = false;
            _pathNodes.Clear();
            _currentNode = default; //_pathNodes.LastOrDefault();
        }

        public float ApproxTotalDistance()
        {
            return _approxTotalDistance == 0.0f ? CalcApproximateDistance(_pathNodes) : _approxTotalDistance;
        }

        private static float CalcApproximateDistance(List<NaviPathNode> pathNodes)
        {
            float distance = 0.0f;
            Vector3 prevVert = new();
            Segment segment = new();
            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                var node0 = pathNodes[i];
                var node1 = pathNodes[i + 1];
                if (node0.VertexSide != NaviSide.Point || node1.VertexSide != NaviSide.Point)
                {
                    Vector3 perpDir = Vector3.Perp2D(Vector3.Normalize2D(node1.Vertex - node0.Vertex));
                    if (node0.VertexSide == NaviSide.Point)
                        segment.Start = node0.Vertex;
                    else if (node0.VertexSide == NaviSide.Left)
                        segment.Start = node0.Vertex + perpDir * node0.Radius;
                    else
                        segment.Start = node0.Vertex - perpDir * node0.Radius;

                    if (node1.VertexSide == NaviSide.Point)
                        segment.End = node1.Vertex;
                    else if (node1.VertexSide == NaviSide.Left)
                        segment.End = node1.Vertex + perpDir * node1.Radius;
                    else
                        segment.End = node1.Vertex - perpDir * node1.Radius;

                    distance += Vector3.Distance2D(segment.Start, segment.End);
                    if (node0.VertexSide != NaviSide.Point)
                        distance += Vector3.Distance2D(prevVert, segment.Start);
                    prevVert = segment.End;
                }
                else
                {
                    distance += Vector3.Distance2D(node0.Vertex, node1.Vertex);
                    prevVert = node1.Vertex;
                }
            }
            return distance;
        }

        public static float CalcAccurateDistance(List<NaviPathNode> pathNodes)
        {
            float distance = 0f;
            Vector3 prevVert = new();

            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                var node0 = pathNodes[i];
                var node1 = pathNodes[i + 1];
                Segment segment = GetPathSegment(node0, node1);

                if (node0.Radius > 0f)
                {
                    Vector3 dir0 = prevVert - node0.Vertex;
                    Vector3 dir1 = segment.Start - node0.Vertex;
                    distance += node0.Radius * Vector3.Angle2D(dir0, dir1); 
                }
                distance += Vector3.Distance2D(segment.Start, segment.End);
                prevVert = segment.End;
            }

            return distance;
        }

        public float ApproxCurrentDistance(Vector3 currentPos)
        {
            float distance = 0.0f;
            var node = GetCurrentGoalNode();
            if (node == null) return distance;

            distance += Vector3.Distance2D(currentPos, node.Vertex);
            var nextIndex = _pathNodes.IndexOf(node) + 1;
            while (nextIndex < _pathNodes.Count)
            {
                var node0 = node;
                var node1 = _pathNodes[nextIndex];
                distance += Vector3.Distance2D(node0.Vertex, node1.Vertex);
                node = node1;
                nextIndex++;
            }
            return distance;
        }

        public static Segment GetPathSegment(NaviPathNode node0, NaviPathNode node1)
        {
            Segment segment = new();
            Segment tangent = new();
            if (node0.VertexSide == NaviSide.Point)
            {
                segment.Start = node0.Vertex;
                if (node1.VertexSide == NaviSide.Point)
                    segment.End = node1.Vertex;
                else
                {
                    if (Segment.CircleTangentPoints2D(node1.Vertex, node1.Radius, node0.Vertex, ref tangent))
                    {
                        segment.End = node1.VertexSide == NaviSide.Left ? tangent.End : tangent.Start;
                        segment.End.Z = node1.Vertex.Z;
                    }
                    else
                        segment.End = node1.Vertex;
                }
            }
            else
            {
                if (node1.VertexSide == NaviSide.Point)
                {
                    if (Segment.CircleTangentPoints2D(node0.Vertex, node0.Radius, node1.Vertex, ref tangent))
                    {
                        segment.Start = node0.VertexSide == NaviSide.Left ? tangent.Start : tangent.End;
                        segment.Start.Z = node0.Vertex.Z;
                    }
                    else
                        segment.Start = node0.Vertex;
                    segment.End = node1.Vertex;
                }
                else
                {
                    if (Segment.CircleTangentPoints2D(node0.Vertex, node0.Radius, node0.VertexSide == NaviSide.Left, 
                        node1.Vertex, node1.Radius, node1.VertexSide == NaviSide.Left, ref segment) == false)
                    {
                        segment.Start = node0.Vertex;
                        segment.End = node1.Vertex;
                    }
                }
            }
            return segment;
        }

        public float AccurateTotalDistance()
        {
            if (_hasAccurateDistance == false)
            {
                _approxTotalDistance = CalcAccurateDistance(_pathNodes);
                if (float.IsFinite(_approxTotalDistance) == false)
                    _approxTotalDistance = 0f;
                _hasAccurateDistance = true;
            }
            return _approxTotalDistance;
        }

        public void Append(List<NaviPathNode> pathNodes, int startIndex)
        {
            int count = pathNodes.Count;
            _pathNodes.Capacity += count;
            for (int i = startIndex; i < count; ++i)
                _pathNodes.Add(pathNodes[i]);

            _currentNode = _pathNodes[0];
        }

        public NaviPathNode GetCurrentGoalNode()
        {
            int currentIndex = _pathNodes.IndexOf(_currentNode);
            if (currentIndex == -1 || currentIndex == _pathNodes.Count - 1) return _currentNode;
            return _pathNodes[currentIndex + 1];
        }

        public Vector3 GetCurrentGoalPosition(Vector3 position)
        {
            int currentIndex = _pathNodes.IndexOf(_currentNode);
            if (currentIndex == -1 || currentIndex == _pathNodes.Count - 1) return position;
            NaviPathNode goalNode = _pathNodes[currentIndex + 1];
            return GetNodeGoalPosition(goalNode, position);
        }

        private static Vector3 GetNodeGoalPosition(NaviPathNode goalNode, Vector3 position)
        {
            if (goalNode.VertexSide == NaviSide.Point) 
                return goalNode.Vertex;

            Segment tangent = new();
            if (Segment.CircleTangentPoints2D(goalNode.Vertex, goalNode.Radius, position, ref tangent))
            {
                if (goalNode.VertexSide == NaviSide.Left)
                    return tangent.End;
                else
                    return tangent.Start;
            }
            else
                return goalNode.Vertex;
        }

        public void PopGoal()
        {
            if (_pathNodes.Count > 0) 
                _pathNodes.RemoveAt(_pathNodes.Count - 1);
        }

        public Vector3 GetStartPosition()
        {
            if (IsValid == false) return Vector3.Zero;
            return _pathNodes[0].Vertex;
        }

        public Vector3 GetFinalPosition()
        {
            if (IsValid == false) return Vector3.Zero;
            return _pathNodes[^1].Vertex;
        }

        public void UpdateEndPosition(Vector3 position)
        {
            if (_pathNodes.Count > 0)
                _pathNodes[^1].Vertex = position;
        }

        public static NaviPathResult CheckCanPathTo(NaviMesh naviMesh, Vector3 position, Vector3 goalPosition, float radius, PathFlags pathFlags)
        {
            List<NaviPathNode> pathNodes = new(256);
            var pathGen = new NaviPathGenerator(naviMesh);
            return pathGen.GeneratePath(position, goalPosition, radius, pathFlags, pathNodes, true, 0, 0f);
        }

        public NaviPathResult GeneratePath(NaviMesh naviMesh, Vector3 position, Vector3 goalPosition, float radius, PathFlags pathFlags, PathGenerationFlags pathGenerationFlags, float incompleteDistance)
        {
            List<NaviPathNode> pathNodes = new(256);
            var generator = new NaviPathGenerator(naviMesh);
            NaviPathResult result = generator.GeneratePath(position, goalPosition, radius, pathFlags, pathNodes, false, pathGenerationFlags, incompleteDistance);
            Init(radius, pathFlags, pathNodes);
            return result;
        }

        public NaviPathResult GenerateWaypointPath(NaviMesh naviMesh, Vector3 position, List<Waypoint> waypoints, float radius, PathFlags pathFlags)
        {
            if (waypoints.Count == 0 || waypoints[^1].Side != NaviSide.Point)
                return NaviPathResult.Failed;

            var startNode = new NaviPathNode(position, NaviSide.Point, 0f, false);
            List<NaviPathNode> pathNodes = new (256);
            var generator = new NaviPathGenerator(naviMesh);
            NaviPathResult result = NaviPathResult.Success;

            foreach (var wp in waypoints)
            {
                if (!Vector3.IsFinite(wp.Point) 
                    || (wp.Side == NaviSide.Point && wp.Radius != 0f) 
                    || (wp.Side != NaviSide.Point && wp.Radius <= 0f))
                    return NaviPathResult.Failed;

                var pathNode = new NaviPathNode(wp.Point,wp.Side, wp.Radius, false);
                Segment pathSegment = GetPathSegment(pathNodes.Count > 0 ? pathNodes[^1] : startNode, pathNode);
                List<NaviPathNode> wpPath = new (256);
                result = generator.GeneratePath(pathSegment.Start, pathSegment.End, radius, pathFlags, wpPath, false, 0, 0f);
                if (result == NaviPathResult.Success)
                {
                    if (pathNodes.Count + wpPath.Count > 256)
                        return NaviPathResult.FailedOutMaxSize;

                    if (wp.Side != NaviSide.Point)
                        wpPath[^1] = pathNode;

                    pathNodes.AddRange(wpPath);
                }
                else
                    break;
            }

            Init(radius, pathFlags, null);

            if (result == NaviPathResult.Success)
                Append(pathNodes, 0);

            return result;
        }

        public NaviPathResult GenerateSimpleMove(Vector3 position, Vector3 goalPosition, float radius, PathFlags pathFlags)
        {
            List<NaviPathNode> pathNodes = new (256);
            NaviPathGenerator.GenerateDirectMove(position, goalPosition, pathNodes);
            Init(radius, pathFlags, pathNodes);
            return NaviPathResult.Success;
        }

        public void GetNextMovePosition(Vector3 fromPoint, float moveDistance, out Vector3 movePosition, out Vector3 moveDirection)
        {
            movePosition = Vector3.Zero;
            moveDirection = Vector3.Zero;
            if (!Vector3.IsFinite(fromPoint) || !float.IsFinite(moveDistance)) return;

            TryAdvanceGoalNode(fromPoint);
            var goalNode = GetCurrentGoalNode();
            if (goalNode != null)
            {
                if (goalNode.VertexSide == NaviSide.Point)
                {
                    float distanceSq = Vector3.DistanceSquared2D(goalNode.Vertex, fromPoint);
                    float moveDistanceSq = moveDistance * moveDistance;
                    moveDirection = Vector3.SafeNormalize2D(goalNode.Vertex - fromPoint);
                    if (distanceSq < moveDistanceSq)
                    {
                        movePosition = goalNode.Vertex;
                        if (_pathNodes.IndexOf(_currentNode) + 1 == _pathNodes.Count)
                            _currentNode = null;
                    }
                    else
                        movePosition = fromPoint + moveDirection * moveDistance;
                }
                else
                {
                    if (Segment.CircleTangents2D(goalNode.Vertex, goalNode.Radius, fromPoint, out Segment tangent))
                    {
                        moveDirection = goalNode.VertexSide == NaviSide.Left ? tangent.End : tangent.Start;
                        if (!Vector3.IsFinite(moveDirection))
                            moveDirection = Vector3.SafeNormalize2D(goalNode.Vertex - fromPoint);
                    }
                    else
                        moveDirection = Vector3.SafeNormalize2D(goalNode.Vertex - fromPoint);
                    movePosition = fromPoint + moveDirection * moveDistance;
                }
            }
            else
            {
                movePosition = GetFinalPosition();
                moveDirection = Vector3.SafeNormalize2D(GetFinalPosition() - fromPoint);
            }

            if (!Vector3.IsFinite(movePosition))
                movePosition = Vector3.Zero;
        }

        private void TryAdvanceGoalNode(Vector3 fromPoint)
        {
            int goalIndex = _pathNodes.IndexOf(GetCurrentGoalNode());
            bool next;
            while (goalIndex != -1 && goalIndex < _pathNodes.Count)
            {
                NaviPathNode goalNode = _pathNodes[goalIndex];
                next = false;
                if (goalNode.VertexSide != NaviSide.Point)
                {
                    int nextGoalIndex = goalIndex + 1;
                    if (nextGoalIndex < _pathNodes.Count)
                    {
                        NaviPathNode nextGoalNode = _pathNodes[nextGoalIndex];
                        Vector3 nextGoalDir = nextGoalNode.Vertex - goalNode.Vertex;
                        Vector3 fromPointDir = fromPoint - goalNode.Vertex;
                        bool flip = Segment.Cross2D(fromPointDir, nextGoalDir) > 0.0f;
                        NaviSide vertexSide = flip ? NaviSide.Left : NaviSide.Right;
                        if (vertexSide == goalNode.VertexSide && Vector3.Dot2D(fromPointDir, nextGoalDir) > 0.0f)
                            next = true;
                    }
                }
                else
                {
                    if (Vector3.DistanceSquared2D(goalNode.Vertex, fromPoint) < 1.0f)
                        next = true;
                }
                if (next == false) break;
                _currentNode = _pathNodes[goalIndex];
                goalIndex++;
            }
        }

    }

    public enum NaviPathResult
    {
        Success = 0,
        Failed = 1,
        FailedRegion = 3,
        FailedNaviMesh = 4,
        FailedTriangle = 5,
        FailedNoPathFound = 7,
        FailedOutMaxSize = 9,
        IncompletedPath = 10,
    }

}
