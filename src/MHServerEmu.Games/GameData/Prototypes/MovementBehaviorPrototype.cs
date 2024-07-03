using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class MovementBehaviorPrototype : Prototype
    {
        public virtual void OnEndPower(in Context context) { }

        public readonly struct Context
        {
            public readonly Power Power;
            public readonly WorldEntity User;
            public readonly WorldEntity Target;
            public readonly Vector3 TargetPosition;

            public Context(Power power, WorldEntity user, WorldEntity target, Vector3 targetPosition)
            {
                Power = power;
                User = user;
                Target = target;
                TargetPosition = targetPosition;
            }
        }
    }

    public class StrafeTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeDistanceMult { get; protected set; }

        public override void OnEndPower(in Context context)
        {
            if (context.Target == null) return;
            Vector3 userPosition = context.User.RegionLocation.Position;
            Vector3 targetPosition = context.Target.RegionLocation.Position;
            context.User.OrientForPower(context.Power, targetPosition, userPosition);
        }
    }

    public class RandomPositionAroundTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeAngle { get; protected set; }
    }

    public class FixedRotationPrototype : MovementBehaviorPrototype
    {
        public float RotationSpeed { get; protected set; }
        public float PivotAngle { get; protected set; }
        public int MaxPivotTimeMS { get; protected set; }
        public float PostPivotAcceleration { get; protected set; }
    }
}
