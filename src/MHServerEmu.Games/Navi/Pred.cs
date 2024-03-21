using MHServerEmu.Core.VectorMath;
using RobustPredicates;

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

        public static bool SortInputs<T>(ref T input0, ref T input1) where T : IComparable<T>
        {
            if (input0.CompareTo(input1) < 0) 
                return false;
            else
            {
                (input0, input1) = (input1, input0);
                return true;
            }
        }

        public static double LineRelationship2D(NaviPoint p0, NaviPoint p1, Vector3 pos)
        {
            bool flip = SortInputs(ref p0, ref p1);
            double d = InternalOrient2D(p1.Pos, p0.Pos, pos);
            return flip ? -d : d;
        }

        private static double InternalOrient2D(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            double[] pa = { v0.X, v0.Y };
            double[] pb = { v1.X, v1.Y };
            double[] pc = { v2.X, v2.Y };
            return Orient2D.Robust(pa, pb, pc);
        }
    }
}
