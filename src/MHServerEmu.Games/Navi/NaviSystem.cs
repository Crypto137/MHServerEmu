using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Navi
{
    public class NaviSystem
    {
        public bool Log = true;
        public Region Region { get; private set; }

        public bool Initialize(Region region)
        {
            Region = region;
            return true;
        }

    }

    public class NaviPathSearchState
    {
        public NaviPathSearchState()
        {
        }
    }

    [Flags]
    public enum NaviTriangleFlags
    {
        Attached = 1 << 0,
    }

    public class NaviTriangle
    {
        public NaviEdge[] Edges { get; set; }
        public int EdgeSideFlags { get; private set; }
        public NaviTriangleFlags Flags { get; private set; }

        public NaviTriangle(NaviEdge e0, NaviEdge e1, NaviEdge e2)
        {
            Edges = new NaviEdge[3];
            Edges[0] = e0;
            Edges[1] = e1;
            Edges[2] = e2;
            UpdateEdgeSideFlags();
            Attach();
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

    [Flags]
    public enum NaviEdgeFlags
    {
        None = 0,
        Flag0 = 1 << 0,
        Flag1 = 1 << 1,
        Flag2 = 1 << 2,
        Flag3 = 1 << 3,
    }

    public class NaviEdge
    {
        public NaviEdgeFlags EdgeFlags { get; set; }
        public NaviEdgePathingFlags PathingFlags { get; set; }
        public NaviPoint[] Points { get; set; }
        public NaviTriangle[] Triangles { get; set; }

        public NaviEdge(NaviPoint p0, NaviPoint p1, NaviEdgeFlags edgeFlags, NaviEdgePathingFlags pathingFlags)
        {
            EdgeFlags = edgeFlags;
            PathingFlags = pathingFlags;
            Points = new NaviPoint[2];
            Points[0] = p0;
            Points[1] = p1;
            Triangles = new NaviTriangle[2];
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
                Triangles[0] = null;
            else
                Triangles[1] = null;
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

    public class NaviPoint
    { 
        public Vector3 Pos { get; internal set; }

        public NaviPoint(Vector3 pos)
        {
            Pos = pos;
        }
    }

}
