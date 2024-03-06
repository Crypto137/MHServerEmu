namespace MHServerEmu.Games.Common
{
    public class Sphere : IBounds
    {
        public Vector3 Center { get; }
        public float Radius { get; }

        public Sphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public Aabb ToAabb()
        {
            return new (new (Center.X - Radius, Center.Y - Radius, Center.Z - Radius),
                        new (Center.X + Radius, Center.Y + Radius, Center.Z + Radius));
        }

        public bool Intersects(Vector3 v) { 
            return Vector3.LengthSqr(Center - v) <= RadiusSquared;
        }

        public ContainmentType Contains(Aabb2 bounds)
        {
            float radius = Radius;
            Vector3 center = Center;
            float minSq = 0.0f;
            float maxSq;

            float min = bounds.Min.X - center.X;
            float max = bounds.Max.X - center.X;
            if (min >= 0.0f)
            {
                if (min > radius) return ContainmentType.Disjoint;
                minSq = min * min;
                maxSq = max * max;
            }
            else if (max <= 0.0f)
            {
                if (max < -radius) return ContainmentType.Disjoint;
                minSq = max * max;
                maxSq = min * min;
            }
            else
            {                
                maxSq = MathF.Max(max * max, min * min);
            }

            min = bounds.Min.Y - center.Y;
            max = bounds.Max.Y - center.Y;
            if (min >= 0.0f)
            {
                if (min > radius) return ContainmentType.Disjoint;
                minSq += min * min;
                maxSq += max * max;
            }
            else if (max <= 0.0f)
            {
                if (max < -radius) return ContainmentType.Disjoint;
                minSq += max * max;
                maxSq += min * min;
            }
            else
            {
                maxSq += MathF.Max(max * max, min * min);
            }

            float radiusSq = RadiusSquared;
            if (minSq > radiusSq) return ContainmentType.Disjoint;
            return maxSq <= radiusSq ? ContainmentType.Contains : ContainmentType.Intersects;
        }

        public bool Intersects(Aabb bounds)
        {
            float sphereRadius = Radius;
            Vector3 center = Center;
            float minSq = 0.0f;

            for (int i = 0; i < 3; i++)
            {
                float min = bounds.Min[i] - center[i];
                float max = bounds.Max[i] - center[i];
                if (min >= 0.0f)
                {
                    if (min > sphereRadius) return false;
                    minSq += min * min;
                }
                else if (max <= 0.0f)
                {
                    if (max < -sphereRadius) return false;
                    minSq += max * max;
                }
            }

            return minSq < RadiusSquared;
        }

        public bool Intersects(Obb obb)
        {
            return obb.Intersects(this);
        }

        public bool Intersects(Sphere sphere)
        {
            return Vector3.Length(sphere.Center - Center) <= sphere.Radius + Radius;
        }

        public bool Intersects(Capsule capsule)
        {
            return capsule.Intersects(this);
        }

        public bool Intersects(Triangle triangle)
        {
            return triangle.Intersects(this);
        }

        public bool Sweep(Aabb aabb, Vector3 velocity, ref float time)
        {
            float diameter = Radius * 2.0f;
            Aabb expandedAabb = new (aabb.Center, aabb.Width + diameter, aabb.Length + diameter, aabb.Height + diameter);
            if (expandedAabb.IntersectRay(Center, velocity, ref time, out Vector3 point) == false) return false;

            int u = 0, v = 0;

            if (point.X < aabb.Min.X) u |= 1;
            if (point.X > aabb.Max.X) v |= 1;
            if (point.Y < aabb.Min.Y) u |= 2;
            if (point.Y > aabb.Max.Y) v |= 2;
            if (point.Z < aabb.Min.Z) u |= 4;
            if (point.Z > aabb.Max.Z) v |= 4;

            int m = u + v;

            Segment seg = new (Center, Center + velocity);

            if (m == 7)
            {
                float tmin = float.MaxValue;
                if (Capsule.IntersectsSegment(seg, SweepGetCorner(aabb, v), SweepGetCorner(aabb, v ^ 1), Radius, ref time))
                    tmin = Math.Min(time, tmin);
                if (Capsule.IntersectsSegment(seg, SweepGetCorner(aabb, v), SweepGetCorner(aabb, v ^ 2), Radius, ref time))
                    tmin = Math.Min(time, tmin);
                if (Capsule.IntersectsSegment(seg, SweepGetCorner(aabb, v), SweepGetCorner(aabb, v ^ 4), Radius, ref time))
                    tmin = Math.Min(time, tmin);
                if (tmin == float.MaxValue)
                    return false;
                time = tmin;
                return true;
            }

            if ((m & (m - 1)) == 0) return true;
            return Capsule.IntersectsSegment(seg, SweepGetCorner(aabb, u ^ 7), SweepGetCorner(aabb, v), Radius, ref time);
        }

        private static Vector3 SweepGetCorner(Aabb b, int n)
        {
            return new Vector3((n & 1) != 0 ? b.Max.X : b.Min.X,
                               (n & 2) != 0 ? b.Max.Y : b.Min.Y,
                               (n & 4) != 0 ? b.Max.Z : b.Min.Z);
        }

        public bool Sweep(Obb obb, Vector3 velocity, ref float time)
        {
            Vector3 obbVelocity = obb.TransformVector(velocity);
            Vector3 center = obb.TransformPoint(Center);
            Sphere sphere = new (center, Radius);
            Aabb aabb = new (obb.Center - obb.Extents, obb.Center + obb.Extents);
            return sphere.Sweep(aabb, obbVelocity, ref time);
        }

        public bool Intersects(Segment seg, ref float time)
        {
            return Intersects(seg, ref time, out _);
        }

        public bool Intersects(Segment segment, ref float time, out Vector3 intersectionPoint)
        {
            Vector3 direction = segment.GetDirection();
            float length = Vector3.Length(direction);
            Vector3 directionNorm = Vector3.Normalize(direction);

            return IntersectsSegment(segment.Start,
                                     directionNorm,
                                     length,
                                     Center,
                                     Radius,
                                     out time,
                                     out intersectionPoint);
        }

        public static bool IntersectsSegment(Vector3 start, Vector3 directionNorm, float length, Vector3 center, float radius, out float time, out Vector3 intersectionPoint)
        {
            if (IntersectsRay(start, directionNorm, center, radius, out float rayDistance, out Vector3 rayPoint))
            {
                Vector3 rayEdge = rayPoint - start;
                float distance = Vector3.Length(rayEdge);
                float rayTime = distance / length;
                if (rayTime <= 1.0f)
                {
                    time = rayTime;
                    intersectionPoint = rayPoint;
                    return true;
                }
            }

            time = 0.0f;
            intersectionPoint = Vector3.Zero;
            return false;
        }

        public static bool IntersectsRay(Vector3 start, Vector3 directionNorm, Vector3 center, float radius, out float rayDistance, out Vector3 rayPoint)
        {
            if (IntersectsRay(start, directionNorm, center, radius, out rayDistance))
            {
                rayPoint = start + directionNorm * rayDistance;
                return true;
            }

            rayDistance = 0.0f;
            rayPoint = Vector3.Zero;
            return false;
        }

        public static bool IntersectsRay(Vector3 start, Vector3 directionNorm, Vector3 center, float radius, out float rayDistance)
        {
            Vector3 sc = start - center;
            float dotscdn = Vector3.Dot(sc, directionNorm);
            float scr = Vector3.Dot(sc, sc) - radius * radius;

            if (scr > 0.0f && dotscdn > 0.0f)
            {
                rayDistance = 0.0f;
                return false;
            }

            float discreminant = dotscdn * dotscdn - scr;
            if (discreminant > 0.0f)
            {
                float discreminantSqrt = MathF.Sqrt(discreminant);
                rayDistance = (-dotscdn - discreminantSqrt);

                if (rayDistance < 0.0f) rayDistance = 0.0f;
                return true;
            }

            rayDistance = 0.0f;
            return false;
        }


        public float RadiusSquared => Radius * Radius;
        public static Sphere Zero => new(Vector3.Zero, 0.0f);
    }
}
