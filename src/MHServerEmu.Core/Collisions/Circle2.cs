using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public struct Circle2
    {
        public Vector3 Center;
        public float Radius;

        public Circle2(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public bool Sweep(Vector3 velocity, Vector3 point, ref float time)
        {
            float a = Vector3.LengthSquared2D(velocity);
            if (a < Segment.Epsilon) return false;
            a = MathHelper.SquareRoot(a);
            Vector3 d = velocity / a;
            Vector3 v = (Center - point).To2D();

            float b = Vector3.Dot(v, d);
            float c = Vector3.Dot(v, v) - Radius * Radius;
            if (c > 0.0f && b > 0.0f) return false;

            float discr = b * b - c;
            if (discr > 0.0f)
            {
                time = (-b - MathHelper.SquareRoot(discr)) / a;
                if (time < 0.0f) time = 0.0f;
                return (time <= 1.0f);
            }
            return false;
        }

    }
}
