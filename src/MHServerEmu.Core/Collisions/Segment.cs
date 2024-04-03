using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public class Segment
    {
        public static readonly Segment Zero = new(Vector3.Zero, Vector3.Zero);

        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }

        public Segment()
        {
            Start = Vector3.Zero;
            End = Vector3.Zero;
        }

        public Segment(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Vector3 GetDirection()
        {
            return End - Start;
        }

        public float Length()
        {
            return Vector3.Length(GetDirection());
        }

        public void Set(Segment segment)
        {
            Start.Set(segment.Start);
            End.Set(segment.End);
        }

        public const float Epsilon = 0.000001f;

        public static bool EpsilonTest(float val1, float val2, float epsilon = Epsilon)
        {
            return val1 >= val2 - epsilon && val1 <= val2 + epsilon;
        }

        public static float SegmentPointDistanceSq(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ba = b - a;
            Vector3 ca = c - a;
            Vector3 cb = c - b;
            float dotcb = Vector3.Dot(ca, ba);
            if (dotcb <= 0.0f) return Vector3.Dot(ca, ca);
            float dotba = Vector3.Dot(ba, ba);
            if (dotcb >= dotba) return Vector3.Dot(cb, cb);
            float dotca = Vector3.Dot(ca, ca);
            return dotca - dotcb * (dotcb / dotba);
        }

        public static float SegmentPointDistanceSq2D(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 a2d = new(a.X, a.Y, 0.0f);
            Vector3 b2d = new(b.X, b.Y, 0.0f);
            Vector3 c2d = new(c.X, c.Y, 0.0f);
            return SegmentPointDistanceSq(a2d, b2d, c2d);
        }

        public static float SegmentPointDistance2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return MathHelper.SquareRoot(SegmentPointDistanceSq2D(a, b, c));
        }

        public static float Cross2D(Vector3 v0, Vector3 v1)
        {
            return v0.X * v1.Y - v0.Y * v1.X;
        }

        public static float SegmentSegmentClosestPoint(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2, out float s, out float t, out Vector3 c1, out Vector3 c2)
        {
            Vector3 ba1 = b1 - a1;
            Vector3 ba2 = b2 - a2;
            Vector3 a12 = a1 - a2;
            float dotba1 = Vector3.Dot(ba1, ba1);
            float dotba2 = Vector3.Dot(ba2, ba2);
            float dotba212 = Vector3.Dot(ba2, a12);

            if (dotba1 <= Epsilon && dotba2 <= Epsilon)
            {
                s = t = 0.0f;
                c1 = a1;
                c2 = a2;
                Vector3 c12 = c1 - c2;
                return Vector3.Dot(c12, c12);
            }

            if (dotba1 <= Epsilon)
            {
                s = 0.0f;
                t = dotba212 / dotba2;
                t = Math.Clamp(t, 0.0f, 1.0f);
            }
            else
            {
                float dotba112 = Vector3.Dot(ba1, a12);
                if (dotba2 <= Epsilon)
                {
                    t = 0.0f;
                    s = Math.Clamp(-dotba112 / dotba1, 0.0f, 1.0f);
                }
                else
                {
                    float dotba12 = Vector3.Dot(ba1, ba2);
                    float denom = dotba1 * dotba2 - dotba12 * dotba12;

                    if (denom != 0.0f)
                        s = Math.Clamp((dotba12 * dotba212 - dotba112 * dotba2) / denom, 0.0f, 1.0f);
                    else
                        s = 0.0f;

                    float tnom = dotba12 * s + dotba212;
                    if (tnom < 0.0f)
                    {
                        t = 0.0f;
                        s = Math.Clamp(-dotba112 / dotba1, 0.0f, 1.0f);
                    }
                    else if (tnom > dotba2)
                    {
                        t = 1.0f;
                        s = Math.Clamp((dotba12 - dotba112) / dotba1, 0.0f, 1.0f);
                    }
                    else
                    {
                        t = tnom / dotba2;
                    }
                }
            }

            c1 = a1 + ba1 * s;
            c2 = a2 + ba2 * t;
            Vector3 c1c2 = c1 - c2;
            return Vector3.Dot(c1c2, c1c2);
        }

        public static bool SegmentsIntersect2D(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            float s1 = SignedDoubleTriangleArea2D(a0, a1, b1);
            float s2 = SignedDoubleTriangleArea2D(a0, a1, b0);
            if (s1 * s2 < 0.0f)
            {
                float s3 = SignedDoubleTriangleArea2D(b0, b1, a0);
                float s4 = s3 + s2 - s1;
                if (s3 * s4 < 0.0f) return true;
            }
            return false;
        }

        public static float SignedDoubleTriangleArea2D(Vector3 t0, Vector3 t1, Vector3 t2)
        {
            Vector3 v0 = t1 - t0;
            Vector3 v1 = t2 - t0;
            return Cross2D(v0, v1);
        }

        public static bool LineLineIntersect2D(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1, out Vector3 outPoint)
        {
            Vector3 av = a1 - a0;
            float ax = av.X;
            float ay = av.Y;

            float bx = b1.X - b0.X;
            float by = b1.Y - b0.Y;
            float cross = ax * by - ay * bx;

            if (cross == 0)
            {
                outPoint = Vector3.Zero;
                return false;
            }

            float cx = b0.X - a0.X;
            float cy = b0.Y - a0.Y;
            float t = (cx * by - cy * bx) / cross;

            outPoint = a0 + av * t;
            return true;
        }

    }

}
