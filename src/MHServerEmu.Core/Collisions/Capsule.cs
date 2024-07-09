using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public struct Capsule
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

        public static Capsule Zero { get; } = new(Vector3.Zero, Vector3.Zero, 0.0f);

        public static bool IntersectsSegment(Segment seg, Vector3 sideA, Vector3 sideB, float radius, ref float time)
        {
            bool SphereIntersects(in Vector3 center, ref float time)
            {
                var sphere = new Sphere(center, radius);
                return sphere.Intersects(seg, ref time);
            }
            // Real-Time Collision Detection p.197 (IntersectSegmentCylinder)

            Vector3 d = sideB - sideA;
            Vector3 m = seg.Start - sideA;
            Vector3 n = seg.End - seg.Start;
            float md = Vector3.Dot(m, d);
            float nd = Vector3.Dot(n, d);
            float dd = Vector3.Dot(d, d);
            // Test if segment fully outside either endcap of capsula
            if (md < 0.0f && md + nd < 0.0f) // Segment outside A side
                return SphereIntersects(sideA, ref time);

            if (md > dd && md + nd > dd) // Segment outside B side
                return SphereIntersects(sideB, ref time);

            float nn = Vector3.Dot(n, n);
            float mn = Vector3.Dot(m, n);
            float a = dd * nn - nd * nd;
            float k = Vector3.Dot(m, m) - radius * radius;
            float c = dd * k - md * md;

            if (Math.Abs(a) < Segment.Epsilon)
            {   // Segment runs parallel to capsula axis                
                if (c > 0.0f) return false; // 'a' and thus the segment lie outside cylinder
                // Now known that segment intersects capsula; figure out how it intersects
                if (md < 0.0f) return SphereIntersects(sideA, ref time); // Intersect segment against A endcap
                else if (md > dd) return SphereIntersects(sideB, ref time); // Intersect segment against B endcap
                else
                {   // 'a' lies inside capsula                 
                    time = 0.0f; 
                    return true;
                }
            }

            float b = dd * mn - nd * md;
            float discr = b * b - a * c;

            if (discr < 0.0f)
            {   // No real roots; no intersection             
                time = 0.0f; 
                return false;
            }

            time = (-b - MathHelper.SquareRoot(discr)) / a;

            if (md + time * nd < 0.0f) return SphereIntersects(sideA, ref time); // Intersection outside capsula on A side
            else if (md + time * nd > dd) return SphereIntersects(sideB, ref time); // Intersection outside capsula on B side
            // Segment intersects cylinder between the endcaps
            return time >= 0.0f && time <= 1.0f;
        }

        public bool Intersects(in Aabb aabb)
        {
            Sphere sphere = new(A, Radius);
            float f = 0.0f;
            return sphere.Sweep(aabb, B - A, ref f);
        }

        public bool Intersects(in Obb obb)
        {
            Sphere sphere = new(A, Radius);
            float f = 0.0f;
            return sphere.Sweep(obb, B - A, ref f);
        }

        public bool Intersects(in Sphere sphere)
        {
            float distanceSq = Segment.SegmentPointDistanceSq(A, B, sphere.Center);
            float radius = Radius + sphere.Radius;
            return distanceSq <= radius * radius;
        }

        public bool Intersects(in Capsule other)
        {
            float distanceSq = Segment.SegmentSegmentClosestPoint(A, B, other.A, other.B, out _, out _, out _, out _);
            float radius = Radius + other.Radius;
            return distanceSq <= radius * radius;
        }

        public bool Intersects(in Triangle triangle)
        {
            return triangle.TriangleIntersectsCircle2D(A, Radius);
        }

        public bool Contains(in Vector3 point)
        {
            float distanceSq = Segment.SegmentPointDistanceSq(A, B, point);
            return distanceSq <= Radius * Radius;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($" Radius: {Radius}");
            sb.AppendLine($" Height: {Vector3.Length(B - A)}");
            return sb.ToString();
        }
    }
}
