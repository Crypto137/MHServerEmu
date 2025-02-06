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
            // return flip

            if (input0.CompareTo(input1) < 0)
            {
                if (input2.CompareTo(input0) < 0)
                {
                    // 1 2 0
                    (input0, input2) = (input2, input0);
                    (input1, input2) = (input2, input1);
                    return false;
                }
                else if (input2.CompareTo(input1) < 0)
                {
                    // 0 2 1
                    (input1, input2) = (input2, input1);
                    return true;
                }
                else
                {
                    // 0 1 2
                    return false;
                }
            }
            else
            {
                if (input2.CompareTo(input1) < 0)
                {
                    // 2 1 0
                    (input0, input2) = (input2, input0);
                    return true;
                }
                else if (input2.CompareTo(input0) < 0)
                {
                    // 2 0 1
                    (input0, input1) = (input1, input0);
                    (input1, input2) = (input2, input1);
                    return false;
                }
                else
                {
                    // 1 0 2
                    (input0, input1) = (input1, input0);
                    return true;
                }
            }
        }

        public static double LineRelationship2D(NaviPoint p0, NaviPoint p1, Vector3 pos)
        {
            bool flip = SortInputs(ref p0, ref p1);
            double d = InternalOrient2D(p1.Pos, p0.Pos, pos);
            return flip ? -d : d;
        }

        public static bool LineSide2D(NaviPoint p0, NaviPoint p1, Vector3 pos)
        {
            double d = LineRelationship2D(p0, p1, pos);
            return d < 0.0;
        }

        private static double InternalOrient2D(Vector3 a, Vector3 b, Vector3 c)
        {
            ReadOnlySpan<double> pa = stackalloc double[] { a.X, a.Y };
            ReadOnlySpan<double> pb = stackalloc double[] { b.X, b.Y };
            ReadOnlySpan<double> pc = stackalloc double[] { c.X, c.Y };
            return Orient2D.Robust(pa, pb, pc);
        }

        public static bool Clockwise2D(NaviPoint p0, NaviPoint p1, NaviPoint p2)
        {
            bool flip = SortInputs(ref p0, ref p1, ref p2);
            double d = InternalOrient2D(p0.Pos, p1.Pos, p2.Pos);
            if (flip) d = -d;
            return d > 0.0;
        }

        public static bool IsDegenerate(NaviPoint p0, NaviPoint p1, NaviPoint p2, double deg = 0.5)
        {
            bool flip = SortInputs(ref p0, ref p1, ref p2);
            double d = InternalOrient2D(p0.Pos, p1.Pos, p2.Pos);
            if (flip) d = -d;
            return d < deg;
        }

        public static float RobustLinePointDistanceSq2D(Vector3 va, Vector3 vb, Vector3 vp)
        {
            Vector3 a = va.To2D();
            Vector3 b = vb.To2D();
            Vector3 p = vp.To2D();
            Vector3 ba = b - a;
            Vector3 ap = a - p;
            return Vector3.LengthSqr(Vector3.Cross(ba, ap)) / Vector3.LengthSqr(ba);
        }

        public static bool CircumcircleContainsPoint(NaviPoint p0, NaviPoint p1, NaviPoint p2, NaviPoint checkPoint)
        {
            bool flip = SortInputs(ref p0, ref p1, ref p2);
            double d = InternalIncircle(p0.Pos, p1.Pos, p2.Pos, checkPoint.Pos);
            if (flip) d = -d;
            return d > 0.0;
        }

        private static double InternalIncircle(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            ReadOnlySpan<double> pa = stackalloc double[] { a.X, a.Y };
            ReadOnlySpan<double> pb = stackalloc double[] { b.X, b.Y };
            ReadOnlySpan<double> pc = stackalloc double[] { c.X, c.Y };
            ReadOnlySpan<double> pd = stackalloc double[] { d.X, d.Y };
            return InCircle.Robust(pa, pb, pc, pd);
        }

        public static float CalcEarPower(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 point)
        {
            double incircle = InternalIncircle(p0, p1, p2, point);
            double orient = InternalOrient2D(p0, p1, p2);

            float power = (float)(incircle / orient);

            return power;
        }

        public static bool Contains2D(NaviPoint p0, NaviPoint p1, NaviPoint p2, Vector3 point)
        {
            return (LineRelationship2D(p0, p1, point) <= 0.0) &&
                   (LineRelationship2D(p1, p2, point) <= 0.0) &&
                   (LineRelationship2D(p2, p0, point) <= 0.0);
        }
    }
}
