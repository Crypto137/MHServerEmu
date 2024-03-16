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

        private NaviSystem _navi;

        public const float CellSize = 12.0f;
        public const float NaviPointBoxEpsilon = 4.0f;

        public HashSet<VertexCacheKey> _vertexCache;
        private int _maxSize;

        public NaviVertexLookupCache(NaviSystem naviSystem)
        {
            _navi = naviSystem;
            _vertexCache = new();
        }

        public void Clear()
        {
            foreach (var key in _vertexCache)
                key.Point.StaticDecRef();

            _vertexCache.Clear();
        }

        public void Initialize(int size)
        {
            size += (size / 10);
            _maxSize = Math.Max(size, 1024);
            int hashSize = (int)(size / 1.0f);
            _vertexCache = new(hashSize);
        }

    }
}