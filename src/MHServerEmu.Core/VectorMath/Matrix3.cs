namespace MHServerEmu.Core.VectorMath
{
    public struct Matrix3
    {
        public Vector3 Col0;
        public Vector3 Col1;
        public Vector3 Col2;

        public Matrix3()
        {
            Col0 = new();
            Col1 = new();
            Col2 = new();
        }

        public Matrix3(in Vector3 col0, in Vector3 col1, in Vector3 col2)
        {
            Col0 = col0;
            Col1 = col1;
            Col2 = col2;
        }

        public static Matrix3 Rotation(float radians, Vector3 vector)
        {
            float x, y, z, s, c, oneMinusC, xy, yz, zx;
            s = MathF.Sin(radians);
            c = MathF.Cos(radians);
            x = vector.X;
            y = vector.Y;
            z = vector.Z;
            xy = (x * y);
            yz = (y * z);
            zx = (z * x);
            oneMinusC = (1.0f - c);
            return new Matrix3(
                new Vector3(((x * x) * oneMinusC) + c, (xy * oneMinusC) + (z * s), (zx * oneMinusC) - (y * s)),
                new Vector3((xy * oneMinusC) - (z * s), ((y * y) * oneMinusC) + c, (yz * oneMinusC) + (x * s)),
                new Vector3((zx * oneMinusC) + (y * s), (yz * oneMinusC) - (x * s), ((z * z) * oneMinusC) + c)
            );
        }

        public static Matrix3 RotationZYX(in Vector3 radiansXYZ)
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

        public static Matrix3 AbsPerElem(in Matrix3 mat)
        {
            return new Matrix3(
                Vector3.AbsPerElem(mat.Col0),
                Vector3.AbsPerElem(mat.Col1),
                Vector3.AbsPerElem(mat.Col2)
            );
        }

        public static Matrix3 Inverse(in Matrix3 mat)
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

        public static Vector3 operator *(in Matrix3 m, in Vector3 v)
        {
            return new Vector3(
                m.Col0.X * v.X + m.Col1.X * v.Y + m.Col2.X * v.Z,
                m.Col0.Y * v.X + m.Col1.Y * v.Y + m.Col2.Y * v.Z,
                m.Col0.Z * v.X + m.Col1.Z * v.Y + m.Col2.Z * v.Z
            );
        }

    }
}
