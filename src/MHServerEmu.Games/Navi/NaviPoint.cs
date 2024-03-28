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
        public ulong Id { get; private set; }
        private static ulong NextId = 0;

        public NaviPoint(Vector3 pos)
        {
            Pos = pos;
            Id = NextId++;
        }

        public override string ToString()
        {
            return $"NaviPoint ({Pos.X:F4} {Pos.Y:F4} {Pos.Z:F4}) flg:{Flags} inf:{Influence}";
        }

        public int CompareTo(NaviPoint other)
        {
            return Id.CompareTo(other.Id);
        }

        public void SetFlag(NaviPointFlags flag)
        {
            Flags |= flag;
        }
        public void ClearFlag(NaviPointFlags flag)
        {
            Flags &= ~flag;
        }

        public bool TestFlag(NaviPointFlags flag)
        {
            return Flags.HasFlag(flag);
        }
    }
}
