using System.Text;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public struct Obb
    {
        public Vector3 Center;
        public Vector3 Extents;
        public Orientation Orientation;
        public Matrix3 InverseRotationMatrix;
        public Matrix3 RotationMatrix => Orientation.GetMatrix3();

        public Obb(Vector3 center, Vector3 extents, Orientation orientation)
        {
            Center = center;
            Extents = extents;
            Orientation = orientation;
            InverseRotationMatrix = Matrix3.Inverse(orientation.GetMatrix3());
        }

        public Vector3 TransformPoint(Vector3 point)
        {
            return InverseRotationMatrix * (point - Center) + Center;
        }

        public Vector3 TransformVector(Vector3 vec)
        {
            return InverseRotationMatrix * vec;
        }

        public Aabb ToAabb()
        {
            Vector3 oobVector = Matrix3.AbsPerElem(RotationMatrix) * Extents;
            return new Aabb(Center - oobVector, Center + oobVector);
        }

        public bool Intersects(in Aabb aabb)
        {
            return ToAabb().Intersects(aabb);
        }

        public bool Intersects(in Capsule capsule)
        {
            return capsule.Intersects(this);
        }

        public bool Intersects(in Obb obb)
        {
            return ToAabb().Intersects(obb.ToAabb());
        }

        public bool Intersects(in Sphere sphere)
        {
            Aabb aabb = new(Center - Extents, Center + Extents);
            Vector3 center = TransformPoint(sphere.Center);
            return aabb.Intersects(new Sphere(center, sphere.Radius));
        }

        public bool Intersects(in Triangle triangle)
        {
            Aabb aabb = new(Center - Extents, Center + Extents);
            var otherTriangle = new Triangle(
                TransformPoint(triangle.Points[0]),
                TransformPoint(triangle.Points[1]),
                TransformPoint(triangle.Points[2])
                );
            return aabb.Intersects(otherTriangle);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            //sb.AppendLine($"Center: {Center.ToStringFloat()}");
            sb.AppendLine($" Box: {Extents}");
            //sb.AppendLine($"Orientation: {Orientation.ToStringFloat()}");
            return sb.ToString();
        }
    }
}
