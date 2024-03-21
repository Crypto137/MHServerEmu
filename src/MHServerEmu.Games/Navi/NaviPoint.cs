using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public enum NaviPointFlags
    {
        None,
        Attached
    }

    public class NaviPoint : IComparable<NaviPoint>
    {
        public Vector3 Pos { get; internal set; }
        public NaviPointFlags Flags { get; private set; }
        public int Influence { get; private set; }
        public float InfluenceRadius { get; private set; }

        private static ulong TotalCount = 0;
        private readonly ulong _count;

        public NaviPoint(Vector3 pos)
        {
            Pos = pos;
            _count = TotalCount++;
        }

        public override string ToString()
        {
            return $"NaviPoint ({Pos.X:F4} {Pos.Y:F4} {Pos.Z:F4}) flg:{Flags} inf:{Influence}";
        }

        public int CompareTo(NaviPoint other)
        {
            return _count.CompareTo(other._count);
        }
    }
}
