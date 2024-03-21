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

        public void Attach()
        {
            foreach (var edge in Edges)
                edge.AttachTriangle(this);

            Flags |= NaviTriangleFlags.Attached;
        }

        public void Detach()
        {
            if (Flags.HasFlag(NaviTriangleFlags.Attached))
            {
                foreach (var edge in Edges)
                    edge.DetachTriangle(this);

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

    }
}
