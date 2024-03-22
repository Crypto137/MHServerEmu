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
        public NaviPointFlags Flags { get; set; }
        public int Influence { get; private set; }
        public float InfluenceRadius { get; private set; }

        private static ulong NextId = 0;
        private readonly ulong _id;

        public NaviPoint(Vector3 pos)
        {
            Pos = pos;
            _id = NextId++;
        }

        public override string ToString()
        {
            return $"NaviPoint ({Pos.X:F4} {Pos.Y:F4} {Pos.Z:F4}) flg:{Flags} inf:{Influence}";
        }

        public int CompareTo(NaviPoint other)
        {
            return _id.CompareTo(other._id);
        }

        public void SetFlag(NaviPointFlags flag)
        {
            Flags |= flag;
        }

        public bool TestFlag(NaviPointFlags flag)
        {
            return Flags.HasFlag(flag);
        }
    }
}
