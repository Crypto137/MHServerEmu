using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public struct Square
    {
        public Point2 Min;
        public Point2 Max;
        public int Width { get => Max.X - Min.X + 1; }
        public int Height { get => Max.Y - Min.Y + 1; }

        public Square(Point2 min, Point2 max)
        {
            Min = min;
            Max = max;
        }
    }
}
