using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class Locomotor
    {
        public static PathFlags GetPathFlags(LocomotorMethod naviMethod)
        {
            return naviMethod switch
            {
                LocomotorMethod.Ground or LocomotorMethod.Airborne => PathFlags.flag1,
                LocomotorMethod.TallGround => PathFlags.flag16,
                LocomotorMethod.Missile or LocomotorMethod.MissileSeeking => PathFlags.flag4,
                LocomotorMethod.HighFlying => PathFlags.flag2,
                _ => PathFlags.None,
            };
        }
    }
}
