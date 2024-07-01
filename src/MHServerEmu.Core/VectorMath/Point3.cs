namespace MHServerEmu.Core.VectorMath
{
    public struct Point3
    {
        public float X;
        public float Y;
        public float Z;

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
