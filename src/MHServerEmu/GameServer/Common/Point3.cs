using Gazillion;

namespace MHServerEmu.GameServer.Common
{
    public class Point3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Point3()
        {
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
        }

        public Point3(Point3 point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }

        public Point3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Point3 operator +(Point3 a, Point3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Point3 operator -(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static bool operator ==(Point3 a, Point3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        public static bool operator !=(Point3 a, Point3 b) => !(a == b);
        public static bool operator >(Point3 a, Point3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : (a.X > b.X && a.Y > b.Y && a.Z > b.Z);
        public static bool operator <(Point3 a, Point3 b) => !(a > b);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            Point3? point = obj as Point3;
            if (point != null) return (X == point.X && Y == point.Y && Z == point.Z);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public override string ToString() => $"x:{X} y:{Y} z:{Z}";
        public NetStructPoint3 ToNetStruct() => NetStructPoint3.CreateBuilder().SetX(X).SetY(Y).SetZ(Z).Build();
    }
}
