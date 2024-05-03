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

        public float ApproxTotalDistance { get => _approxTotalDistance == 0.0f ? CalcApproximateDistance(_pathNodes) : _approxTotalDistance; }
        public List<NaviPathNode> PathNodeList { get => _pathNodes; }
        public bool IsComplete { get; internal set; }
        public bool IsCurrentGoalNodeLastNode { get; internal set; }

        public NaviPath()
        {
            _pathNodes = new();
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

        private static Segment GetPathSegment(NaviPathNode node0, NaviPathNode node1)
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

        internal NaviPathResult GenerateSimpleMove(Vector3 position, Vector3 syncPosition, float radius, PathFlags pathFlags)
        {
            throw new NotImplementedException();
        }

        public NaviPathNode GetCurrentGoalNode()
        {
            int currentIndex = _pathNodes.IndexOf(_currentNode);
            if (currentIndex == -1 || currentIndex == _pathNodes.Count - 1) return null;
            return _pathNodes[currentIndex + 1];
        }

        public Vector3 GetCurrentGoalPosition(Vector3 position)
        {
            int currentIndex = _pathNodes.IndexOf(_currentNode);
            if (currentIndex == -1 || currentIndex == _pathNodes.Count - 1)
                return position;
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

            if (pathNodes != null) Append(pathNodes, 0);
        }

        public void PopGoal()
        {
            if (_pathNodes.Count > 0) 
                _pathNodes.RemoveAt(_pathNodes.Count - 1);
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

        public Vector3 GetStartPosition()
        {
            if (!IsValid) return Vector3.Zero;
            return _pathNodes.First().Vertex;
        }

        public Vector3 GetFinalPosition()
        {
            if (!IsValid) return Vector3.Zero;
            return _pathNodes.Last().Vertex;
        }

        internal NaviPathResult GeneratePath(NaviMesh naviMesh, Vector3 position, Vector3 goalPosition, float radius, PathFlags pathFlags, PathGenerationFlags pathGenerationFlags, float incompleteDistance)
        {
            throw new NotImplementedException();
        }

        internal void UpdateEndPosition(Vector3 position)
        {
            throw new NotImplementedException();
        }

        internal void GetNextMovePosition(Vector3 currentPosition, float moveDistance, out Vector3 movePosition, out Vector3 moveDirection)
        {
            throw new NotImplementedException();
        }

        internal float ApproxCurrentDistance(Vector3 position)
        {
            throw new NotImplementedException();
        }

        internal NaviPathResult GenerateWaypointPath(NaviMesh naviMesh, Vector3 position, List<Waypoint> waypoints, float radius, PathFlags pathFlags)
        {
            throw new NotImplementedException();
        }
    }

    public enum PathGenerationFlags
    {
        Default = 0,
        IncompletedPath = 1 << 1,
    }

    public enum NaviPathResult
    {
        Success = 0,
        Failed = 1,
        FailedRegion = 3,
        IncompletedPath = 10,

    }

    public enum NaviSide
    {
        Left = 0,
        Right = 1,
        Point = 2
    }
}
