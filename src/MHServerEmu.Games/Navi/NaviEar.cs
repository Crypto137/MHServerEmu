using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public class NaviEar : IComparable<NaviEar>
    {
        public float Power;
        public NaviPoint Point;
        public NaviEdge Edge;
        public int PrevIndex;
        public int NextIndex;

        public int CompareTo(NaviEar other)
        {
            return other.Power.CompareTo(Power);
        }

        public void CalcPower(List<NaviEar> listEar, Vector3 point)
        {
            var p0 = listEar[PrevIndex].Point; 
            var p1 = Point;
            var p2 = listEar[NextIndex].Point;

            if (Pred.IsDegenerate(p0, p1, p2) == false)
                Power = Pred.CalcEarPower(p0.Pos, p1.Pos, p2.Pos, point);
            else
                Power = float.MaxValue;
        }
    }
}
