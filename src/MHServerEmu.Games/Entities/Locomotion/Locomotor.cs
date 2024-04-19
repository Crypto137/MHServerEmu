using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class Locomotor
    {
        public const float MovementSweepPadding = 0.5f;
        public LocomotionState LocomotionState { get; private set; }
        public bool MovementImpeded { get; set; }
        public bool IsMoving { get; private set; }
        public bool IsLocomoting { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting); }
        public bool IsWalking { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsWalking); }
        public bool IsLooking { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsLooking); }
        public bool HasLocomotionNoEntityCollide { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.LocomotionNoEntityCollide); }
        public bool IsMovementPower { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsMovementPower); }
        public bool IsDrivingMovementMode { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsDrivingMovementMode); }
        public bool IsHighFlying { get => LocomotionState.Method == LocomotorMethod.HighFlying; }
        public bool IsSyncMoving { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IsSyncMoving); }
        public bool IgnoresWorldCollision { get => LocomotionState.LocomotionFlags.HasFlag(LocomotionFlags.IgnoresWorldCollision); }
        public int CurrentMoveHeight { get => LocomotionState.Height; }
        public bool IsMissile { get => LocomotionState.Method == LocomotorMethod.Missile || LocomotionState.Method == LocomotorMethod.MissileSeeking; }
        public bool IsSeekingMissile { get => LocomotionState.Method == LocomotorMethod.MissileSeeking; }
        public PathFlags PathFlags { get => GetPathFlags(LocomotionState.Method); }
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

        internal SweepResult SweepFromTo(Vector3 fromPosition, Vector3 toPosition, ref Vector3 resultPosition, float movementSweepPadding = MovementSweepPadding)
        {
            throw new NotImplementedException();
        }

        internal SweepResult SweepTo(Vector3 toPosition, ref Vector3 resultPosition, ref Vector3 resultNormal, float movementSweepPadding = MovementSweepPadding)
        {
            throw new NotImplementedException();
        }
    }
}
