using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public class NaviVertexLookupCache
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public class VertexCacheKey
        {
            public NaviPoint Point;
            public int X;
            public int Y;

            public override bool Equals(object obj)
            {
                if (obj is not VertexCacheKey other) return false;
                if (X != other.X || Y != other.Y) return false;

                return Pred.NaviPointCompare2D(Point.Pos, other.Point.Pos);
            }

            public override int GetHashCode()
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
                return !left.Equals(right);
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
            _vertexCache.Clear();
        }

        public void Initialize(int size)
        {
            size += (size / 10);
            _maxSize = Math.Max(size, 1024);
            int hashSize = (int)(size / 1.0f);
            _vertexCache = new(hashSize);
        }

        public NaviPoint CacheVertex(Vector3 pos, out bool addedOut)
        {
            NaviPoint point = FindVertex(pos);
            if (point != null)
            {
                addedOut = false;
                return point;
            }

            point = new NaviPoint(pos);
            VertexCacheKey entry = new()
            {
                Point = point,
                X = (int)(point.Pos.X / CellSize),
                Y = (int)(point.Pos.Y / CellSize)
            };

            addedOut = _vertexCache.Add(entry);
            if (addedOut == false)
            {
                if (_navi.Log) Logger.Error($"CacheVertex failed to add point {point} due to existing point {entry.Point}");
                return null;
            }

            return point;
        }

        private VertexCacheKey FindVertexKey(Vector3 pos)
        {
            int x0 = (int)((pos.X - NaviPointBoxEpsilon) / CellSize);
            int x1 = (int)((pos.X + NaviPointBoxEpsilon) / CellSize);
            int y0 = (int)((pos.Y - NaviPointBoxEpsilon) / CellSize);
            int y1 = (int)((pos.Y + NaviPointBoxEpsilon) / CellSize);

            VertexCacheKey entry = new()
            {
                Point = new NaviPoint(pos)
            };

            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    entry.X = x;
                    entry.Y = y;

                    if (_vertexCache.TryGetValue(entry, out var key))
                        return key;
                }

            return default;
        }

        public NaviPoint FindVertex(Vector3 pos)
        {
            VertexCacheKey key = FindVertexKey(pos);
            return key?.Point;
        }

    }
}