using Gazillion;

namespace MHServerEmu.GameServer.Common
{
    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3()
        {
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
        }

        public Vector3(Vector3 point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public NetStructPoint3 ToNetStructPoint3() => NetStructPoint3.CreateBuilder().SetX(X).SetY(Y).SetZ(Z).Build();
        public NetStructIPoint3 ToNetStructIPoint3() => NetStructIPoint3.CreateBuilder()
            .SetX((uint)MathF.Max(0f, X)).SetY((uint)MathF.Max(0f, Y)).SetZ((uint)MathF.Max(0f, Z)).Build();    // Use MathF.Max when converting to NetStructIPoint3 to prevent underflow

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static bool operator ==(Vector3 a, Vector3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public static bool operator >(Vector3 a, Vector3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : (a.X > b.X && a.Y > b.Y && a.Z > b.Z);
        public static bool operator <(Vector3 a, Vector3 b) => !(a > b);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            Vector3? point = obj as Vector3;
            if (point != null) return (X == point.X && Y == point.Y && Z == point.Z);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y} z:{Z}";
    }
}
