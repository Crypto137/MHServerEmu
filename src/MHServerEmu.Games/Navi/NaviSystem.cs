using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Navi
{
    public class NaviSystem
    {
        public Region Region { get; private set; }

        private List<NaviPoint> _naviPoints = new ();
        private List<object> _naviEdges = new();
        private List<object> _naviTriangles = new();
        private List<object> _naviPathSearchStates = new();

        public bool Initialize(Region region)
        {
            Region = region;
            return true;
        }

        public NaviPoint NewPoint()
        {
            var point = new NaviPoint();
            _naviPoints.Add(point);
            return point;
        }

        public NaviEdge NewEdge()
        {
            var edge = new NaviEdge();
            _naviEdges.Add(edge);
            return edge;
        }

        public NaviTriangle NewTriangle()
        {
            var triangle = new NaviTriangle();
            _naviTriangles.Add(triangle);
            return triangle;
        }

        public NaviPathSearchState NewPathSearchState()
        {
            var pathSearchState = new NaviPathSearchState();
            _naviPathSearchStates.Add(pathSearchState);
            return pathSearchState;
        }

        public void Delete(NaviPoint navPoint) => _naviPoints.Remove(navPoint);
        public void Delete(NaviEdge navEdge) => _naviEdges.Remove(navEdge);
        public void Delete(NaviTriangle navTriangle) => _naviTriangles.Remove(navTriangle);
        public void Delete(NaviPathSearchState navPathSearchState) => _naviPathSearchStates.Remove(navPathSearchState);

    }

    public class NaviPathSearchState
    {
    }

    public class NaviTriangle
    {
    }

    public class NaviEdge
    {
    }

    public class NaviPoint
    {
        public Vector3 Pos { get; internal set; }
    }

    public class NaviRef<T>
    {
        internal void SafeRelease()
        {
            throw new NotImplementedException();
        }
    }
}
