
namespace MHServerEmu.Games.Navi
{
    public class NaviSweep
    {

    }

    public enum HeightSweepType
    {
        None,
        Constraint
    }

    public enum SweepResult
    {
        Success = 0,
        Clipped = 1,
        HeightMap = 2,
        Failed = 3
    }
}
