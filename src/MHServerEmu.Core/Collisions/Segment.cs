using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public struct Segment
    {
        public static Segment Zero { get; } = new(Vector3.Zero, Vector3.Zero);

        public Vector3 Start;
        public Vector3 End;

        public Segment()
        {
            Start = Vector3.Zero;
            End = Vector3.Zero;
        }

        public Segment(in Vector3 start, in Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Vector3 Direction => End - Start;
        public float Length =>  Vector3.Length(Direction);

        public const float Epsilon = 0.000001f;

        public static bool EpsilonTest(float val1, float val2, float epsilon = Epsilon)
        {
            return val1 >= val2 - epsilon && val1 <= val2 + epsilon;
        }

        public static bool IsNearZero(float value, float epsilon = Epsilon) => EpsilonTest(value, 0.0f, epsilon);

        public static float SegmentPointDistanceSq(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            Vector3 ba = b - a;
            Vector3 ca = c - a;
            Vector3 cb = c - b;
            float dotcb = Vector3.Dot(ca, ba);
            if (dotcb <= 0.0f) return Vector3.Dot(ca, ca);
            float dotba = Vector3.Dot(ba, ba);
            if (dotcb >= dotba) return Vector3.Dot(cb, cb);
            float dotca = Vector3.Dot(ca, ca);
            return dotca - dotcb * (dotcb / dotba);
        }

        public static float SegmentPointDistanceSq2D(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            return SegmentPointDistanceSq(a.To2D(), b.To2D(), c.To2D());
        }

        public static float SegmentPointDistance2D(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            return MathHelper.SquareRoot(SegmentPointDistanceSq2D(a, b, c));
        }

        public static float Cross2D(Vector3 v0, Vector3 v1)
        {
            return v0.X * v1.Y - v0.Y * v1.X;
        }

        public static float SegmentSegmentDistanceSq2D(in Vector3 a1, in Vector3 b1, in Vector3 a2, in Vector3 b2)
        {
            return SegmentSegmentClosestPoint(a1.To2D(), b1.To2D(), a2.To2D(), b2.To2D(), out _, out _, out _, out _);
        }

        public static Vector3 SegmentPointClosestPoint(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            // Real-Time Collision Detection p.129 (ClosestPtPointSegment)
            Vector3 ab = b - a;
            // Project c onto ab, but deferring divide by Dot(ab, ab)
            float t = Vector3.Dot(c - a, ab);
            if (t <= 0.0f)
            {   // c projects outside the [a,b] interval, on the a side; clamp to a
                return a;
            }
            else
            {
                float denom = Vector3.Dot(ab, ab); // Always nonnegative since denom = ||ab||^2
                if (t >= denom)
                {   // c projects outside the [a,b] interval, on the b side; clamp to b
                    return b;
                }
                else
                {
                    // c projects inside the [a,b] interval; must do deferred divide now
                    t /= denom;
                    return a + ab * t;
                }
            }
        }

        public static float SegmentSegmentClosestPoint(in Vector3 s1Start, in Vector3 s1End, in Vector3 s2Start, in Vector3 s2End, out float s1Dist, out float s2Dist, out Vector3 point1, out Vector3 point2)
        {
            // Based on Real-Time Collision Detection by Christer Ericson, pages 149-151 (ClosestPtSegmentSegment)

            Vector3 s1Dir = s1End - s1Start; // Direction vector of segment S1
            Vector3 s2Dir = s2End - s2Start; // Direction vector of segment S2
            Vector3 vector = s1Start - s2Start;
            float a = Vector3.Dot(s1Dir, s1Dir); // Squared length of segment S1, always nonnegative
            float e = Vector3.Dot(s2Dir, s2Dir); // Squared length of segment S2, always nonnegative
            float f = Vector3.Dot(s2Dir, vector);

            // Check if either or both segments degenerate into points
            if (a <= Epsilon && e <= Epsilon)
            {   // Both segments degenerate into points                
                s1Dist = s2Dist = 0.0f;
                point1 = s1Start;
                point2 = s2Start;
                return Vector3.Dot(vector, vector);
            }

            if (a <= Epsilon)
            {   // First segment degenerates into a point                
                s1Dist = 0.0f;
                s2Dist = f / e;
                s2Dist = Math.Clamp(s2Dist, 0.0f, 1.0f);
            }
            else
            {
                float c = Vector3.Dot(s1Dir, vector);
                if (e <= Epsilon)
                {   // Second segment degenerates into a point                    
                    s2Dist = 0.0f;
                    s1Dist = Math.Clamp(-c / a, 0.0f, 1.0f);
                }
                else
                {   // The general nondegenerate case starts here                    
                    float b = Vector3.Dot(s1Dir, s2Dir);
                    float denom = a * e - b * b; ; // Always nonnegative

                    // If segments not parallel, compute closest point on L1 to L2 and
                    // clamp to segment S1. Else pick arbitrary s (here 0)
                    if (denom != 0.0f)
                        s1Dist = Math.Clamp((b * f - c * e) / denom, 0.0f, 1.0f);
                    else
                        s1Dist = 0.0f;

                    // Compute point on L2 closest to S1(s)
                    float tnom = b * s1Dist + f;
                    if (tnom < 0.0f)
                    {
                        s2Dist = 0.0f;
                        s1Dist = Math.Clamp(-c / a, 0.0f, 1.0f);
                    }
                    else if (tnom > e)
                    {
                        s2Dist = 1.0f;
                        s1Dist = Math.Clamp((b - c) / a, 0.0f, 1.0f);
                    }
                    else
                    {
                        s2Dist = tnom / e;
                    }
                }
            }

            point1 = s1Start + s1Dir * s1Dist;
            point2 = s2Start + s2Dir * s2Dist;
            vector = point1 - point2;
            return Vector3.Dot(vector, vector);
        }

        public static bool SegmentsIntersect2D(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d)
        {
            // Real-Time Collision Detection p.152 (Test2DSegmentSegment)
            // Sign of areas correspond to which side of ab points c and d are
            float a1 = SignedDoubleTriangleArea2D(a, b, d); // Compute winding of abd (+ or -)
            float a2 = SignedDoubleTriangleArea2D(a, b, c); // To intersect, must have sign opposite of a1
            // If c and d are on different sides of ab, areas have different signs
            if (a1 * a2 < 0.0f)
            {   // Compute signs for a and b with respect to segment cd
                float a3 = SignedDoubleTriangleArea2D(c, d, a); // Compute winding of cda (+ or -)
                // Since area is constant a1 - a2 = a3 - a4, or a4 = a3 + a2 - a1
                float a4 = a3 + a2 - a1;
                // Points a and b on different sides of cd if areas have different signs
                if (a3 * a4 < 0.0f) return true; // Segments intersect.
            }
            return false; // Segments not intersecting (or collinear)
        }

        public static float SignedDoubleTriangleArea2D(in Vector3 a, in Vector3 b, in Vector3 c)
        {   
            // Returns 2 times the signed triangle area. The result is positive if
            // abc is ccw, negative if abc is cw, zero if abc is degenerate.
            return Cross2D(b - a, c - a);
        }

        public static bool LineLineIntersect2D(in Vector3 line1Start, in Vector3 line1End, in Vector3 line2Start, in Vector3 line2End, out Vector3 outPoint)
        {
            Vector3 line1Dir = line1End - line1Start;
            float ax = line1Dir.X;
            float ay = line1Dir.Y;

            float bx = line2End.X - line2Start.X;
            float by = line2End.Y - line2Start.Y;
            float cross = ax * by - ay * bx;

            if (cross == 0)
            {
                outPoint = Vector3.Zero;
                return false;
            }

            float vx = line2Start.X - line1Start.X;
            float vy = line2Start.Y - line1Start.Y;
            float line1Dist = (vx * by - vy * bx) / cross;

            outPoint = line1Start + line1Dir * line1Dist;
            return true;
        }

        public static float Lerp(float min, float max, float value)
        {
            if (value >= 0.0f && value <= 1.0f)
                return min + (max - min) * value;
            else
                return min;
        }

        public static bool RayLineIntersect2D(in Vector3 rayStart, in Vector3 rayDirection, in Vector3 lineStart, in Vector3 lineDirection, out float rayDistance, out float lineDistance)
        {
            Vector3 perpLineDir = Vector3.Perp2D(lineDirection);
            Vector3 perpRayDir = Vector3.Perp2D(rayDirection);
            float d = Vector3.Dot2D(perpLineDir, rayDirection);
            if (d != 0.0f)
            {
                Vector3 vector = lineStart - rayStart;
                rayDistance = Vector3.Dot2D(perpLineDir, vector) / d;
                lineDistance = Vector3.Dot2D(perpRayDir, vector) / d;

                return true;
            }
            rayDistance = 0.0f;
            lineDistance = 0.0f;
            return false;
        }

        public static bool RaySegmentIntersect2D(in Vector3 rayStart, in Vector3 rayDirection, in Vector3 segmentStart, in Vector3 segmentDirection, out Vector3 intersectPoint)
        {
            if (RayLineIntersect2D(rayStart, rayDirection, segmentStart, segmentDirection, out float rayDistance, out float lineDistance))
                if (rayDistance >= 0.0f && lineDistance >= 0.0f && lineDistance <= 1.0f)
                {
                    intersectPoint = rayStart.To2D() + rayDirection.To2D() * rayDistance;
                    return true;
                }
            intersectPoint = default;
            return false;
        }

        public static bool CircleTangentPoints2D(Vector3 point0, float radius, Vector3 point1, ref Segment tangent)
        {
            Vector3 p0 = point0.To2D();
            Vector3 p1 = point1.To2D();
            Vector3 dir = p1 - p0;

            float distSq = Vector3.DistanceSquared2D(p1, p0);
            if (distSq <= Epsilon)
            {
                tangent.Start = Vector3.Zero;
                tangent.End = Vector3.Zero;
                return false;
            }

            float dist = MathHelper.SquareRoot(distSq);
            float radiusSq = radius * radius;

            if (distSq < (radiusSq + Epsilon))
            {
                tangent.Start = p0 + (dir * (radius / dist));
                tangent.End = tangent.Start;
                return true;
            }
            else
            {
                float tangentDist = radiusSq / dist; // (radiusSq - (distSq - radiusSq) + distSq) / (2.0f * dist);
                float tangentLength = MathHelper.SquareRoot(radiusSq - tangentDist * tangentDist);
                Vector3 tangentPoint = p0 + dir * (tangentDist / dist);
                Vector3 tangentPerp = Vector3.Perp2D(dir * (tangentLength / dist));
                tangent.Start = tangentPoint + tangentPerp;
                tangent.End = tangentPoint - tangentPerp;
                return true;
            }
        }

        public static bool CircleTangentPoints2D(Vector3 p0, float r0, bool left0, Vector3 p1, float r1, bool left1, ref Segment tangent)
        {
            Vector3 d = p1 - p0;
            float lengthSq = Vector3.LengthSqr(d);
            float r01 = r0 + r1;
            float r10 = r1 - r0;
            float r0Sq = r0 * r0;

            float l2, l;
            Segment tangentDir;

            if (left0 == left1)
            {
                if (EpsilonTest(r10, 0.0f) == false)
                {
                    if (lengthSq <= r01 * r01) return false;

                    float r1Sq = r1 * r1;
                    float a = -r0Sq;
                    float b = 2.0f * r0Sq;
                    float c = r1 * r1 - r0Sq;
                    float discr = Math.Abs(b * b - 4.0f * a * c);
                    float root = MathHelper.SquareRoot(discr);

                    float q = (b + root) * (-0.5f / c);
                    if (q >= 0.5f)
                        l2 = Math.Abs(lengthSq - r0Sq / (q * q));
                    else
                    {
                        float nq = 1.0f - q;
                        l2 = Math.Abs(lengthSq - r1Sq / (nq * nq));
                    }
                    l = MathHelper.SquareRoot(l2);
                    tangentDir = GetTangentDirections(d, l);
                }
                else
                {
                    d /= MathHelper.SquareRoot(lengthSq);
                    tangentDir = new(d, d);
                }

                if (left0)
                {
                    tangent.Start = p0 + Vector3.Perp2D(tangentDir.Start) * r0;
                    tangent.End = p1 + Vector3.Perp2D(tangentDir.Start) * r1;
                }
                else
                {
                    tangent.Start = p0 - Vector3.Perp2D(tangentDir.End) * r0;
                    tangent.End = p1 - Vector3.Perp2D(tangentDir.End) * r1;
                }
            }
            else
            {
                if (lengthSq <= r01 * r01) return false;

                if (EpsilonTest(r10, 0.0f) == false)
                {
                    float r1Sq = r1 * r1;
                    float a = -r0Sq;
                    float b = 2.0f * r0Sq;
                    float c = r1 * r1 - r0Sq;
                    float discr = Math.Abs(b * b - 4 * a * c);
                    float root = MathHelper.SquareRoot(discr);

                    float q = (b - root) * (-0.5f / c);
                    if (q >= 0.5f)
                        l2 = Math.Abs(lengthSq - r0Sq / (q * q));
                    else
                    {
                        float nq = 1.0f - q;
                        l2 = Math.Abs(lengthSq - r1Sq / (nq * nq));                        
                    }
                }
                else
                    l2 = Math.Abs(lengthSq - 4 * r0Sq);

                l = MathHelper.SquareRoot(l2);
                tangentDir = GetTangentDirections(d, l);

                bool flip = Cross2D(tangentDir.Start, d) > 0f;

                if (left0) 
                    r1 = -r1;
                else
                    r0 = -r0;

                if (left0 ^ flip)
                {
                    tangent.Start = p0 + Vector3.Perp2D(tangentDir.Start) * r0;
                    tangent.End = p1 + Vector3.Perp2D(tangentDir.Start) * r1;
                }
                else
                {
                    tangent.Start = p0 + Vector3.Perp2D(tangentDir.End) * r0;
                    tangent.End = p1 + Vector3.Perp2D(tangentDir.End) * r1;
                }
            }

            return true;
        }

        private static Segment GetTangentDirections(Vector3 d, float l)
        {
            float a, b, root, discr, inv;

            Segment direction = new();
            float l2 = l * l;
            float dx2 = d.X * d.X;
            float dy2 = d.Y * d.Y;
            float c = dx2 + dy2;
            float invC = -0.5f / c;

            if (Math.Abs(d.X) >= Math.Abs(d.Y))
            {
                a = l2 - dx2;
                b = -2.0f * l * d.Y;
                discr = Math.Abs(b * b - 4.0f * a * c);
                root = MathHelper.SquareRoot(discr);
                inv = 1.0f / d.X;
                direction.Start.Y = (b + root) * invC;
                direction.Start.X = (l - d.Y * direction.Start.Y) * inv;
                direction.End.Y = (b - root) * invC;
                direction.End.X = (l - d.Y * direction.End.Y) * inv;
            }
            else
            {
                a = l2 - dy2;
                b = -2.0f * l * d.X;
                discr = Math.Abs(b * b - 4.0f * a * c);
                root = MathHelper.SquareRoot(discr);
                inv = 1.0f / d.Y;
                direction.Start.X = (b + root) * invC;
                direction.Start.Y = (l - d.X * direction.Start.X) * inv;
                direction.End.X = (b - root) * invC;
                direction.End.Y = (l - d.X * direction.End.X) * inv;
            }

            direction.Start.Z = 0.0f;
            direction.End.Z = 0.0f;

            return direction;
        }

        public static bool CircleTangents2D(Vector3 point0, float radius, Vector3 point1, out Segment tangent)
        {
            Vector3 p0 = point0.To2D();
            Vector3 p1 = point1.To2D();

            Segment tangentPoint = new();
            tangent = new ();
            if (CircleTangentPoints2D(p0, radius, p1, ref tangentPoint) == false) return false;

            tangent.Start = Vector3.Perp2D(Vector3.Normalize(tangentPoint.Start - p0));
            tangent.End = -Vector3.Perp2D(Vector3.Normalize(tangentPoint.End - p0));
            return true;
        }

        public static void SafeNormalAndLength2D(Vector3 v, out Vector3 outNormal, out float outLength, Vector3 safeNormal)
        {
            Vector3 v2d = v.To2D();
            if (!Vector3.IsNearZero(v2d))
            {
                float length = Vector3.Length(v2d);
                outNormal = v2d / length;
                outLength = length;
            }
            else
            {
                outNormal = safeNormal;
                outLength = 0.0f;
            }
        }

        public static float LinePointSide2D(in Vector3 start, in Vector3 end, in Vector3 point)
        {
            return Cross2D(start - end, point - end);
        }
    }

}
