
using System;

namespace MHServerEmu.Games.Common
{
    public class Transform3
    {
        public Vector3 Col0 { get; set; }
        public Vector3 Col1 { get; set; }
        public Vector3 Col2 { get; set; }
        public Vector3 Col3 { get; set; }

        public Vector3 Translation { 
            get {
                return Col3;
            } 
            set { 
                Col3 = value;
            } 
        }

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
            return new (
                Vector3.XAxis,
                Vector3.YAxis,
                Vector3.ZAxis,
                new Vector3(0.0f)
            );
        }

        public static Transform3 BuildTransform(Vector3 translation, Vector3 rotation)
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
            tmp0 = (cZ * sY);
            tmp1 = (sZ * sY);

            return new (
                new Vector3((cZ * cY), (sZ * cY), -sY),
                new Vector3(((tmp0 * sX) - (sZ * cX)), ((tmp1 * sX) + (cZ * cX)), (cY * sX)),
                new Vector3(((tmp0 * cX) + (sZ * sX)), ((tmp1 * cX) - (cZ * sX)), (cY * cX)),
                new Vector3(0.0f)
            );
        }

        public static Point3 operator *(Transform3 t, Point3 p)
        {
            return new Point3(
                (t.Col0.X * p.X) + (t.Col1.X * p.Y) + (t.Col2.X * p.Z) + t.Col3.X,
                (t.Col0.Y * p.X) + (t.Col1.Y * p.Y) + (t.Col2.Y * p.Z) + t.Col3.Y,
                (t.Col0.Z * p.X) + (t.Col1.Z * p.Y) + (t.Col2.Z * p.Z) + t.Col3.Z
            );
        }
    }
}
