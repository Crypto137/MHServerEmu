using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public class NaviPath
    {
        public bool IsValid { get =>  _pathNodes.Count > 0 ; }

        private List<NaviPathNode> _pathNodes;
        private int _currentNodeIndex;
        private float _approxTotalDistance;
        private bool _hasAccurateDistance;
        private PathFlags _pathFlags;
        private float _radius;
        private float _radiusSq;
        private float _width;

        public float ApproxTotalDistance { get => _approxTotalDistance == 0.0f ? CalcApproximateDistance(_pathNodes) : _approxTotalDistance; }

        private float CalcApproximateDistance(List<NaviPathNode> pathNodes)
        {
            throw new NotImplementedException();
        }

        public List<NaviPathNode> PathNodeList { get => _pathNodes; }
        public bool IsComplete { get; internal set; }

        public NaviPath()
        {
            _pathNodes = new();
        }

        internal static float CalcAccurateDistance(List<NaviPathNode> pathNodes)
        {
            throw new NotImplementedException();
        }

        internal float AccurateTotalDistance()
        {
            throw new NotImplementedException();
        }

        public void Append(List<NaviPathNode> pathNodes, int startIndex)
        {
            int count = pathNodes.Count;
            _pathNodes.Capacity += count;
            for (int i = startIndex; i < count; ++i)
                _pathNodes.Add(pathNodes[i]);

            _currentNodeIndex = 0;
        }

        internal NaviPathResult GenerateSimpleMove(Vector3 position, Vector3 syncPosition, float radius, PathFlags pathFlags)
        {
            throw new NotImplementedException();
        }

        public int GetCurrentGoalNode()
        {
            if (_currentNodeIndex == 0) return 0;
            return ++_currentNodeIndex;
        }

        internal Vector3 GetCurrentGoalPosition(Vector3 position)
        {
            throw new NotImplementedException();
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
            _currentNodeIndex = 0;

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
            _currentNodeIndex = 0;
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
    }

    public enum PathGenerationFlags
    {
        Default = 0,
    }

    public enum NaviPathResult
    {
        Success = 0,
        Failed = 1,
        FailedRegion = 3,
        IncompletedPath = 10,

    }
}
