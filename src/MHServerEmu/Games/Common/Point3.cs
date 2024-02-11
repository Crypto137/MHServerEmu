
namespace MHServerEmu.Games.Common
{
    public class Point3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Point3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }
    }
}
