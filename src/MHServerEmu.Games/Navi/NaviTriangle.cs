using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Navi
{
    [Flags]
    public enum NaviTriangleFlags
    {
        Attached = 1 << 0,
        Markup = 1 << 1,
    }

    public class NaviTriangle
    {
        public NaviEdge[] Edges { get; set; }
        public byte EdgeSideFlags { get; private set; }
        public NaviTriangleFlags Flags { get; set; }
        public PathFlags PathingFlags { get; set; }
        public ContentFlagCounts ContentFlagCounts { get; set; }

        public NaviTriangle(NaviEdge e0, NaviEdge e1, NaviEdge e2)
        {
            Edges = new NaviEdge[3];
            Edges[0] = e0;
            Edges[1] = e1;
            Edges[2] = e2;
            ContentFlagCounts = new();
            UpdateEdgeSideFlags();
            Attach();
        }

        public void SetFlag(NaviTriangleFlags flag)
        {
            Flags |= flag;
        }

        public void ClearFlag(NaviTriangleFlags flag)
        {
            Flags &= ~flag;
        }

        public bool TestFlag(NaviTriangleFlags flag)
        {
            return Flags.HasFlag(flag);
        }

        public void Attach()
        {
            foreach (var edge in Edges)
                edge.AttachTriangle(this);

            SetFlag(NaviTriangleFlags.Attached);
        }

        public void Detach()
        {
            if (TestFlag(NaviTriangleFlags.Attached))
            {
                foreach (var edge in Edges)
                    edge.DetachTriangle(this);

                ClearFlag(NaviTriangleFlags.Attached);
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
                EdgeSideFlags = (byte)(~EdgeSideFlags & 0x07);
        }

        public NaviPoint EdgePointCW(int edgeIndex, int point)
        {
            int pointIndex = EdgeSideFlag(edgeIndex) ^ point;
            return Edges[edgeIndex].Points[pointIndex];
        }

        public NaviPoint PointCW(int edgeIndex)
        {
            int pointIndex = EdgeSideFlag(edgeIndex);
            return Edges[edgeIndex].Points[pointIndex];
        }

        public bool Contains(NaviPoint point)
        {
            return PointCW(0) == point || PointCW(1) == point || PointCW(2) == point;
        }

        public int EdgeSideFlag(int edgeIndex)
        {
            return (EdgeSideFlags >> edgeIndex) & 1;
        }

        public int OpposedEdgeIndex(NaviPoint point)
        {
            if (Edges[0].Points[0] == point || Edges[0].Points[1] == point)
            {
                if (Edges[1].Points[0] == point || Edges[1].Points[1] == point)
                    return 2;
                else
                    return 1;
            }
            else
                return 0;
        }

        public NaviTriangle NextTriangleSharingPoint(NaviPoint point)
        {
            int edgeIdx = OpposedEdgeIndex(point);
            var edge = Edge((edgeIdx + 1) % 3);
            return edge.OpposedTriangle(this);
        }

        public override string ToString()
        {
            return $"NaviTriangle [e0={Edges[0]} e1={Edges[1]} e2={Edges[2]}]";
        }

        public string ToStringWithIntegrity()
        {
            bool adjCheck =
                (Edges[0].Triangles[0] == this || Edges[0].Triangles[1] == this) &&
                (Edges[1].Triangles[0] == this || Edges[1].Triangles[1] == this) &&
                (Edges[2].Triangles[0] == this || Edges[2].Triangles[1] == this);

            bool pointsCheck =
                (EdgePointCW(0, 1) == EdgePointCW(1, 0)) && (EdgePointCW(1, 1) == EdgePointCW(2, 0)) && (EdgePointCW(2, 1) == EdgePointCW(0, 0)) &&
                (EdgePointCW(0, 0) == EdgePointCW(2, 1)) && (EdgePointCW(1, 0) == EdgePointCW(0, 1)) && (EdgePointCW(2, 0) == EdgePointCW(1, 1));

            return string.Format("{0} [p0={1} p1={2} p2={3}] adj={4} pts={5} wnd={6} notdeg={7}",
                ToString(),
                PointCW(0).ToString(), PointCW(1).ToString(), PointCW(2).ToString(),
                adjCheck ? 1 : 0,
                pointsCheck ? 1 : 0,
                Pred.Clockwise2D(PointCW(0), PointCW(1), PointCW(2)) ? 1 : 0,
                Pred.IsDegenerate(PointCW(0), PointCW(1), PointCW(2)) ? 0 : 1);
        }

        public NaviPoint OpposedVertex(NaviEdge edge)
        {
            for (int i = 0; i < 3; i++)
            {
                NaviPoint point = PointCW(i);
                if (edge.Points[0] != point && edge.Points[1] != point)
                    return point;
            }
            return null;
        }

        public int EdgeIndex(NaviEdge edge)
        {
            for (int i = 0; i < 3; i++)
                if (Edges[i] == edge) return i;
            return 0;
        }
    }

    public class NaviTriangleState
    {
        public NaviTriangleState(NaviTriangle triangle)
        {
        }

        internal void RestoreState(NaviTriangle t0)
        {
            throw new NotImplementedException();
        }
    }
}
