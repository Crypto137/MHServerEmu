using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Common
{
    public class Vector3
    {
        public static readonly Vector3 Zero = new(0.0f, 0.0f, 0.0f);

        // precision values: 3 for position, 6 for orientation

        public float X { get; set; }    // Yaw for orientation
        public float Y { get; set; }    // Pitch for orientation
        public float Z { get; set; }    // Roll for orientation

        public float this[int index]
        {
            get
            {                
                if (index == 0) return X;
                if (index == 1) return Y;
                if (index == 2) return Z;
                throw new IndexOutOfRangeException("Invalid index for Vector3");
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else if (index == 2) Z = value;
                else
                    throw new IndexOutOfRangeException("Invalid index for Vector3");
            }
        }

        public Vector3()
        {
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
        }

        public Vector3(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(CodedInputStream stream, int precision = 3)
        {
            X = stream.ReadRawZigZagFloat(precision);
            Y = stream.ReadRawZigZagFloat(precision);
            Z = stream.ReadRawZigZagFloat(precision);
        }

        public Vector3(NetStructPoint3 point3)
        {
            X = point3.X;
            Y = point3.Y;
            Z = point3.Z;
        }

        public void Encode(CodedOutputStream stream, int precision = 3)
        {
            stream.WriteRawZigZagFloat(X, precision);
            stream.WriteRawZigZagFloat(Y, precision);
            stream.WriteRawZigZagFloat(Z, precision);
        }

        public NetStructPoint3 ToNetStructPoint3() => NetStructPoint3.CreateBuilder().SetX(X).SetY(Y).SetZ(Z).Build();
        public NetStructIPoint3 ToNetStructIPoint3() => NetStructIPoint3.CreateBuilder()
            .SetX((uint)MathF.Max(0f, X)).SetY((uint)MathF.Max(0f, Y)).SetZ((uint)MathF.Max(0f, Z)).Build();    // Use MathF.Max when converting to NetStructIPoint3 to prevent underflow

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 v, float f) => new(v.X * f, v.Y * f, v.Z * f);
        public static Vector3 operator /(Vector3 v, float f) => new(v.X / f, v.Y / f, v.Z / f);
        public static bool operator ==(Vector3 a, Vector3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public static bool operator >(Vector3 a, Vector3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : (a.X > b.X && a.Y > b.Y && a.Z > b.Z);
        public static bool operator <(Vector3 a, Vector3 b) => !(a > b);
        public static float Length(Vector3 v) => MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        public static bool EpsilonSphereTest(Vector3 v1, Vector3 v2, float epsilon) => LengthSqr(v1 - v2) < epsilon;
        public static float LengthSqr(Vector3 v) => v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        public static bool IsNearZero(Vector3 v, float epsilon) => LengthSqr(v) < epsilon;
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            Vector3 point = obj as Vector3;
            if (point != null) return (X == point.X && Y == point.Y && Z == point.Z);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y} z:{Z}";
        public static float Dot(Vector3 v1, Vector3 v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        public static float SegmentPointDistanceSq(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ba = b - a;
            Vector3 ca = c - a;
            Vector3 cb = c - b;
            float dotcb = Dot(ca, ba);
            if (dotcb <= 0.0f) return Dot(ca, ca);
            float dotba = Dot(ba, ba);
            if (dotcb >= dotba) return Dot(cb, cb);
            float dotca = Dot(ca, ca);
            return (dotca - dotcb * (dotcb / dotba));
        }

        public static float DistanceSquared2D(Vector3 a, Vector3 b) => LengthSqr(new Vector3(b.X - a.X, b.Y - a.Y, 0.0f));
        
        public static Vector3 Normalize2D(Vector3 v)
        {
            Vector3 vector2D = new (v.X, v.Y, 0f);
            return IsNearZero(vector2D, 0.000001f) ? new(XAxis) : Normalize(vector2D); 
        }
        public static Vector3 XAxis => new (1f, 0f, 0f);
        public static Vector3 Normalize(Vector3 v) =>  v / Length(v);
    }
}
