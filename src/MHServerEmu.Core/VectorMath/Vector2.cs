using Gazillion;

namespace MHServerEmu.Core.VectorMath
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X;
        public float Y;

        public Vector2()
        {
            X = 0.0f;
            Y = 0.0f;
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Vector2 a, Vector2 b) => a.Equals(b);
        public static bool operator !=(Vector2 a, Vector2 b) => !a.Equals(b);
        public static bool operator >(Vector2 a, Vector2 b) => a.X > b.X && a.Y > b.Y;
        public static bool operator <(Vector2 a, Vector2 b) => !(a > b);

        public override bool Equals(object obj)
        {
            if (obj is not Vector2 other) return false;
            return Equals(other);
        }

        public bool Equals(Vector2 point)
        {
            return X == point.X && Y == point.Y;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y}";
    }
}
