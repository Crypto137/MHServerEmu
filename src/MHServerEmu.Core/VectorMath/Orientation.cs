using Gazillion;
using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.VectorMath
{
    public struct Orientation : IEquatable<Orientation>
    {
        public float Yaw;
        public float Pitch;
        public float Roll;

        public Orientation()
        {
            Yaw = 0.0f;
            Pitch = 0.0f;
            Roll = 0.0f;
        }

        public Orientation(float yaw, float pich, float roll)
        {
            Yaw = yaw;
            Pitch = pich;
            Roll = roll;
        }

        public static Orientation Zero { get; } = new(0.0f, 0.0f, 0.0f);
        public static Orientation Player { get; } = new(-2.356194f, 0f, 0f);

        public float this[int index]
        {
            get
            {
                if (index == 0) return Yaw;
                if (index == 1) return Pitch;
                if (index == 2) return Roll;
                throw new IndexOutOfRangeException("Invalid index for Vector3");
            }
            set
            {
                if (index == 0) Yaw = value;
                else if (index == 1) Pitch = value;
                else if (index == 2) Roll = value;
                else
                    throw new IndexOutOfRangeException("Invalid index for Vector3");
            }
        }

        public NetStructPoint3 ToNetStructPoint3() => NetStructPoint3.CreateBuilder().SetX(Yaw).SetY(Pitch).SetZ(Roll).Build();

        public static Orientation operator +(Orientation a, Orientation b) => new(a.Yaw + b.Yaw, a.Pitch + b.Pitch, a.Roll + b.Roll);
        public static Orientation operator -(Orientation a, Orientation b) => new(a.Yaw - b.Yaw, a.Pitch - b.Pitch, a.Roll - b.Roll);
        public static bool operator ==(Orientation left, Orientation right) => left.Equals(right);
        public static bool operator !=(Orientation left, Orientation right) => !left.Equals(right);

        public override int GetHashCode() => (Yaw, Pitch, Roll).GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is not Orientation other) return false;
            return Equals(other);
        }

        public bool Equals(Orientation other)
        {
            return Yaw == other.Yaw && Pitch == other.Pitch && Roll == other.Roll;
        }

        public static bool IsFinite(Orientation v)
        {
            return float.IsFinite(v.Yaw) && float.IsFinite(v.Pitch) && float.IsFinite(v.Roll);
        }

        public static Orientation FromDeltaVector2D(Vector3 delta)
        {
            return new(MathF.Atan2(delta.Y, delta.X), 0.0f, 0.0f);
        }

        public static Orientation FromTransform3(in Transform3 transform)
        {
            return FromDeltaVector2D(transform.Col0);
        }

        public static Orientation FromDeltaVector(in Vector3 delta)
        {
            return new(MathF.Atan2(delta.Y, delta.X), MathF.Atan2(delta.Z, MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y)), 0.0f);
        }

        public Matrix3 GetMatrix3()
        {
            return Matrix3.RotationZYX(new(-Roll, -Pitch, Yaw));
        }

        public float GetYawNormalized()
        {
            float yaw = WrapAngleRadians(Yaw);
            if (yaw > MathHelper.Pi) yaw -= MathHelper.TwoPi;
            return yaw;
        }

        /// <summary>
        /// Angle is simplified into [0;2π] interval
        /// </summary>
        public static float WrapAngleRadians(float angleInRadian)
        {
            int wrap = (int)(angleInRadian / MathHelper.TwoPi);
            if (wrap > 0) return angleInRadian - wrap * MathHelper.TwoPi;
            if (angleInRadian < 0.0f) return angleInRadian - (wrap - 1) * MathHelper.TwoPi;
            return angleInRadian;
        }

        public override string ToString() => $"({Yaw:0.00}, {Pitch:0.00}, {Roll:0.00})";

        public string ToStringNames() => $"yaw :{Yaw:0.00} pich:{Pitch:0.00} roll:{Roll:0.00}";

    }
}
