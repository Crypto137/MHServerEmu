
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
                nextEdge = next.Edge((oppoEdgeIndex + 1) % 3);
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

    }

}
