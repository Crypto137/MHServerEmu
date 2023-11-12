using System.Text.Json.Serialization;

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

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
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


        public override string ToString() => $"Min:{Min} Max:{Max}";

        public Vector3 GetCenter()
        {
            Vector3 size = Max - Min;
            return Min + (size / 2.0f);
        }

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

    }
    public enum ContainmentType
    {
        Contains,    
        Disjoint,   
        Intersects  
    }

}
