using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public class Pred
    {
        public static bool NaviPointCompare2D(Vector3 p0, Vector3 p1)
        {
            float x0 = p0.X;
            float x1 = p1.X;
            float y0 = p0.Y;
            float y1 = p1.Y;
            float xd = (x0 < x1) ? (x1 - x0) : (x0 - x1);
            float yd = (y0 < y1) ? (y1 - y0) : (y0 - y1);
            return (xd * xd + yd * yd) <= 9.0f;
        }
    }
}
