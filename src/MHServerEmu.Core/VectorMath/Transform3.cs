using MHServerEmu.Core.Collisions;

namespace MHServerEmu.Core.VectorMath
{
    public class Transform3
    {
        public Vector3 Col0 { get; set; }
        public Vector3 Col1 { get; set; }
        public Vector3 Col2 { get; set; }
        public Vector3 Col3 { get; set; }

        public Vector3 Translation { get => Col3; set => Col3 = value; }

        public Orientation Orientation => Orientation.FromTransform3(this);

        public Transform3(Transform3 transform)
        {
            Col0 = transform.Col0;
            Col1 = transform.Col1;
            Col2 = transform.Col2;
            Col3 = transform.Col3;
        }

        public Transform3(Vector3 _col0, Vector3 _col1, Vector3 _col2, Vector3 _col3)
        {
            Col0 = _col0;
            Col1 = _col1;
            Col2 = _col2;
            Col3 = _col3;
        }

        public static Transform3 Identity()
        {
            return new(
                Vector3.XAxis,
                Vector3.YAxis,
                Vector3.ZAxis,
                new Vector3(0.0f)
            );
        }


        public static Transform3 BuildTransform(Vector3 translation, Orientation rotation)
        {
            Transform3 transform = RotationZYX(new Vector3(-rotation.Roll, -rotation.Pitch, rotation.Yaw));
            transform.Translation = translation;
            return transform;
        }

        public static Transform3 RotationZYX(Vector3 radiansXYZ)
        {
            float sX, cX, sY, cY, sZ, cZ, tmp0, tmp1;
            sX = MathF.Sin(radiansXYZ.X);
            cX = MathF.Cos(radiansXYZ.X);
            sY = MathF.Sin(radiansXYZ.Y);
            cY = MathF.Cos(radiansXYZ.Y);
            sZ = MathF.Sin(radiansXYZ.Z);
            cZ = MathF.Cos(radiansXYZ.Z);
            tmp0 = cZ * sY;
            tmp1 = sZ * sY;

            return new(
                new Vector3(cZ * cY, sZ * cY, -sY),
                new Vector3(tmp0 * sX - sZ * cX, tmp1 * sX + cZ * cX, cY * sX),
                new Vector3(tmp0 * cX + sZ * sX, tmp1 * cX - cZ * sX, cY * cX),
                new Vector3(0.0f)
            );
        }

        public static Transform3 RotationZ(float radians)
        {
            float s, c;
            s = MathF.Sin(radians);
            c = MathF.Cos(radians);
            return new(
                new Vector3(c, s, 0.0f),
                new Vector3(-s, c, 0.0f),
                Vector3.ZAxis,
                Vector3.Zero
            );
        }

        public static Transform3 operator *(Transform3 left, Transform3 right)
        {
            return new Transform3(
                left * right.Col0,
                left * right.Col1,
                left * right.Col2,
                new Vector3(left * new Point3(right.Col3))
            );
        }

        public static Vector3 operator *(Transform3 t, Vector3 v)
        {
            return new Vector3(
                t.Col0.X * v.X + t.Col1.X * v.Y + t.Col2.X * v.Z,
                t.Col0.Y * v.X + t.Col1.Y * v.Y + t.Col2.Y * v.Z,
                t.Col0.Z * v.X + t.Col1.Z * v.Y + t.Col2.Z * v.Z
            );
        }

        public static Aabb2 operator *(Transform3 t, Aabb2 b)
        {
            var points = b.GetPoints();
            var box = new Aabb2();
            foreach (Point2 point in points)
                box.Expand(t * new Point2(point.X, point.Y));
            return box;
        }

        public static Point2 operator *(Transform3 t, Point2 p)
        {
            return new Point2(
                t.Col0.X * p.X + t.Col1.X * p.Y + t.Col3.X,
                t.Col0.Y * p.X + t.Col1.Y * p.Y + t.Col3.Y
            );
        }

        public static Point3 operator *(Transform3 t, Point3 p)
        {
            return new Point3(
                t.Col0.X * p.X + t.Col1.X * p.Y + t.Col2.X * p.Z + t.Col3.X,
                t.Col0.Y * p.X + t.Col1.Y * p.Y + t.Col2.Y * p.Z + t.Col3.Y,
                t.Col0.Z * p.X + t.Col1.Z * p.Y + t.Col2.Z * p.Z + t.Col3.Z
            );
        }
    }
}
