using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.Common
{
    public class Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

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

        public Vector2(CodedInputStream stream, int precision = 3)
        {
            X = stream.ReadRawZigZagFloat(precision);
            Y = stream.ReadRawZigZagFloat(precision);
        }

        public void Encode(CodedOutputStream stream, int precision = 3)
        {
            stream.WriteRawZigZagFloat(X, precision);
            stream.WriteRawZigZagFloat(Y, precision);
        }

        public NetStructPoint2 ToNetStructPoint2() => NetStructPoint2.CreateBuilder().SetX(X).SetY(Y).Build();
        public NetStructIPoint2 ToNetStructIPoint2() => NetStructIPoint2.CreateBuilder()
            .SetX((uint)MathF.Max(0f, X)).SetY((uint)MathF.Max(0f, Y)).Build();     // Use MathF.Max when converting to NetStructIPoint2 to prevent underflow

        public void Set(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Vector2 a, Vector2 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public static bool operator >(Vector2 a, Vector2 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : (a.X > b.X && a.Y > b.Y);
        public static bool operator <(Vector2 a, Vector2 b) => !(a > b);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            Vector2 point = obj as Vector2;
            if (point != null) return (X == point.X && Y == point.Y);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y}";
    }
}
