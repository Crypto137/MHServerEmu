namespace MHServerEmu.Games.Navi
{
    public class NaviVertexLookupCache
    {
        public struct VertexCacheKey
        {
            public NaviPoint Point;
            public int X;
            public int Y;

            public override readonly bool Equals(object obj)
            {
                if (obj is not VertexCacheKey other) return false;
                if (X != other.X || Y != other.Y) return false;

                return Pred.NaviPointCompare2D(Point.Pos, other.Point.Pos);
            }

            public override readonly int GetHashCode()
            {
                const ulong Magic = 3636507997UL;
                ulong xm = (ulong)X * Magic;
                uint hash = (uint)xm + (uint)(xm >> 32);
                ulong ym = (ulong)Y * Magic;
                hash ^= (uint)ym + (uint)(ym >> 32);
                return (int)hash;
            }

            public static bool operator ==(VertexCacheKey left, VertexCacheKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(VertexCacheKey left, VertexCacheKey right)
            {
                return !(left == right);
            }
        }

        private NaviSystem _naviSystem;

        public const float CellSize = 12.0f;
        public const float NaviPointBoxEpsilon = 4.0f;

        public HashSet<VertexCacheKey> _vertexCache;

        public NaviVertexLookupCache(NaviSystem naviSystem)
        {
            _naviSystem = naviSystem;
            _vertexCache = new();
        }

        internal void Clear()
        {
            throw new NotImplementedException();
        }

        internal void Initialize(int totalVertices)
        {
            throw new NotImplementedException();
        }
    }
}