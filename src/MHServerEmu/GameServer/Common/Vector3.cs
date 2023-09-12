using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Common
{
    public class Vector3
    {
        // precision values: 3 for position, 6 for orientation

        public float X { get; set; }    // Yaw for orientation
        public float Y { get; set; }    // Pitch for orientation
        public float Z { get; set; }    // Roll for orientation

        public Vector3()
        {
            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(CodedInputStream stream, int precision = 3)
        {
            X = stream.ReadRawFloat(precision);
            Y = stream.ReadRawFloat(precision);
            Z = stream.ReadRawFloat(precision);
        }

        public Vector3(NetStructPoint3 point3)
        {
            X = point3.X;
            Y = point3.Y;
            Z = point3.Z;
        }

        public byte[] Encode(int precision = 3)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawFloat(X, precision);
                cos.WriteRawFloat(Y, precision);
                cos.WriteRawFloat(Z, precision);

                cos.Flush();
                return ms.ToArray();
            }
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            Vector3 point = obj as Vector3;
            if (point != null) return (X == point.X && Y == point.Y && Z == point.Z);

            return false;
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        public override string ToString() => $"x:{X} y:{Y} z:{Z}";
    }
}
