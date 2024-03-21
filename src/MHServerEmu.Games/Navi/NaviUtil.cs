
namespace MHServerEmu.Games.Navi
{
    public class NaviUtil
    {
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
    }

}
