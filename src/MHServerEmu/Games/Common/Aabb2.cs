
namespace MHServerEmu.Games.Common
{
    public class Aabb2
    { 
        public Vector2 Min { get; set; }
        public Vector2 Max { get; set; }       

        public Aabb2(Vector3 center, float diameter)
        {
            float radius = diameter * 0.5f;
            Min = new(center.X - radius, center.Y - radius);
            Max = new(center.X + radius, center.Y + radius);
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

        public Vector2 Center { get =>  new ((Min.X + Max.X) * 0.5f, (Min.Y + Max.Y) * 0.5f); }
        public float Width { get => Max.X - Min.X; }
        public float Length { get => Max.Y - Min.Y; }
    }
}
