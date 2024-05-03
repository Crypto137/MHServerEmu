namespace MHServerEmu.Core.VectorMath
{
    public class Matrix3
    {
        public Vector3 Col0 { get; set; }
        public Vector3 Col1 { get; set; }
        public Vector3 Col2 { get; set; }

        public Matrix3()
        {
            Col0 = new();
            Col1 = new();
            Col2 = new();
        }

        public Matrix3(Vector3 col0, Vector3 col1, Vector3 col2)
        {
            Col0 = col0;
            Col1 = col1;
            Col2 = col2;
        }

        public static Matrix3 RotationZYX(Vector3 radiansXYZ)
        {
            float sX = MathF.Sin(radiansXYZ.X);
            float cX = MathF.Cos(radiansXYZ.X);
            float sY = MathF.Sin(radiansXYZ.Y);
            float cY = MathF.Cos(radiansXYZ.Y);
            float sZ = MathF.Sin(radiansXYZ.Z);
            float cZ = MathF.Cos(radiansXYZ.Z);
            float tmp0 = cZ * sY;
            float tmp1 = sZ * sY;
            return new(
                new Vector3(cZ * cY, sZ * cY, -sY),
                new Vector3(tmp0 * sX - sZ * cX, tmp1 * sX + cZ * cX, cY * sX),
                new Vector3(tmp0 * cX + sZ * sX, tmp1 * cX - cZ * sX, cY * cX)
            );
        }

        public static Matrix3 RotationZ(float radians)
        {
            float s = MathF.Sin(radians);
            float c = MathF.Cos(radians);
            return new(
                new (c, s, 0.0f),
                new (-s, c, 0.0f),
                Vector3.ZAxis
            );
        }

        public static Matrix3 AbsPerElem(Matrix3 mat)
        {
            return new Matrix3(
                Vector3.AbsPerElem(mat.Col0),
                Vector3.AbsPerElem(mat.Col1),
                Vector3.AbsPerElem(mat.Col2)
            );
        }

        public static Matrix3 Inverse(Matrix3 mat)
        {
            float detinv;
            var tmp0 = Vector3.Cross(mat.Col1, mat.Col2);
            var tmp1 = Vector3.Cross(mat.Col2, mat.Col0);
            var tmp2 = Vector3.Cross(mat.Col0, mat.Col1);
            detinv = 1.0f / Vector3.Dot(mat.Col2, tmp2);
            return new(
                new Vector3(tmp0.X * detinv, tmp1.X * detinv, tmp2.X * detinv),
                new Vector3(tmp0.Y * detinv, tmp1.Y * detinv, tmp2.Y * detinv),
                new Vector3(tmp0.Z * detinv, tmp1.Z * detinv, tmp2.Z * detinv)
            );
        }

        public static Vector3 operator *(Matrix3 m, Vector3 v)
        {
            return new Vector3(
                m.Col0.X * v.X + m.Col1.X * v.Y + m.Col2.X * v.Z,
                m.Col0.Y * v.X + m.Col1.Y * v.Y + m.Col2.Y * v.Z,
                m.Col0.Z * v.X + m.Col1.Z * v.Y + m.Col2.Z * v.Z
            );
        }

    }
}
