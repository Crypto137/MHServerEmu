using System.Text.Json.Serialization;
using static MHServerEmu.Games.Powers.PowerPrototypes;

namespace MHServerEmu.Games.Common
{
    public class Aabb
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        [JsonIgnore]
        public float Width { get => Max.X - Min.X; }
        [JsonIgnore]
        public float Length { get => Max.Y - Min.Y; }
        [JsonIgnore]
        public float Height { get => Max.Z - Min.Z; }
        [JsonIgnore]
        public Vector3 Center { get => Min + ((Max - Min) / 2.0f); }
        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }
        public Aabb(Aabb bound)
        {
            Min = new(bound.Min);
            Max = new(bound.Max);
        }

        public Aabb(Vector3 center, float width, float length, float height)
        {
            float halfWidth = width / 2.0f;
            float halfLength = length / 2.0f;
            float halfHeight = height / 2.0f;

            Min = new (center.X - halfWidth, center.Y - halfLength, center.Z - halfHeight);
            Max = new (center.X + halfWidth, center.Y + halfLength, center.Z + halfHeight);
        }

        [JsonIgnore]

        public static readonly Aabb InvertedLimit = new (
            new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
            new Vector3(float.MinValue, float.MinValue, float.MinValue)
          );

        public static Aabb operator +(Aabb aabb1, Aabb aabb2)
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

        public static Aabb operator +(Aabb aabb, Vector3 point)
        {
            Vector3 newMin = new(Math.Min(aabb.Min.X, point.X),
                                    Math.Min(aabb.Min.Y, point.Y),
                                    Math.Min(aabb.Min.Z, point.Z));

            Vector3 newMax = new(Math.Max(aabb.Max.X, point.X),
                                    Math.Max(aabb.Max.Y, point.Y),
                                    Math.Max(aabb.Max.Z, point.Z));

            return new Aabb(newMin, newMax);
        }

        public override string ToString() => $"Min:{Min} Max:{Max}";

        public Aabb Translate(Vector3 newPosition) => new(Min + newPosition, Max + newPosition);

        public Aabb Expand(float expandSize)
        {
            Vector3 expandVec = new(expandSize, expandSize, expandSize);
            return new(Min - expandVec, Max + expandVec);
        }

        public ContainmentType ContainsXY(Aabb bounds)
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

        public ContainmentType ContainsXY(Aabb areaBounds, float epsilon)
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
            return (distance > 0.000001f) ? MathF.Sqrt(distance) : 0.0f;
        }

        public void RoundToNearestInteger()
        {
            Min = new Vector3(MathF.Round(Min.X), MathF.Round(Min.Y), MathF.Round(Min.Z));
            Max = new Vector3(MathF.Round(Max.X), MathF.Round(Max.Y), MathF.Round(Max.Z));
        }

        public bool IntersectsXY(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y;
        }

        public bool Intersects(Aabb bounds)
        {
            return  bounds.Min.X <= Max.X && bounds.Max.X >= Min.X &&
                    bounds.Min.Y <= Max.Y && bounds.Max.Y >= Min.Y &&
                    bounds.Min.Z <= Max.Z && bounds.Max.Z >= Min.Z;
        }
    }
    public enum ContainmentType
    {
        Contains,    
        Disjoint,   
        Intersects  
    }

}
