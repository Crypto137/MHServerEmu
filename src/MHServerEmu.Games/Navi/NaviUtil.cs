
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public class NaviUtil
    {
        public static bool IsPointConstraint(NaviPoint point, NaviTriangle triangle)
        {
            NaviTriangle next = triangle;
            NaviTriangle triEnd = triangle;
            NaviEdge nextEdge;
            do
            {
                int oppoEdgeIndex = next.OpposedEdgeIndex(point);
                nextEdge = next.EdgeMod(oppoEdgeIndex + 1);
                if (nextEdge.TestFlag(NaviEdgeFlags.Constraint)) return true;
                next = next.NextTriangleSharingPoint(point);
            } while (next != triEnd);

            return false;
        }

        public static double FindMaxValue(double d0, double d1, double d2, out int edgeIndex)
        {
            if (d1 > d0)
            {
                if (d1 > d2)
                {
                    edgeIndex = 1;
                    return d1;
                }
                else
                {
                    edgeIndex = 2;
                    return d2;
                }
            }
            else
            {
                if (d2 > d0)
                {
                    edgeIndex = 2;
                    return d2;
                }
                else
                {
                    edgeIndex = 0;
                    return d0;
                }
            }
        }

        public static Vector3 ProjectToPlane(NaviTriangle triangle, Vector3 pos)
        {            
            Plane plane = new (triangle.PointCW(0).Pos, triangle.PointCW(1).Pos, triangle.PointCW(2).Pos);
            float z = plane.SolveForZ(pos.X, pos.Y);
            return new Vector3(pos.X, pos.Y, z);
        }

        public static bool NaviInteriorContainsCircle<T>(NaviCdt naviCdt, Vector3 position, float radius, NaviTriangle triangle, T flagsCheck) where T : IContainsPathFlagsCheck
        {
            if (flagsCheck.PathingFlagsCheck(triangle.PathingFlags) == false) return false;
            if (radius > 0.0f)
            {
                NaviSerialCheck naviSerialCheck = new(naviCdt);
                for (int i = 0; i < 3; ++i)
                {
                    if (NaviInteriorContainsCircleInternal(naviSerialCheck, position, radius, triangle, triangle.Edges[i], flagsCheck) == false)
                        return false;
                }
            }
            return true;
        }

        public static bool NaviInteriorContainsCircleInternal<T>(NaviSerialCheck serialCheck, Vector3 position, float radius, NaviTriangle triangle, NaviEdge edge, T flagsCheck) where T : IContainsPathFlagsCheck
        {
            if (edge.TestOperationSerial(serialCheck) == false) return true;

            if (Segment.SegmentPointDistance2D(edge.Points[0].Pos, edge.Points[1].Pos, position) > radius) return true;

            NaviTriangle triOppo = edge.OpposedTriangle(triangle);

            if (edge.TestFlag(NaviEdgeFlags.Constraint))
                if (triOppo == null || flagsCheck.PathingFlagsCheck(triOppo.PathingFlags) == false)
			        return false;

            int edgeIndex = triOppo.EdgeIndex(edge);
            bool contains = NaviInteriorContainsCircleInternal(serialCheck, position, radius, triOppo, triOppo.EdgeMod(edgeIndex + 1), flagsCheck);
            if (contains) contains = NaviInteriorContainsCircleInternal(serialCheck, position, radius, triOppo, triOppo.EdgeMod(edgeIndex + 2), flagsCheck);

            return contains;
        }

        public static float TriangleArcPathDistance(NaviEdge edge1, NaviEdge edge2, float radius)
        {
            Vector3 a, b, c;
            if (edge1.Points[0] == edge2.Points[0])
            {
                a = edge1.Point(1);
                b = edge1.Point(0);
                c = edge2.Point(1);
            }
            else if (edge1.Points[0] == edge2.Points[1])
            {
                a = edge1.Point(1);
                b = edge1.Point(0);
                c = edge2.Point(0);
            }
            else if (edge1.Points[1] == edge2.Points[0])
            {
                a = edge1.Point(0);
                b = edge1.Point(1);
                c = edge2.Point(1);
            }
            else if (edge1.Points[1] == edge2.Points[1])
            {
                a = edge1.Point(0);
                b = edge1.Point(1);
                c = edge2.Point(0);
            }
            else
                return 0.0f;

            Vector3 ab = Vector3.Normalize2D(a - b);
            Vector3 cb = Vector3.Normalize2D(c - b);
            return radius * MathF.Acos(Vector3.Dot(ab, cb));
        }

        public static bool IsAngleObtuse2D(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return Vector3.Dot2D(p0 - p1, p2 - p1) < 0.0f;
        }
    }

}
