namespace MHServerEmu.Games.Common
{
    public class Aabb : IBounds
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public float Width { get => Max.X - Min.X; }
        public float Length { get => Max.Y - Min.Y; }
        public float Height { get => Max.Z - Min.Z; }
        
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

        public static Aabb InvertedLimit
        {
            get => new(
                new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                new Vector3(float.MinValue, float.MinValue, float.MinValue)
            );
        }

        public static Aabb Zero
        {
            get => new(
                Vector3.Zero,
                Vector3.Zero
            );
        }

        public float Volume { get => Width * Length * Height; }

        public void Set(Aabb aabb)
        {
            Min.Set(aabb.Min);
            Max.Set(aabb.Max);
        }

        public void Set(Vector3 min, Vector3 max)
        {
            Min.Set(min);
            Max.Set(max);
        }

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
        public string ToStringFloat() => $"[{Min.ToStringFloat()}, {Max.ToStringFloat()}]";
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

        public ContainmentType Contains(Aabb2 bounds)
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
            Min.Set( MathF.Round(Min.X), MathF.Round(Min.Y), MathF.Round(Min.Z) );
            Max.Set( MathF.Round(Max.X), MathF.Round(Max.Y), MathF.Round(Max.Z) );
        }

        public bool IntersectsXY(Vector3 point)
        {
            if (Max.X < point.X || Min.X > point.X || 
                Max.Y < point.Y || Min.Y > point.Y)
                return false;
            return true;
        }

        public bool Intersects(Aabb bounds)
        {
            if (Max.X < bounds.Min.X || Min.X > bounds.Max.X ||
                Max.Y < bounds.Min.Y || Min.Y > bounds.Max.Y ||
                Max.Z < bounds.Min.Z || Min.Z > bounds.Max.Z)
                return false;
            return true;
        }

        public bool Intersects(Aabb2 bounds)
        {
            if (Max.X < bounds.Min.X || Min.X > bounds.Max.X ||
                Max.Y < bounds.Min.Y || Min.Y > bounds.Max.Y)
                return false;
            return true;
        }

        public bool IsZero()
        {
            return Vector3.IsNearZero(Min) && Vector3.IsNearZero(Max);
        }

        public bool FullyContains(Aabb bounds)
        {
            return  bounds.Min.X >= Min.X && bounds.Max.X <= Max.X &&
                    bounds.Min.Y >= Min.Y && bounds.Max.Y <= Max.Y &&
                    bounds.Min.Z >= Min.Z && bounds.Max.Z <= Max.Z;
        }

        public float Radius2D() => Math.Max(Width, Length);

    }
    public enum ContainmentType
    {
        Contains,    
        Disjoint,   
        Intersects  
    }

}
