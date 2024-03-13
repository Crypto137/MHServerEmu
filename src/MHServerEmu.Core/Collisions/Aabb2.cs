using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public class Aabb2 : IBounds
    {
        public Vector2 Min { get; set; }
        public Vector2 Max { get; set; }

        public Aabb2()
        {
            Min = new Vector2();
            Max = new Vector2();
        }

        public Aabb2(Vector3 center, float diameter)
        {
            float radius = diameter * 0.5f;
            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
        }

        public Aabb2(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public Aabb2(Vector2 center, float diameter)
        {
            float radius = diameter * 0.5f;
            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
        }

        public bool FullyContainsXY(Aabb bounds)
        {
            return bounds.Min.X >= Min.X && bounds.Max.X <= Max.X &&
                   bounds.Min.Y >= Min.Y && bounds.Max.Y <= Max.Y;
        }

        public Aabb2 Expand(float expandSize)
        {
            Vector2 expandVector = new(expandSize, expandSize);
            return Expand(expandVector);
        }

        public void Expand(Point2 p)
        {
            if (p.X < Min.X) Min.X = p.X;
            else if (p.X > Max.X) Max.X = p.X;
            if (p.Y < Min.Y) Min.Y = p.Y;
            else if (p.Y > Max.Y) Max.Y = p.Y;
        }

        public Aabb2 Expand(Vector2 expandSize) => new(Min - expandSize, Max + expandSize);

        public Aabb2 Translate(Vector3 newPosition)
        {
            Vector2 translationVector = new(newPosition.X, newPosition.Y);
            return new(Min + translationVector, Max + translationVector);
        }

        /// <summary>
        /// Return the coordinates of the corners
        /// </summary>
        public Point2[] GetPoints()
        {
            return new Point2[]
            {
                new Point2(Min.X, Min.Y),
                new Point2(Min.X, Max.Y),
                new Point2(Max.X, Max.Y),
                new Point2(Max.X, Min.Y)
            };
        }

        public Aabb2 Translate(Vector2 newPosition) => new(Min + newPosition, Max + newPosition);

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

        public bool Intersects(Aabb bounds)
        {
            if (Max.X < bounds.Min.X || Min.X > bounds.Max.X ||
                Max.Y < bounds.Min.Y || Min.Y > bounds.Max.Y)
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

        public Vector2 Center { get => new((Min.X + Max.X) * 0.5f, (Min.Y + Max.Y) * 0.5f); }
        public float Width { get => Max.X - Min.X; }
        public float Length { get => Max.Y - Min.Y; }
    }
}
