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
        Mask = 11, // Constraint | Flag1 | Door
    }

    public class NaviEdge
    {
        public NaviEdgeFlags EdgeFlags { get; private set; }
        public NaviEdgePathingFlags PathingFlags { get; set; }
        public NaviPoint[] Points { get; set; }
        public NaviTriangle[] Triangles { get; set; }
        public bool IsAttached => Triangles[0] != null || Triangles[1] != null;

        public uint Serial { get; private set; }

        public NaviEdge(NaviPoint p0, NaviPoint p1, NaviEdgeFlags edgeFlags, NaviEdgePathingFlags pathingFlags = null)
        {
            EdgeFlags = edgeFlags;
            PathingFlags = new(pathingFlags);
            Points = new NaviPoint[2];
            Points[0] = p0;
            Points[1] = p1;
            Triangles = new NaviTriangle[2];
        }

        public uint GetHash()
        {
            uint hash = 2166136261;
            hash = (hash ^ (byte)EdgeFlags) * 16777619;
            hash = (hash ^ PathingFlags.GetHash()) * 16777619;
            hash = (hash ^ Points[0].GetHash()) * 16777619;
            hash = hash ^ Points[1].GetHash();
            return hash;
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

        public bool Contains(NaviPoint p)
        {
            return Points[0] == p || Points[1] == p;
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

        public uint GetHashOpposedTriangle(NaviTriangle triangle)
        {
            var triOppo = OpposedTriangle(triangle);
            return triOppo != null ? triOppo.GetHash() : 0;
        }

        public string ToHashString()
        {
            uint tri0 = Triangles[0] != null ? Triangles[0].GetHash() : 0;
            uint tri1 = Triangles[1] != null ? Triangles[1].GetHash() : 0;
            return $"{GetHash():X} T[{tri0:X} {tri1:X}]";
        }
    }

}
