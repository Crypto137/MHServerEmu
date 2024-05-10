using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Behavior
{
    public class Combat
    {
        private static bool CheckAggroDistance(Agent agressor, WorldEntity target, float distance)
        {
            if (distance > 0.0)
            {
                float distanceSq = distance * distance;
                if (distanceSq < Vector3.DistanceSquared2D(agressor.RegionLocation.Position, target.RegionLocation.Position)) 
                    return false;
            }
            return true;
        }
    }

    [Flags]
    public enum CombatTargetFlags
    {
        None = 0,

    }

    public enum CombatTargetType
    {
        Hostile,
        Ally
    }
}
