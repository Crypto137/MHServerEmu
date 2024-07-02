namespace MHServerEmu.Core.VectorMath
{
    public struct Point2 : IEquatable<Point2>
    {
        public int X;
        public int Y;

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

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override bool Equals(object obj)
        {
            if (obj is not Point2 other) return false;
            return Equals(other);
        }

        public bool Equals(Point2 point)
        {
            return X == point.X && Y == point.Y;
        }

        public static float DistanceSquared2D(Point2 a, Point2 b) => Vector3.LengthSqr(new(b.X - a.X, b.Y - a.Y, 0.0f));

        public static bool operator ==(Point2 a, Point2 b) => a.Equals(b);
        public static bool operator !=(Point2 a, Point2 b) => !a.Equals(b);
    }
}
