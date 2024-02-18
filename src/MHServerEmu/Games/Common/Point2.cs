namespace MHServerEmu.Games.Common
{
    public class Point2
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point2(float x, float y)
        {
            X = (int)x;
            Y = (int)y;
        }

        public Point2(Point2 p)
        {
            X = p.X;
            Y = p.Y;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override bool Equals(object obj)
        {
            if (obj is not Point2) return false;
            Point2 other = (Point2)obj;
            return X == other.X && Y == other.Y;
        }

        public void Set(Point2 p)
        {
            X = p.X;
            Y = p.Y;
        }

        public static float DistanceSquared2D(Point2 a, Point2 b) => Vector3.LengthSqr(new(b.X - a.X, b.Y - a.Y, 0.0f));

        public static bool operator ==(Point2 a, Point2 b) => a.Equals(b);
        public static bool operator !=(Point2 a, Point2 b) => !(a == b);
    }
}
