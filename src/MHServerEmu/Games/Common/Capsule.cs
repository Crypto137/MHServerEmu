using System.Text;

namespace MHServerEmu.Games.Common
{
    public class Capsule
    {
        public Vector3 A;
        public Vector3 B;
        public float Radius;

        public Capsule(Vector3 a, Vector3 b, float radius)
        {
            A = a;
            B = b;
            Radius = radius;
        }

        public static Capsule Zero => new(Vector3.Zero, Vector3.Zero, 0.0f);

        public static bool IntersectsSegment(Segment seg, Vector3 a, Vector3 b, float radius, ref float time)
        {
            bool SphereIntersects(Vector3 center, ref float time)
            {
                var sphere = new Sphere(center, radius);
                return sphere.Intersects(seg, ref time);
            }

            Vector3 ba = b - a;
            Vector3 sa = seg.Start - a;
            Vector3 es = seg.End - seg.Start;
            float dotsaba = Vector3.Dot(sa, ba);
            float dotesba = Vector3.Dot(es, ba);
            float dotba = Vector3.Dot(ba, ba);

            if (dotsaba < 0.0f && dotsaba + dotesba < 0.0f)
                return SphereIntersects(a, ref time);

            if (dotsaba > dotba && dotsaba + dotesba > dotba)
                return SphereIntersects(b, ref time);

            float dotes = Vector3.Dot(es, es);
            float dotsaes = Vector3.Dot(sa, es);
            float m = dotba * dotes - dotesba * dotesba;
            float sar = Vector3.Dot(sa, sa) - radius * radius;
            float mr = dotba * sar - dotsaba * dotsaba;

            if (Math.Abs(m) < float.Epsilon)
            {
                if (mr > 0.0f) return false;

                if (dotsaba < 0.0f) return SphereIntersects(a, ref time);
                else if (dotsaba > dotba) return SphereIntersects(b, ref time);
                else
                {
                    time = 0.0f;
                    return true;
                }
            }

            float dmn = dotba * dotsaes - dotesba * dotsaba;
            float mnr = dmn * dmn - m * mr;

            if (mnr < 0.0f)
            {
                time = 0.0f;
                return false;
            }

            time = (-dmn - MathF.Sqrt(mnr)) / m;

            if (dotsaba + time * dotesba < 0.0f) return SphereIntersects(a, ref time);
            else if (dotsaba + time * dotesba > dotba) return SphereIntersects(b, ref time);

            return time >= 0.0f && time <= 1.0f;
        }

        public bool Intersects(Aabb aabb)
        {
            Sphere sphere = new(A, Radius);
            float f = 0.0f;
            return sphere.Sweep(aabb, B - A, ref f);
        }

        public bool Intersects(Obb obb)
        {
            Sphere sphere = new(A, Radius);
            float f = 0.0f;
            return sphere.Sweep(obb, B - A, ref f);
        }

        public bool Intersects(Sphere sphere)
        {
            float distanceSq = Segment.SegmentPointDistanceSq(A, B, sphere.Center);
            float radius = Radius + sphere.Radius;
            return distanceSq <= radius * radius;
        }

        public bool Intersects(Capsule other)
        {
            float distanceSq = Segment.SegmentSegmentClosestPoint(A, B, other.A, other.B, out _, out _, out _, out _);
            float radius = Radius + other.Radius;
            return distanceSq <= radius * radius;
        }

        public bool Intersects(Triangle triangle)
        {
            return triangle.TriangleIntersectsCircle2D(A, Radius);
        }
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($" Radius: {Radius}");
            sb.AppendLine($" Height: {Vector3.Length(B-A)}");
            return sb.ToString();
        }
    }
}
