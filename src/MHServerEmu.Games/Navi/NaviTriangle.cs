using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;

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
        public InvasiveListNode<NaviTriangle> InvasiveListNode { get; private set; }

        public ContentFlagCounts ContentFlagCounts;     // ContentFlagCounts needs to be a field for Clear() calls

        public NaviTriangle(NaviEdge e0, NaviEdge e1, NaviEdge e2)
        {
            Edges = new NaviEdge[3];
            Edges[0] = e0;
            Edges[1] = e1;
            Edges[2] = e2;
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

        public ulong GetHash64()
        {
            ulong hash = 14695981039346656037;
            hash = (hash ^ Edges[0].GetHash64()) * 1099511628211;
            hash = (hash ^ Edges[1].GetHash64()) * 1099511628211;
            hash = (hash ^ Edges[2].GetHash64()) * 1099511628211;
            // hash = (hash ^ EdgeSideFlags) * 1099511628211;
            // hash = (hash ^ (byte)Flags) * 1099511628211;
            // hash = (hash ^ (byte)PathingFlags) * 1099511628211;
            // hash = (hash ^ ContentFlagCounts.GetHash64()) * 1099511628211;

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

        public string ToHashString64()
        {
            return $"{GetHash64():X} E[{Edges[0].GetHash64():X} {Edges[1].GetHash64():X} {Edges[2].GetHash64():X}]\n [{PointCW(0).ToStringCoord2D()} {PointCW(1).ToStringCoord2D()} {PointCW(2).ToStringCoord2D()}]";
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

        public void OpposedTriangle(NaviPoint point, out NaviTriangle triangle, out NaviEdge edge)
        {
            edge = OpposedEdge(point);
            triangle = edge.OpposedTriangle(this);
        }

        public bool Contains(Vector3 point)
        {
            return Pred.Contains2D(PointCW(0), PointCW(1), PointCW(2), point);
        }

        public bool TestPathFlags(PathFlags pathFlags) => (PathingFlags & pathFlags) != 0;

        public float GetRelationOfEdgeToPoint(Vector3 point, int edgeIndex)
        {
            var edge = Edges[edgeIndex];
            float d = Segment.Cross2D(point - edge.Points[0].Pos, edge.Points[1].Pos - edge.Points[0].Pos);
            if (EdgeSideFlag(edgeIndex) != 0) d = -d;
            return d;
        }

        public float CalculateWidth(NaviEdge edge1, NaviEdge edge2)
        {
            NaviPoint point = NaviEdge.SharedVertex(edge1, edge2);
            NaviEdge edge = OpposedEdge(point);
            NaviPoint point2 = edge2.OpposedPoint(point);
            NaviPoint point1 = edge1.OpposedPoint(point);
            if (NaviUtil.IsAngleObtuse2D(point.Pos, point2.Pos, point1.Pos) 
                || NaviUtil.IsAngleObtuse2D(point.Pos, point1.Pos, point2.Pos))
            {
                float width1 = edge1.Length2D() - point.InfluenceRadius - point1.InfluenceRadius;
                float width2 = edge2.Length2D() - point.InfluenceRadius - point2.InfluenceRadius;
                return Math.Min(width1, width2);
            }
            else if (edge.TestFlag(NaviEdgeFlags.Constraint))
            {
                float dist = Segment.SegmentPointDistance2D(edge.Points[0].Pos, edge.Points[1].Pos, point.Pos);
                return dist - point.InfluenceRadius;
            }
            else
            {
                float width1 = edge1.Length2D() - point.InfluenceRadius - point1.InfluenceRadius;
                float width2 = edge2.Length2D() - point.InfluenceRadius - point2.InfluenceRadius;
                float width = Math.Min(width1, width2);
                return SearchWidth(point, this, edge, width);
            }
        }

        private static float SearchWidth(NaviPoint point, NaviTriangle triangle, NaviEdge edge, float width)
        {
            if (NaviUtil.IsAngleObtuse2D(point.Pos, edge.Points[0].Pos, edge.Points[1].Pos) ||
                NaviUtil.IsAngleObtuse2D(point.Pos, edge.Points[1].Pos, edge.Points[0].Pos))
                return width;

            float dist = Segment.SegmentPointDistance2D(edge.Points[0].Pos, edge.Points[1].Pos, point.Pos);
            if (dist > width)
                return width;
            else if (edge.TestFlag(NaviEdgeFlags.Constraint))
                return dist - point.InfluenceRadius;
            else
            {
                NaviTriangle oppoTriangle = edge.OpposedTriangle(triangle);
                int edgeIndex = oppoTriangle.EdgeIndex(edge);
                width = SearchWidth(point, oppoTriangle, oppoTriangle.EdgeMod(edgeIndex + 1), width);
                return SearchWidth(point, oppoTriangle, oppoTriangle.EdgeMod(edgeIndex + 2), width);
            }
        }

        public float CalculateWidthIgnoreInitialEdges(NaviEdge edge1, NaviEdge edge2)
        {
            NaviPoint point = NaviEdge.SharedVertex(edge1, edge2);
            NaviEdge edge = OpposedEdge(point);
            return SearchWidth(point, this, edge, float.MaxValue);
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
            ContentFlagCounts = triangle.ContentFlagCounts;
        }

        public void RestoreState(NaviTriangle triangle)
        {
            triangle.SetFlag(Flags);
            triangle.PathingFlags = PathingFlags;
            triangle.ContentFlagCounts = ContentFlagCounts;
        }
    }
}
