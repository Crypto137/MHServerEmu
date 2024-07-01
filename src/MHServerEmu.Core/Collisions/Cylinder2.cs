using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public class Cylinder2
    {
        public Vector3 Center;
        public float HalfHeight;
        public float Radius;

        public Cylinder2(Vector3 center, float halfHeight, float radius)
        {
            Center = center;
            HalfHeight = halfHeight;
            Radius = radius;
        }

        public bool Sweep(Vector3 velocity, Aabb bound, ref float resultTime, ref Vector3? resultNormal)
        {
            float posZ = Center.Z;
            float minZ = posZ - HalfHeight;
            float maxZ = posZ + HalfHeight;
            Vector3 min = bound.Min;
            Vector3 max = bound.Max;

            if (min.Z > maxZ || max.Z < minZ)  return false;

            if (min.Y <= Center.Y && max.Y >= Center.Y &&
                min.X <= Center.X && max.X >= Center.X)
            {
                resultTime = 0.0f;
                resultNormal = Vector3.ZAxis;
                return true;
            }

            Stack<Vector3> points = new();
            if (velocity.X != 0.0f)
            {
                float x, distX, minTimeX;
                if (velocity.X > 0.0f)
                {
                    x = min.X;
                    distX = x - (Center.X + Radius);
                    minTimeX = -(Radius / velocity.X);
                }
                else
                {
                    x = max.X;
                    distX = (x + Radius) - Center.X;
                    minTimeX = Radius / velocity.X;
                }
                float time = distX / velocity.X;
                if (time <= 1.0f)
                {
                    float posY = Center.Y + velocity.Y * time;
                    if (time >= minTimeX && min.Y <= posY && max.Y >= posY)
                    {
                        resultTime = Math.Max(time, 0.0f);
                        resultNormal = (velocity.X > 0.0f) ? Vector3.XAxisNeg : Vector3.XAxis;
                        return true;
                    }
                    if (posY < min.Y)
                        points.Push(new(x, min.Y, 0.0f));
                    else
                        points.Push(new(x, max.Y, 0.0f));
                }
            }

            if (velocity.Y != 0.0f)
            {
                float y, distY, minTimeY;
                if (velocity.Y > 0.0f)
                {
                    y = min.Y;
                    distY = y - (Center.Y + Radius);
                    minTimeY = -(Radius / velocity.Y);
                }
                else
                {
                    y = max.Y;
                    distY = (y + Radius) - Center.Y;
                    minTimeY = (Radius / velocity.Y);
                }
                float time = distY / velocity.Y;
                if (time <= 1.0f)
                {
                    float posX = Center.X + velocity.X * time;
                    if (time >= minTimeY && min.X <= posX && max.X >= posX)
                    {
                        resultTime = Math.Max(time, 0.0f);
                        resultNormal = (velocity.Y > 0.0f) ? Vector3.YAxisNeg : Vector3.YAxis;
                        return true;
                    }
                    if (posX < min.X)
                        points.Push(new(min.X, y, 0.0f));
                    else
                        points.Push(new(max.X, y, 0.0f));
                }
            }

            Vector3 minPoint = new();
            float minTime = float.MaxValue;
            while (points.Count > 0)
            {
                Vector3 point = points.Pop();
                float time = 0.0f;
                Circle2 circle = new(Center, Radius);
                if (circle.Sweep(velocity, point, ref time) && time < minTime)
                {
                    minTime = time;
                    minPoint = point;
                }
            }

            if (minTime < float.MaxValue)
            {
                Vector3 hitPoint = Center + velocity * minTime;
                resultTime = minTime;
                resultNormal = Vector3.SafeNormalize2D(hitPoint - minPoint, Vector3.ZAxis);
                return true;
            }
            return false;
        }

    }
}
