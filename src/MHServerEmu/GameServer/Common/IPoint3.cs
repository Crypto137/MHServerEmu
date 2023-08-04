using Gazillion;

namespace MHServerEmu.GameServer.Common
{
    public class IPoint3
    {
        public uint X { get; set; }
        public uint Y { get; set; }
        public uint Z { get; set; }

        public IPoint3()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public IPoint3(IPoint3 point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }

        public IPoint3(uint x, uint y, uint z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public NetStructIPoint3 ToNetStruct() => NetStructIPoint3.CreateBuilder().SetX(X).SetY(Y).SetZ(Z).Build();

        public static IPoint3 operator +(IPoint3 a, IPoint3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static IPoint3 operator -(IPoint3 a, IPoint3 b)
        {
            // Checks to prevent uint underflow
            return new(a.X >= b.X ? a.X - b.X : 0,
                a.Y >= b.Y ? a.Y - b.Y : 0,
                a.Z >= b.Z ? a.Z - b.Z : 0);
        }
        public static bool operator ==(IPoint3 a, IPoint3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        public static bool operator !=(IPoint3 a, IPoint3 b) => !(a == b);
        public static bool operator >(IPoint3 a, IPoint3 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : (a.X > b.X && a.Y > b.Y && a.Z > b.Z);
        public static bool operator <(IPoint3 a, IPoint3 b) => !(a > b);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            IPoint3? point = obj as IPoint3;
            if (point != null) return (X == point.X && Y == point.Y && Z == point.Z);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y} z:{Z}";
    }
}
