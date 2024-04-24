using System.Text;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public class Triangle
    {
        public Vector3[] Points { get; set; } = new Vector3[3];

        public Triangle(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Points[0] = p0;
            Points[1] = p1;
            Points[2] = p2;
        }

        public static Triangle Zero => new(Vector3.Zero, Vector3.Zero, Vector3.Zero);

        public Vector3 this[int index]
        {
            get
            {
                if (index < 3) return Points[index];
                throw new IndexOutOfRangeException("Invalid index for Triangle");
            }
            set
            {
                if (index < 3) Points[index] = value;
                throw new IndexOutOfRangeException("Invalid index for Triangle");
            }
        }

        public Plane ToPlane()
        {
            Vector3 n = GetNormal();
            float d = Vector3.Dot(n, Points[0]);
            return new(n, d);
        }

        public Vector3 GetNormal()
        {
            Vector3 edge1 = Points[1] - Points[0];
            Vector3 edge2 = Points[2] - Points[0];
            return Vector3.Normalize(Vector3.Cross(edge1, edge2));
        }

        public bool Intersects(Aabb other)
        {
            Vector3 c = other.Center;
            Vector3 e = other.Extents;

            float ex = e.X;
            float ey = e.Y;
            float ez = e.Z;

            Vector3 v0 = Points[0] - c;
            Vector3 v1 = Points[1] - c;
            Vector3 v2 = Points[2] - c;

            Vector3 d0 = v1 - v0;
            Vector3 d1 = v2 - v1;
            Vector3 d2 = v0 - v2;

            float p0, p1, p2, r;
            float ax, ay, az;
            float min, max;

            ax = Math.Abs(d0.X);
            ay = Math.Abs(d0.Y);
            az = Math.Abs(d0.Z);

            p0 = d0.Z * v0.Y - d0.Y * v0.Z;
            p2 = d0.Z * v2.Y - d0.Y * v2.Z;
            r = az * ey + ay * ez;
            (min, max) = p0 < p2 ? (p0, p2) : (p2, p0);
            if (min > r || max < -r) return false;

            p0 = -d0.Z * v0.X + d0.X * v0.Z;
            p2 = -d0.Z * v2.X + d0.X * v2.Z;
            r = az * ex + ax * ez;
            (min, max) = p0 < p2 ? (p0, p2) : (p2, p0);
            if (min > r || max < -r) return false;

            p1 = d0.Y * v1.X - d0.X * v1.Y;
            p2 = d0.Y * v2.X - d0.X * v2.Y;
            r = ay * ex + ax * ey;
            (min, max) = p2 < p1 ? (p2, p1) : (p1, p2);
            if (min > r || max < -r) return false;

            ax = Math.Abs(d1.X);
            ay = Math.Abs(d1.Y);
            az = Math.Abs(d1.Z);

            p0 = d1.Z * v0.Y - d1.Y * v0.Z;
            p2 = d1.Z * v2.Y - d1.Y * v2.Z;
            r = az * ey + ay * ez;
            (min, max) = p0 < p2 ? (p0, p2) : (p2, p0);
            if (min > r || max < -r) return false;

            p0 = -d1.Z * v0.X + d1.X * v0.Z;
            p2 = -d1.Z * v2.X + d1.X * v2.Z;
            r = az * ex + ax * ez;
            (min, max) = p0 < p2 ? (p0, p2) : (p2, p0);
            if (min > r || max < -r) return false;

            p0 = d1.Y * v0.X - d1.X * v0.Y;
            p1 = d1.Y * v1.X - d1.X * v1.Y;
            r = ay * ex + ax * ey;
            (min, max) = p0 < p1 ? (p0, p1) : (p1, p0);
            if (min > r || max < -r) return false;

            ax = Math.Abs(d2.X);
            ay = Math.Abs(d2.Y);
            az = Math.Abs(d2.Z);

            p0 = d2.Z * v0.Y - d2.Y * v0.Z;
            p1 = d2.Z * v1.Y - d2.Y * v1.Z;
            r = az * ey + ay * ez;
            (min, max) = p0 < p1 ? (p0, p1) : (p1, p0);
            if (min > r || max < -r) return false;

            p0 = -d2.Z * v0.X + d2.X * v0.Z;
            p1 = -d2.Z * v1.X + d2.X * v1.Z;
            r = az * ex + ax * ez;
            (min, max) = p0 < p1 ? (p0, p1) : (p1, p0);
            if (min > r || max < -r) return false;

            p1 = d2.Y * v1.X - d2.X * v1.Y;
            p2 = d2.Y * v2.X - d2.X * v2.Y;
            r = ay * ex + ax * ey;
            (min, max) = p2 < p1 ? (p2, p1) : (p1, p2);
            if (min > r || max < -r) return false;


            if (Math.Max(Math.Max(v0.X, v1.X), v2.X) < -ex || Math.Min(Math.Min(v0.X, v1.X), v2.X) > ex) return false;
            if (Math.Max(Math.Max(v0.Y, v1.Y), v2.Y) < -ey || Math.Min(Math.Min(v0.Y, v1.Y), v2.Y) > ey) return false;
            if (Math.Max(Math.Max(v0.Z, v1.Z), v2.Z) < -ez || Math.Min(Math.Min(v0.Z, v1.Z), v2.Z) > ez) return false;

            return ToPlane().Intersects(other) == Plane.IntersectionType.Intersect;
        }

        public bool Intersects(Obb obb)
        {
            return obb.Intersects(this);
        }

        public bool Intersects(Sphere sphere)
        {
            return TriangleIntersectsCircle2D(sphere.Center, sphere.Radius);
        }

        public bool Intersects(Capsule capsule)
        {
            return capsule.Intersects(this);
        }

        public bool Intersects(Triangle triangle)
        {
            throw new NotImplementedException("We don't want Triangle-Triangle bounds intersection happening right now...");
        }

        public bool TriangleIntersectsCircle2D(Vector3 center, float radius)
        {
            if (TriangleContainsPoint2D(center)) return true;

            Vector3 p0 = Points[0].To2D();
            Vector3 p2 = Points[1].To2D();
            Vector3 p3 = Points[2].To2D();
            Vector3 center2D = center.To2D();

            float radiusSq = radius * radius;
            if (Segment.SegmentPointDistanceSq(p0, p2, center2D) < radiusSq) return true;
            if (Segment.SegmentPointDistanceSq(p2, p3, center2D) < radiusSq) return true;
            if (Segment.SegmentPointDistanceSq(p3, p0, center2D) < radiusSq) return true;

            return false;
        }

        public bool TriangleContainsPoint2D(Vector3 p)
        {
            Vector3 p0 = Points[0];
            Vector3 p1 = Points[1];
            Vector3 p2 = Points[2];
            Vector3 p10 = p1 - p0;
            Vector3 p20 = p2 - p0;

            if (Segment.Cross2D(p10, p20) > 0.0f)
            {
                if (Segment.Cross2D(p - p0, p1 - p0) > 0.0f) return false;
                if (Segment.Cross2D(p - p1, p2 - p1) > 0.0f) return false;
                if (Segment.Cross2D(p - p2, p0 - p2) > 0.0f) return false;
            }
            else
            {
                if (Segment.Cross2D(p - p0, p1 - p0) < 0.0f) return false;
                if (Segment.Cross2D(p - p1, p2 - p1) < 0.0f) return false;
                if (Segment.Cross2D(p - p2, p0 - p2) < 0.0f) return false;
            }
            return true;
        }
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($" P0: {Points[0]}");
            sb.AppendLine($" P1: {Points[1]}");
            sb.AppendLine($" P2: {Points[2]}");
            return sb.ToString();
        }
    }
}
