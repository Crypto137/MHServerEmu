namespace MHServerEmu.Games.Common
{
    public class Sphere : IBounds
    {
        public Sphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public Vector3 Center { get; }
        public float Radius { get; }

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

        public float RadiusSquared { get => Radius * Radius; }
    }
}
