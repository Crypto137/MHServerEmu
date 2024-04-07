using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Navi
{
    [Flags]
    public enum NaviTriangleFlags
    {
        Attached = 1 << 0,
        Markup = 1 << 1,
    }

    public class TriangleList : InvasiveList<NaviTriangle>
    {
        public TriangleList(int maxIterators = 1) : base(maxIterators) { }
        public override InvasiveListNode<NaviTriangle> GetInvasiveListNode(NaviTriangle element, int listId) => element.InvasiveListNode;
    }

    public class NaviTriangle
    {
        public NaviEdge[] Edges { get; private set; }
        public byte EdgeSideFlags { get; private set; }
        public NaviTriangleFlags Flags { get; private set; }
        public PathFlags PathingFlags { get; set; }
        public ContentFlagCounts ContentFlagCounts { get; set; }
        public InvasiveListNode<NaviTriangle> InvasiveListNode { get; private set; }

        public NaviTriangle(NaviEdge e0, NaviEdge e1, NaviEdge e2)
        {
            Edges = new NaviEdge[3];
            Edges[0] = e0;
            Edges[1] = e1;
            Edges[2] = e2;
            ContentFlagCounts = new();
            InvasiveListNode = new();
            UpdateEdgeSideFlags();
            Attach();
        }

        public uint GetHash()
        {
            uint hash = 2166136261;
            hash = (hash ^ Edges[0].GetHash()) * 16777619;
            hash = (hash ^ Edges[1].GetHash()) * 16777619;
            hash = (hash ^ Edges[2].GetHash()) * 16777619;
            hash = (hash ^ EdgeSideFlags) * 16777619;
            hash = (hash ^ (byte)Flags) * 16777619;
            hash = (hash ^ (byte)PathingFlags) * 16777619;
            hash = (hash ^ ContentFlagCounts.GetHash()) * 16777619;

            return hash;
        }

        public string ToHashString()
        {
            return $"{GetHash():X} E[{Edges[0].GetHashOpposedTriangle(this):X} {Edges[1].GetHashOpposedTriangle(this):X} {Edges[2].GetHashOpposedTriangle(this):X}]";
        }

        public string ToHashString2()
        {
            return $"{GetHash():X} E[{Edges[0].GetHash():X} {Edges[1].GetHash():X} {Edges[2].GetHash():X}] [{EdgeSideFlags:X} {(byte)Flags:X} {(byte)PathingFlags:X}]";
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

        public NaviEdge EdgeMod(int index)
        {
            return Edges[index % 3];
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

        public NaviEdge OpposedEdge(NaviPoint point)
        {
            return Edges[OpposedEdgeIndex(point)];
        }

        public NaviTriangle OpposedTriangle(NaviPoint point, out NaviEdge edge)
        {
            edge = OpposedEdge(point);
            return edge.OpposedTriangle(this);
        }

        public NaviTriangle NextTriangleSharingPoint(NaviPoint point)
        {
            int edgeIdx = OpposedEdgeIndex(point);
            var edge = EdgeMod(edgeIdx + 1);
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

        public NaviEdge FindEdge(NaviPoint p0, NaviPoint p1)
        {
            foreach (var edge in Edges)
                if ((edge.Points[0] == p0 && edge.Points[1] == p1) ||
                    (edge.Points[0] == p1 && edge.Points[1] == p0))
                    return edge;
            return null;
        }

        public NaviPoint OpposedVertex(NaviTriangle triangle)
        {
            for (int i = 0; i < 3; i++)
            {
                NaviPoint p = triangle.PointCW(i);
                if (PointCW(0) == p || PointCW(1) == p || PointCW(2) == p) continue;
                return p;
            }
            return null;
        }

        public float CalcSpawnableArea()
        {
            if (PathingFlags.HasFlag(PathFlags.Walk) && PathingFlags.HasFlag(PathFlags.BlackOutZone) == false)
                return Segment.SignedDoubleTriangleArea2D(PointCW(0).Pos, PointCW(1).Pos, PointCW(2).Pos) / 2.0f;
            return 0.0f;
        }
    }

    public class NaviTriangleState
    {
        public NaviTriangleFlags Flags { get; private set; }
        public PathFlags PathingFlags { get; private set; }
        public ContentFlagCounts ContentFlagCounts { get; private set; }    
        
        public NaviTriangleState(NaviTriangle triangle)
        {
            Flags = triangle.Flags & NaviTriangleFlags.Markup;
            PathingFlags = triangle.PathingFlags;
            ContentFlagCounts = new(triangle.ContentFlagCounts);
        }

        public void RestoreState(NaviTriangle triangle)
        {
            triangle.SetFlag(Flags);
            triangle.PathingFlags = PathingFlags;
            triangle.ContentFlagCounts.Set(ContentFlagCounts);
        }
    }
}
