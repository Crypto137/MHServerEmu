using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class Locomotor
    {
        public static PathFlags GetPathFlags(LocomotorMethod naviMethod)
        {
            return naviMethod switch
            {
                LocomotorMethod.Ground or LocomotorMethod.Airborne => PathFlags.Walk,
                LocomotorMethod.TallGround => PathFlags.TallWalk,
                LocomotorMethod.Missile or LocomotorMethod.MissileSeeking => PathFlags.Power,
                LocomotorMethod.HighFlying => PathFlags.Fly,
                _ => PathFlags.None,
            };
        }
    }
}
