using Gazillion;

namespace MHServerEmu.GameServer.Common
{
    public class IPoint2
    {
        public uint X { get; set; }
        public uint Y { get; set; }

        public IPoint2()
        {
            X = 0;
            Y = 0;
        }

        public IPoint2(IPoint2 point)
        {
            X = point.X;
            Y = point.Y;
        }

        public IPoint2(uint x, uint y)
        {
            X = x;
            Y = y;
        }

        public NetStructIPoint2 ToNetStruct() => NetStructIPoint2.CreateBuilder().SetX(X).SetY(Y).Build();

        public static IPoint2 operator +(IPoint2 a, IPoint2 b) => new(a.X + b.X, a.Y + b.Y);
        public static IPoint2 operator -(IPoint2 a, IPoint2 b)
        {
            // Checks to prevent uint underflow
            return new(a.X >= b.X ? a.X - b.X : 0,
                a.Y >= b.Y ? a.Y - b.Y : 0);
        }
        public static bool operator ==(IPoint2 a, IPoint2 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        public static bool operator !=(IPoint2 a, IPoint2 b) => !(a == b);
        public static bool operator >(IPoint2 a, IPoint2 b) => ReferenceEquals(null, a) ? ReferenceEquals(null, b) : (a.X > b.X && a.Y > b.Y);
        public static bool operator <(IPoint2 a, IPoint2 b) => !(a > b);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            IPoint2? point = obj as IPoint2;
            if (point != null) return (X == point.X && Y == point.Y);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y}";
    }
}
