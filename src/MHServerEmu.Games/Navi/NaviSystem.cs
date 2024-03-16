using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Regions;
using System.Drawing;

namespace MHServerEmu.Games.Navi
{
    public class NaviSystem
    {
        public bool Log = true;
        public Region Region { get; private set; }

        private List<NaviPoint> _naviPoints = new();
        private List<NaviEdge> _naviEdges = new();
        private List<NaviTriangle> _naviTriangles = new();
        private List<NaviPathSearchState> _naviPathSearchStates = new();

        public bool Initialize(Region region)
        {
            Region = region;
            return true;
        }

        public NaviPathSearchState NewPathSearchState()
        {
            var pathSearchState = new NaviPathSearchState(this);
            _naviPathSearchStates.Add(pathSearchState);
            return pathSearchState;
        }

        public void Delete(NaviRef navRef)
        {
            if (navRef is NaviPoint navPoint) 
                _naviPoints.Remove(navPoint);
            else if (navRef is NaviEdge navEdge) 
                _naviEdges.Remove(navEdge);
            else if (navRef is NaviTriangle navTriangle) 
                _naviTriangles.Remove(navTriangle);
            else if (navRef is NaviPathSearchState navPathSearchState) 
                _naviPathSearchStates.Remove(navPathSearchState);
        }

        public NaviPoint CreatePoint(Vector3 pos)
        {
            var point = new NaviPoint(this);
            _naviPoints.Add(point);
            point.Pos = pos;
            return point;
        }

        public NaviEdge CreateEdge(NaviPoint p0, NaviPoint p1, NaviEdgeFlags edgeFlags, NaviEdgePathingFlags pathingFlags)
        {
            var edge = new NaviEdge(this);
            _naviEdges.Add(edge);
            edge.EdgeFlags = edgeFlags;
            edge.PathingFlags = pathingFlags;
            edge.Points[0] = p0;
            edge.Points[1] = p1;
            return edge;
        }

        public NaviTriangle CreateTriangle(NaviEdge e0, NaviEdge e1, NaviEdge e2)
        {
            var triangle = new NaviTriangle(this);
            _naviTriangles.Add(triangle);
            triangle.Edges[0] = e0;
            triangle.Edges[1] = e1;
            triangle.Edges[2] = e2;
            triangle.UpdateEdgeSideFlags();
            triangle.Attach();
            return triangle;
        }
    }

    [Flags]
    public enum NaviEdgeFlags
    {
        None = 0,
        Flag0 = 1 << 0,
        Flag1 = 1 << 1,
        Flag2 = 1 << 2,
        Flag3 = 1 << 3,
    }

    public class NaviPathSearchState : NaviRef
    {
        public NaviPathSearchState(NaviSystem navi) : base(navi)
        {
        }
    }

    [Flags]
    public enum NaviTriangleFlags
    {
        Attached = 1 << 0,
    }

    public class NaviTriangle : NaviRef
    {
        public NaviEdge[] Edges { get; set; } = new NaviEdge[3];
        public int EdgeSideFlags { get; private set; }
        public NaviTriangleFlags Flags { get; private set; }
        
        public NaviTriangle(NaviSystem navi) : base(navi)
        {
        }

        public void Attach()
        {
            for (int i = 0; i < 3; i++)
                Edges[i].AttachTriangle(this);

            Flags |= NaviTriangleFlags.Attached;
        }

        public void Detach()
        {
            if (Flags.HasFlag(NaviTriangleFlags.Attached))
            {
                for (int i = 0; i < 3; i++)
                    Edges[i].DetachTriangle(this);

                Flags &= ~NaviTriangleFlags.Attached;
            }
        }

        public Vector3 Centroid()
        {
            return (PointCW(0).Pos + PointCW(1).Pos + PointCW(2).Pos) / 3.0f;
        }

        public NaviEdge Edge(int index)
        {
            return Edges[index];
        }

        public void UpdateEdgeSideFlags()
        {
            EdgeSideFlags = 0;

            if (Edges[0].Points[0] == Edges[1].Points[0] || Edges[0].Points[1] == Edges[1].Points[1])
                EdgeSideFlags |= (1 << 1); 

            if (Edges[0].Points[0] == Edges[2].Points[0] || Edges[0].Points[1] == Edges[2].Points[1])
                EdgeSideFlags |= (1 << 2);

            if (EdgePointCW(0, 1) != EdgePointCW(1, 0))
                EdgeSideFlags = (~EdgeSideFlags) & 0x07;
        }

        private NaviPoint EdgePointCW(int edgeIndex, int point)
        {
            int pointIndex = ((EdgeSideFlags >> edgeIndex) & 1) ^ point;
            return Edges[edgeIndex].Points[pointIndex];
        }

        private NaviPoint PointCW(int edgeIndex)
        {
            int pointIndex = EdgeSideFlag(edgeIndex);
            return Edges[edgeIndex].Points[pointIndex];
        }

        private int EdgeSideFlag(int edgeIndex)
        {
            return (EdgeSideFlags >> edgeIndex) & 1;
        }
    }

    public class NaviEdge : NaviRef
    {
        public NaviEdgeFlags EdgeFlags { get; set; }
        public NaviEdgePathingFlags PathingFlags { get; set; }
        public NaviPoint[] Points { get; set; } = new NaviPoint[2];
        public NaviTriangle[] Triangles { get; set; } = new NaviTriangle[2];

        public NaviEdge(NaviSystem navi) : base(navi)
        {            
        }

        public void AttachTriangle(NaviTriangle triangle)
        {
            if (Triangles[0] == null) 
                Triangles[0] = triangle;
            else 
                Triangles[1] = triangle;
        }

        public void DetachTriangle(NaviTriangle triangle)
        {
            if (Triangles[0] == triangle)
            {
                Triangles[0].Release();
                Triangles[0] = null;
            }
            else
            {
                Triangles[1].Release();
                Triangles[1] = null;
            }
        }

        public NaviTriangle OpposedTriangle(NaviTriangle triangle)
        {
            if (Triangles[0] == triangle) 
                return Triangles[1];
            else 
                return Triangles[0];
        }

    }

    public class NaviEdgePathingFlags
    {
    }

    public class NaviPoint : NaviRef
    { 
        public Vector3 Pos { get; internal set; }

        public NaviPoint(NaviSystem navi) : base(navi)
        {
        }
    }

    public class NaviRef
    {
        protected NaviSystem _navi;
        protected int _ref;

        public NaviRef(NaviSystem navi)
        {
            _navi = navi;
            _ref = 1;
        }

        public void StaticIncRef()
        {
            _ref++;
        }

        public void StaticDecRef()
        {
            _ref--;
            if (_ref == 0) _navi.Delete(this);
        }

        public void SafeRelease()
        {
            Release();
        }

        public void Release()
        {
            StaticDecRef();
        }
    }
}
