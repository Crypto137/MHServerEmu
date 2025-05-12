using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Core.VectorMath
{
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X;
        public float Y;
        public float Z;

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

        public Vector3(Point3 point)
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

        public Vector3(NetStructPoint3 point3)
        {
            X = point3.X;
            Y = point3.Y;
            Z = point3.Z;
        }

        public Vector3(float v)
        {
            X = v;
            Y = v;
            Z = v;
        }

        public NetStructPoint3 ToNetStructPoint3() => NetStructPoint3.CreateBuilder().SetX(X).SetY(Y).SetZ(Z).Build();

        public static Vector3 operator +(in Vector3 a, in Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(in Vector3 a, in Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator -(in Vector3 a) => new(-a.X, -a.Y, -a.Z);
        public static Vector3 operator *(in Vector3 v, float f) => new(v.X * f, v.Y * f, v.Z * f);
        public static Vector3 operator /(in Vector3 v, float f) => new(v.X / f, v.Y / f, v.Z / f);
        public static bool operator ==(Vector3 a, Vector3 b) => a.Equals(b);
        public static bool operator !=(Vector3 a, Vector3 b) => !a.Equals(b);
        public static bool operator >(Vector3 a, Vector3 b) => a.X > b.X && a.Y > b.Y && a.Z > b.Z;
        public static bool operator <(Vector3 a, Vector3 b) => !(a > b);
        public static float Length(in Vector3 v) => MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z); // MathF.Sqrt(LengthSqr(v))
        public static float LengthTest(in Vector3 v) => IsNearZero(v) ? 0.0f : Length(v);
        public static float Length2D(in Vector3 v)
        {
            var v2d = v.To2D();
            return IsNearZero(v2d) ? 0.0f : Length(v2d);
        }
        public static bool EpsilonSphereTest(in Vector3 v1, in Vector3 v2, float epsilon = Segment.Epsilon) => LengthSqr(v1 - v2) < epsilon;
        public static float LengthSqr(in Vector3 v) => v.X * v.X + v.Y * v.Y + v.Z * v.Z;
        public static float LengthSquared2D(in Vector3 v) => LengthSqr(v.To2D());
        public static bool IsNearZero(in Vector3 v, float epsilon = Segment.Epsilon) => LengthSqr(v) < epsilon;
        public static bool IsNearZero2D(in Vector3 v, float epsilon = Segment.Epsilon) => LengthSquared2D(v) < epsilon;

        public override bool Equals(object obj)
        {
            if (obj is not Vector3 other) return false;
            return Equals(other);
        }

        public bool Equals(Vector3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public string ToStringNames() => $"x:{X} y:{Y} z:{Z}";
        public override string ToString() => $"({X:0.00}, {Y:0.00}, {Z:0.00})";
        public static float Dot(in Vector3 v1, in Vector3 v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        public static float Dot2D(in Vector3 v1, in Vector3 v2) => v1.X * v2.X + v1.Y * v2.Y;
        public static float DistanceSquared2D(in Vector3 a, in Vector3 b) => LengthSqr(new(b.X - a.X, b.Y - a.Y, 0.0f));
        public static float DistanceSquared(in Vector3 a, in Vector3 b) => LengthSqr(b - a);

        public static Vector3 Normalize2D(in Vector3 v)
        {
            Vector3 vector2D = v.To2D();
            return IsNearZero(vector2D) ? XAxis : Normalize(vector2D);
        }

        public static Vector3 Normalize(in Vector3 v) => v / Length(v);

        public static bool IsFinite(in Vector3 v)
        {
            return float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
        }

        public static Vector3 RandomUnitVector2D(GRandom random)
        {
            float r = 2.0f * MathF.PI * random.NextFloat();
            float x = MathF.Cos(r);
            float y = MathF.Sin(r);
            return new(x, y, 0.0f);
        }

        public static float Distance2D(in Vector3 v1, in Vector3 v2) => Distance(v1.To2D(), v2.To2D());
        public static float Distance(in Vector3 v1, in Vector3 v2) => MathHelper.SquareRoot(DistanceSquared(v1, v2));

        public static Vector3 Flatten(Vector3 v, Axis axis)
        {
            return new(axis == Axis.X ? 0.0f : v.X,
                       axis == Axis.Y ? 0.0f : v.Y,
                       axis == Axis.Z ? 0.0f : v.Z);
        }

        public readonly Vector3 To2D() => new(X, Y, 0.0f);

        public static Vector3 AbsPerElem(in Vector3 vec)
        {
            return new Vector3(
                MathF.Abs(vec.X),
                MathF.Abs(vec.Y),
                MathF.Abs(vec.Z)
            );
        }

        public static Vector3 AxisAngleRotate(Vector3 pos, Vector3 axis, float angle)
        {
            if (Segment.EpsilonTest(LengthSquared(axis), 1.0f) == false) return pos;
            float cosA = MathF.Cos(angle);
            return pos * cosA + Cross(axis, pos) * MathF.Sin(angle) + axis * Dot(axis, pos) * (1.0f - cosA);
        }

        public static float AngleYaw(Vector3 a, Vector3 b)
        {
            Vector3 delta = Normalize2D(b - a);
            return MathF.Atan2(delta.Y, delta.X);
        }

        public static float Angle(Vector3 a, Vector3 b)
        {
            float magnitudes = Length(a) * Length(b);
            return magnitudes > 0.0f ? MathF.Acos(Math.Clamp(Dot(a, b) / magnitudes, -1.0f, 1.0f)) : 0.0f;
        }

        public static float Angle2D(Vector3 a, Vector3 b)
        {
            float magnitudes = Length2D(a) * Length2D(b);
            return magnitudes > 0.0f ? MathF.Acos(Math.Clamp(Dot2D(a, b) / magnitudes, -1.0f, 1.0f)) : 0.0f;
        }

        public static Vector3 Cross(in Vector3 v1, in Vector3 v2)
        {
            return new Vector3(
                v1.Y * v2.Z - v1.Z * v2.Y,
                v1.Z * v2.X - v1.X * v2.Z,
                v1.X * v2.Y - v1.Y * v2.X
            );
        }

        public static float LengthSquared(in Vector3 v) => LengthSqr(v);

        public void RoundToNearestInteger()
        {
            X = MathF.Round(X);
            Y = MathF.Round(Y);
            Z = MathF.Round(Z);
        }

        public float MaxElem() => Math.Max(Z, Math.Max(X, Y));

        public static Vector3 SafeNormalize2D(in Vector3 v) => SafeNormalize2D(v, XAxis);
        public static Vector3 SafeNormalize2D(in Vector3 v, in Vector3 zero)
        {
            Vector3 vector2D = v.To2D();
            return IsNearZero(vector2D) ? zero : Normalize(vector2D);
        }

        public static Vector3 SafeNormalize(in Vector3 v) => SafeNormalize(v, XAxis);
        public static Vector3 SafeNormalize(in Vector3 v, in Vector3 zero)
        {
            return IsNearZero(v) ? zero : Normalize(v);
        }

        public static Vector3 Perp2D(in Vector3 v) => new(v.Y, -v.X, 0.0f);

        public static Vector3 MaxPerElem(in Vector3 v0, in Vector3 v2)
        {
            return new Vector3(
                v0.X > v2.X ? v0.X : v2.X,
                v0.Y > v2.Y ? v0.Y : v2.Y,
                v0.Z > v2.Z ? v0.Z : v2.Z
            );
        }

        public static Vector3 MinPerElem(in Vector3 v0, in Vector3 v1)
        {
            return new Vector3(
                v0.X < v1.X ? v0.X : v1.X,
                v0.Y < v1.Y ? v0.Y : v1.Y,
                v0.Z < v1.Z ? v0.Z : v1.Z
            );
        }

        public static Vector3 Project(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 u = p1 - p0;
            Vector3 v = p2 - p0;
            return u * (Dot(u, v) / Dot(u, u)) + p0;
        }

        public static Vector3 Truncate(Vector3 v, float maxLength)
        {
            Vector3 truncatedVector = v;
            float length = Length(truncatedVector);
            if (length > maxLength)
                truncatedVector = (truncatedVector / length) * maxLength;

            return truncatedVector;
        }

        public static void SafeNormalAndLength2D(Vector3 v, out Vector3 resultNormal, out float resultLength) => SafeNormalAndLength2D(v, out resultNormal, out resultLength, XAxis);
        public static void SafeNormalAndLength2D(Vector3 v, out Vector3 resultNormal, out float resultLength, Vector3 zero)
        {
            Vector3 vector = v.To2D();
            if (IsNearZero(vector))
            {
                resultNormal = zero;
                resultLength = 0f;
            }
            else            
            {
                float length = Length(vector);
                resultNormal = vector / length;
                resultLength = length;
            }
        }

        public static Vector3 NextVector3(GRandom random, Vector3 vector, Vector3 variance)
        {
            Vector3 halfVaiance = variance / 2.0f;
            float z = random.NextFloat(vector.Z - halfVaiance.Z, vector.Z + halfVaiance.Z);
            float y = random.NextFloat(vector.Y - halfVaiance.Y, vector.Y + halfVaiance.Y);
            float x = random.NextFloat(vector.X - halfVaiance.X, vector.X + halfVaiance.X);
            return new Vector3(x, y, z);
        }

        public static Vector3 ToRadians(Vector3 angle)
        {
            return new Vector3(
                MathHelper.ToRadians(angle.X),
                MathHelper.ToRadians(angle.Y),
                MathHelper.ToRadians(angle.Z)
            );
        }

        // static vectors

        public static Vector3 Zero { get; } = new(0.0f, 0.0f, 0.0f);
        public static Vector3 XAxis { get; } = new(1.0f, 0.0f, 0.0f);
        public static Vector3 XAxisNeg { get; } = new(-1.0f, 0.0f, 0.0f);
        public static Vector3 YAxis { get; } = new(0.0f, 1.0f, 0.0f); 
        public static Vector3 YAxisNeg { get; } = new(0.0f, -1.0f, 0.0f); 
        public static Vector3 ZAxis { get; } = new(0.0f, 0.0f, 1.0f); 
        public static Vector3 ZAxisNeg { get; } = new(0.0f, 0.0f, -1.0f); 
        public static Vector3 Forward { get; } = XAxis; 
        public static Vector3 Right { get; } = YAxis; 
        public static Vector3 Up { get; } = ZAxis; 
        public static Vector3 Back { get; } = XAxisNeg; 
        public static Vector3 Left { get; } = YAxisNeg; 
        public static Vector3 Down { get; } = ZAxisNeg; 

    }

    public enum Axis
    {
        Invalid = -1,
        X = 0,
        Y = 1,
        Z = 2,
    }
}
