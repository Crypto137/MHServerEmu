using Gazillion;

namespace MHServerEmu.GameServer.Common
{
    public class Point2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point2()
        {
            X = 0.0f;
            Y = 0.0f;
        }

        public Point2(Point2 point)
        {
            X = point.X;
            Y = point.Y;
        }

        public Point2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public NetStructPoint2 ToNetStruct() => NetStructPoint2.CreateBuilder().SetX(X).SetY(Y).Build();

        public static Point2 operator +(Point2 a, Point2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Point2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Point2 a, Point2 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        public static bool operator !=(Point2 a, Point2 b) => !(a == b);
        public static bool operator >(Point2 a, Point2 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : (a.X > b.X && a.Y > b.Y);
        public static bool operator <(Point2 a, Point2 b) => !(a > b);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            Point2? point = obj as Point2;
            if (point != null) return (X == point.X && Y == point.Y);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y}";
    }
}
