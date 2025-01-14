using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public struct Aabb : IBounds
    {
        public Vector3 Min;
        public Vector3 Max;

        public float Width { get => Max.X - Min.X; }
        public float Length { get => Max.Y - Min.Y; }
        public float Height { get => Max.Z - Min.Z; }

        public Vector3 Center { get => Min + (Max - Min) / 2.0f; }
        public Vector3 SizeVec => Max - Min;

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Aabb(Vector3 center, float width, float length, float height)
        {
            float halfWidth = width / 2.0f;
            float halfLength = length / 2.0f;
            float halfHeight = height / 2.0f;

            Min = new(center.X - halfWidth, center.Y - halfLength, center.Z - halfHeight);
            Max = new(center.X + halfWidth, center.Y + halfLength, center.Z + halfHeight);
        }

        public Aabb(Vector3 center, float size)
        {
            float halfSize = size / 2.0f;
            Min = new(center.X - halfSize, center.Y - halfSize, center.Z - halfSize);
            Max = new(center.X + halfSize, center.Y + halfSize, center.Z + halfSize);
        }

        public static Aabb InvertedLimit => new(
                new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                new Vector3(float.MinValue, float.MinValue, float.MinValue)
            );

        public static Aabb Zero => new(Vector3.Zero, Vector3.Zero);
        public float Volume => Width * Length * Height;
        public Vector3 Extents => (Max - Min) * 0.5f;

        public static Aabb operator +(in Aabb aabb1, in Aabb aabb2)
        {
            Vector3 newMin = new(
                Math.Min(aabb1.Min.X, aabb2.Min.X),
                Math.Min(aabb1.Min.Y, aabb2.Min.Y),
                Math.Min(aabb1.Min.Z, aabb2.Min.Z)
            );

            Vector3 newMax = new(
                Math.Max(aabb1.Max.X, aabb2.Max.X),
                Math.Max(aabb1.Max.Y, aabb2.Max.Y),
                Math.Max(aabb1.Max.Z, aabb2.Max.Z)
            );

            return new Aabb(newMin, newMax);
        }

        public static Aabb operator +(in Aabb aabb, in Vector3 point)
        {
            Vector3 newMin = new(Math.Min(aabb.Min.X, point.X),
                                    Math.Min(aabb.Min.Y, point.Y),
                                    Math.Min(aabb.Min.Z, point.Z));

            Vector3 newMax = new(Math.Max(aabb.Max.X, point.X),
                                    Math.Max(aabb.Max.Y, point.Y),
                                    Math.Max(aabb.Max.Z, point.Z));

            return new Aabb(newMin, newMax);
        }
        public override string ToString() => $"[{Min}, {Max}]";
        public Aabb Translate(Vector3 newPosition) => new(Min + newPosition, Max + newPosition);

        public Aabb Expand(float expandSize)
        {
            Vector3 expandVec = new(expandSize, expandSize, expandSize);
            return new(Min - expandVec, Max + expandVec);
        }

        public ContainmentType ContainsXY(in Aabb bounds)
        {
            if (bounds.Min.X > Max.X || bounds.Max.X < Min.X ||
                bounds.Min.Y > Max.Y || bounds.Max.Y < Min.Y)
            {
                return ContainmentType.Disjoint;
            }
            else if (bounds.Min.X >= Min.X && bounds.Max.X <= Max.X &&
                     bounds.Min.Y >= Min.Y && bounds.Max.Y <= Max.Y)
            {
                return ContainmentType.Contains;
            }
            return ContainmentType.Intersects;
        }

        public ContainmentType ContainsXY(in Vector3 point)
        {
            if (point.X > Max.X || point.X < Min.X ||
                point.Y > Max.Y || point.Y < Min.Y)
                return ContainmentType.Disjoint;
            else
                return ContainmentType.Contains;
        }

        public ContainmentType Contains(in Vector3 point)
        {
            if (point.X > Max.X || point.X < Min.X ||
                point.Y > Max.Y || point.Y < Min.Y ||
                point.Z > Max.Z || point.Z < Min.Z)
                return ContainmentType.Disjoint;
            else
                return ContainmentType.Contains;
        }

        public ContainmentType Contains(in Aabb2 bounds)
        {
            if (bounds.Min.X > Max.X || bounds.Max.X < Min.X ||
                bounds.Min.Y > Max.Y || bounds.Max.Y < Min.Y)
            {
                return ContainmentType.Disjoint;
            }
            else if (bounds.Min.X >= Min.X && bounds.Max.X <= Max.X &&
                     bounds.Min.Y >= Min.Y && bounds.Max.Y <= Max.Y)
            {
                return ContainmentType.Contains;
            }
            return ContainmentType.Intersects;
        }

        public ContainmentType ContainsXY(in Aabb areaBounds, float epsilon)
        {
            Aabb expanded = Expand(epsilon);
            return expanded.ContainsXY(areaBounds);
        }

        public float DistanceToPointSq2D(Vector3 point)
        {
            float distance = 0.0f;

            for (int i = 0; i < 2; i++)
            {
                float value = point[i];

                if (value < Min[i])
                    distance += MathF.Pow(Min[i] - value, 2);
                else if (value > Max[i])
                    distance += MathF.Pow(value - Max[i], 2);
            }

            return distance;
        }

        public float DistanceToPoint2D(Vector3 point)
        {
            float distance = DistanceToPointSq2D(point);
            return distance > Segment.Epsilon ? MathHelper.SquareRoot(distance) : 0.0f;
        }

        public void RoundToNearestInteger()
        {
            Min = new (MathHelper.Round(Min.X), MathHelper.Round(Min.Y), MathHelper.Round(Min.Z));
            Max = new (MathHelper.Round(Max.X), MathHelper.Round(Max.Y), MathHelper.Round(Max.Z));
        }

        public bool IntersectsXY(in Vector3 point)
        {
            if (Max.X < point.X || Min.X > point.X ||
                Max.Y < point.Y || Min.Y > point.Y)
                return false;
            return true;
        }

        public bool Intersects(in Vector3 point)
        {
            if (Max.X < point.X || Min.X > point.X ||
                Max.Y < point.Y || Min.Y > point.Y ||
                Max.Z < point.Z || Min.Z > point.Z)
                return false;
            return true;
        }

        public bool Intersects(in Aabb bounds)
        {
            if (Max.X < bounds.Min.X || Min.X > bounds.Max.X ||
                Max.Y < bounds.Min.Y || Min.Y > bounds.Max.Y ||
                Max.Z < bounds.Min.Z || Min.Z > bounds.Max.Z)
                return false;
            return true;
        }

        public bool Intersects(in Capsule capsule)
        {
            return capsule.Intersects(this);
        }

        public bool Intersects(in Triangle triangle)
        {
            return triangle.Intersects(this);
        }

        public bool Intersects(in Sphere sphere)
        {
            return sphere.Intersects(this);
        }

        public bool Intersects(in Obb obb)
        {
            return obb.Intersects(this);
        }

        public bool Intersects(in Aabb2 bounds)
        {
            if (Max.X < bounds.Min.X || Min.X > bounds.Max.X ||
                Max.Y < bounds.Min.Y || Min.Y > bounds.Max.Y)
                return false;
            return true;
        }

        public bool Intersects(Segment segment, ref float intersection)
        {
            return IntersectRay(segment.Start, segment.Direction, ref intersection, out _) && intersection <= 1.0f;
        }

        public static Aabb AabbFromWedge(in Vector3 point, in Vector3 direction, float angle, float radius)
        {
            var wedgeDirection = Vector3.SafeNormalize(direction) * radius;
            float halfAngle = MathHelper.ToRadians(angle / 2.0f);

            var circlePoint = wedgeDirection;
            if (Math.Abs(circlePoint.X) > Math.Abs(circlePoint.Y))
            {
                float axisX = circlePoint.X > 0.0f ? 1.0f : -1.0f;
                circlePoint = Vector3.XAxis * (axisX * radius);
            }
            else
            {
                float axisY = circlePoint.Y > 0.0f ? 1.0f : -1.0f;
                circlePoint = Vector3.YAxis * (axisY * radius);
            }

            var leftPoint = Vector3.AxisAngleRotate(wedgeDirection, Vector3.ZAxis, halfAngle);
            var rightPoint = Vector3.AxisAngleRotate(wedgeDirection, Vector3.ZAxis, -halfAngle);

            if (angle < 180.0f)
            {
                Vector3[] points = { new(0, 0, 0), circlePoint, leftPoint, rightPoint };
                return AabbFromPoints(points).Translate(point);
            }
            else
            {
                var leftCirclePoint = Vector3.AxisAngleRotate(circlePoint, Vector3.ZAxis, MathHelper.PiOver2);
                var rightCirclePoint = Vector3.AxisAngleRotate(circlePoint, Vector3.ZAxis, -MathHelper.PiOver2);

                Vector3[] points = { circlePoint, leftPoint, rightPoint, leftCirclePoint, rightCirclePoint };
                return AabbFromPoints(points).Translate(point);
            }
        }

        public static Aabb AabbFromPoints(in Vector3[] points)
        {
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            foreach (Vector3 point in points)
            {
                if (point.X > max.X) max.X = point.X;
                if (point.Y > max.Y) max.Y = point.Y;
                if (point.Z > max.Z) max.Z = point.Z;

                if (point.X < min.X) min.X = point.X;
                if (point.Y < min.Y) min.Y = point.Y;
                if (point.Z < min.Z) min.Z = point.Z;
            }

            return new Aabb(min, max);
        }

        public bool IsZero() => Vector3.IsNearZero(Min) && Vector3.IsNearZero(Max);
        public bool IsValid() => Min.X <= Max.X && Min.Y <= Max.Y && Min.Z <= Max.Z;

        public bool FullyContains(in Aabb bounds)
        {
            return bounds.Min.X >= Min.X && bounds.Max.X <= Max.X &&
                    bounds.Min.Y >= Min.Y && bounds.Max.Y <= Max.Y &&
                    bounds.Min.Z >= Min.Z && bounds.Max.Z <= Max.Z;
        }

        public float Radius2D() => Math.Max(Width, Length);

        public Vector3[] GetCorners()
        {
            Vector3[] corners = new Vector3[8];

            corners[0] = Min;
            corners[1] = new(Min.X, Max.Y, Min.Z);
            corners[2] = new(Min.X, Max.Y, Max.Z);
            corners[3] = new(Min.X, Min.Y, Max.Z);
            corners[4] = new(Max.X, Min.Y, Min.Z);
            corners[5] = new(Max.X, Max.Y, Min.Z);
            corners[6] = Max;
            corners[7] = new(Max.X, Min.Y, Max.Z);

            return corners;
        }

        public bool IntersectRay(in Vector3 point, in Vector3 velocity, ref float time, out Vector3 intersectPoint)
        {
            // Real-Time Collision Detection p.180 (IntersectRayAABB)
            float tMin = 0.0f;              // set to -FLT_MAX to get first hit on line
            float tMax = float.MaxValue;    // set to max distance ray can travel (for segment)
            // For all three slabs
            for (int i = 0; i < 3; ++i)
            {
                if (Math.Abs(velocity[i]) < Segment.Epsilon)
                {   // Ray is parallel to slab. No hit if origin not within slab
                    if (point[i] < Min[i] || point[i] > Max[i])
                    {
                        time = 0.0f;
                        intersectPoint = default;
                        return false;
                    }
                }
                else
                {   // Compute intersection t value of ray with near and far plane of slab
                    float ood = 1.0f / velocity[i];
                    float t1 = (Min[i] - point[i]) * ood;
                    float t2 = (Max[i] - point[i]) * ood;
                    // Make t1 be intersection with near plane, t2 with far plane
                    if (t1 > t2) (t2, t1) = (t1, t2);
                    // Compute the intersection of slab intersection intervals
                    tMin = Math.Max(tMin, t1);
                    tMax = Math.Min(tMax, t2);
                    // Exit with no collision as soon as slab intersection becomes empty
                    if (tMin > tMax)
                    {
                        time = 0.0f;
                        intersectPoint = default;
                        return false;
                    }
                }
            }
            // Ray intersects all 3 slabs. Return point (q) and intersection t value (tmin)
            intersectPoint = point + velocity * tMin;
            time = tMin;

            return true;
        }

        public string BoxToString()
        {
            return $" Box: {SizeVec}";
        }

    }
    public enum ContainmentType
    {
        Contains,
        Disjoint,
        Intersects
    }

}
