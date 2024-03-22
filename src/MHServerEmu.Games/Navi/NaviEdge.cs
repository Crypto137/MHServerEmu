using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    [Flags]
    public enum NaviEdgeFlags
    {
        None = 0,
        Constraint = 1 << 0,
        Flag1 = 1 << 1,
        Delaunay = 1 << 2,
        Door = 1 << 3,
    }

    public class NaviEdge
    {
        public NaviEdgeFlags EdgeFlags { get; private set; }
        public NaviEdgePathingFlags PathingFlags { get; set; }
        public NaviPoint[] Points { get; set; }
        public NaviTriangle[] Triangles { get; set; }
        public bool IsAttached => Triangles[0] != null || Triangles[1] != null;

        public uint Serial { get; private set; }

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

        public Vector3 Midpoint()
        {
            return (Points[0].Pos + Points[1].Pos) / 2.0f;
        }

        public override string ToString()
        {
            return $"NaviEdge [p0={Points[0]} p1={Points[1]}]";
        }

        public bool TestOperationSerial(NaviSerialCheck check)
        {
            bool result = Serial != check.Serial;
            Serial = check.Serial;
            return result;
        }
        
        public Vector3 Point(int index) => Points[index].Pos;

        public NaviPoint OpposedPoint(NaviPoint point)
        {
            if (Points[0] == point)
                return Points[1];
            else
                return Points[0];
        }

        public void SetFlag(NaviEdgeFlags flag)
        {
            EdgeFlags |= flag;
        }

        public void ClearFlag(NaviEdgeFlags flag)
        {
            EdgeFlags &= ~flag;
        }

        public bool TestFlag(NaviEdgeFlags flag)
        {
            return EdgeFlags.HasFlag(flag);
        }

        public void ConstraintMerge(NaviEdge edge)
        {
            bool flip = Points[0] != edge.Points[0] && Points[1] != edge.Points[1];
            PathingFlags.Merge(edge.PathingFlags, flip);
            SetFlag(NaviEdgeFlags.Constraint);
        }
    }

}
