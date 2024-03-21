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

        public static bool SortInputs<T>(ref T input0, ref T input1, ref T input2) where T : IComparable<T>
        {
            int i0, i1, i2;
            bool flip;

            if (input0.CompareTo(input1) < 0)
            {
                if (input2.CompareTo(input0) < 0)
                {
                    i0 = 2; i1 = 0; i2 = 1; flip = false;
                }
                else if (input2.CompareTo(input1) < 0)
                {
                    i0 = 0; i1 = 2; i2 = 1; flip = true;
                }
                else
                    return false;
            }
            else
            {
                if (input2.CompareTo(input1) < 0)
                {
                    i0 = 2; i1 = 1; i2 = 0; flip = true;
                }
                else if (input2.CompareTo(input0) < 0)
                {
                    i0 = 1; i1 = 2; i2 = 0; flip = false;
                }
                else
                {
                    i0 = 1; i1 = 0; i2 = 2; flip = true;
                }
            }

            T[] inputs = { input0, input1, input2 };
            input0 = inputs[i0];
            input1 = inputs[i1];
            input2 = inputs[i2];
            return flip;
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

        public static bool Clockwise2D(NaviPoint p0, NaviPoint p1, NaviPoint p2)
        {
            bool flip = SortInputs(ref p0, ref p1, ref p2);
            double d = InternalOrient2D(p0.Pos, p1.Pos, p2.Pos);
            if (flip) d = -d;
            return d > 0.0f;
        }

        public static bool IsDegenerate(NaviPoint p0, NaviPoint p1, NaviPoint p2, double deg = 0.5)
        {
            bool flip = SortInputs(ref p0, ref p1, ref p2);
            double d = InternalOrient2D(p0.Pos, p1.Pos, p2.Pos);
            if (flip) d = -d;
            return d < deg;
        }
    }
}
