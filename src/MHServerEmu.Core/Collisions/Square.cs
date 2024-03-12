using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public class Square
    {
        public Point2 Min { get; set; }
        public Point2 Max { get; set; }
        public int Width { get => Max.X - Min.X + 1; }
        public int Height { get => Max.Y - Min.Y + 1; }
        public Square(Point2 min, Point2 max)
        {
            Min = min;
            Max = max;
        }
    }
}
