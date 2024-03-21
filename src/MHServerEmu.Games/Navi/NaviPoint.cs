using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public enum NaviPointFlags
    {
        None,
        Attached
    }

    public class NaviPoint
    {
        public Vector3 Pos { get; internal set; }
        public NaviPointFlags Flags { get; private set; }
        public int Influence { get; private set; }
        public float InfluenceRadius { get; private set; }

        public NaviPoint(Vector3 pos)
        {
            Pos = pos;
        }

        public override string ToString()
        {
            return $"NaviPoint ({Pos.X:F4} {Pos.Y:F4} {Pos.Z:F4}) flg:{Flags} inf:{Influence}";
        }

    }
}
