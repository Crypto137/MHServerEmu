using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Collisions
{
    public class Segment
    {
        public static readonly Segment Zero = new(Vector3.Zero, Vector3.Zero);

        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }

        public Segment()
        {
            Start = Vector3.Zero;
            End = Vector3.Zero;
        }

        public Segment(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Segment(Segment segment)
        {
            Start = segment.Start;
            End = segment.End;
        }

        public Vector3 Direction => End - Start;
        public float Length =>  Vector3.Length(Direction);

        public void Set(Segment segment)
        {
            Start.Set(segment.Start);
            End.Set(segment.End);
        }

        public const float Epsilon = 0.000001f;

        public static bool EpsilonTest(float val1, float val2, float epsilon = Epsilon)
        {
            return val1 >= val2 - epsilon && val1 <= val2 + epsilon;
        }

        public static bool IsNearZero(float value, float epsilon = Epsilon) => EpsilonTest(value, 0.0f, epsilon);

        public static float SegmentPointDistanceSq(Vector3 a, Vector3 b, Vector3 c)
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

        public static float SegmentPointDistanceSq2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return SegmentPointDistanceSq(a.To2D(), b.To2D(), c.To2D());
        }

        public static float SegmentPointDistance2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return MathHelper.SquareRoot(SegmentPointDistanceSq2D(a, b, c));
        }

        public static float Cross2D(Vector3 v0, Vector3 v1)
        {
            return v0.X * v1.Y - v0.Y * v1.X;
        }

        public static float SegmentSegmentDistanceSq2D(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2)
        {
            return SegmentSegmentClosestPoint(a1.To2D(), b1.To2D(), a2.To2D(), b2.To2D(), out _, out _, out _, out _);
        }

        public static float SegmentSegmentClosestPoint(Vector3 s1Start, Vector3 s1End, Vector3 s2Start, Vector3 s2End, out float s1Dist, out float s2Dist, out Vector3 point1, out Vector3 point2)
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

        public static bool SegmentsIntersect2D(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
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

        public static float SignedDoubleTriangleArea2D(Vector3 a, Vector3 b, Vector3 c)
        {   
            // Returns 2 times the signed triangle area. The result is positive if
            // abc is ccw, negative if abc is cw, zero if abc is degenerate.
            return Cross2D(b - a, c - a);
        }

        public static bool LineLineIntersect2D(Vector3 line1Start, Vector3 line1End, Vector3 line2Start, Vector3 line2End, out Vector3 outPoint)
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

        public static bool RayLineIntersect2D(Vector3 rayStart, Vector3 rayDirection, Vector3 lineStart, Vector3 lineDirection, out float rayDistance, out float lineDistance)
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

        public static bool RaySegmentIntersect2D(Vector3 rayStart, Vector3 rayDirection, Vector3 segmentStart, Vector3 segmentDirection, out Vector3 intersectPoint)
        {
            if (RayLineIntersect2D(rayStart, rayDirection, segmentStart, segmentDirection, out float rayDistance, out float lineDistance))
                if (rayDistance >= 0.0f && lineDistance >= 0.0f && lineDistance <= 1.0f)
                {
                    intersectPoint = rayStart.To2D() + rayDirection.To2D() * rayDistance;
                    return true;
                }
            intersectPoint = null;
            return false;
        }

    }

}
