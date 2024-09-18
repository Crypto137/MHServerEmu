using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class MovementBehaviorPrototype : Prototype
    {
        public virtual void OnEndPower(in Context context) { }
        public virtual bool GenerateTargetPosition(in Context context, ref Vector3 targetPositionResult) { return false; }

        public readonly struct Context
        {
            public readonly Power Power;
            public readonly WorldEntity Owner;
            public readonly WorldEntity Target;
            public readonly Vector3 TargetPosition;

            public Context(Power power, WorldEntity owner, WorldEntity target, Vector3 targetPosition)
            {
                Power = power;
                Owner = owner;
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
            Vector3 userPosition = context.Owner.RegionLocation.Position;
            Vector3 targetPosition = context.Target.RegionLocation.Position;
            context.Owner.OrientForPower(context.Power, targetPosition, userPosition);
        }

        public override bool GenerateTargetPosition(in Context context, ref Vector3 targetPositionResult)
        {
            var target = context.Target;
            var owner = context.Owner;
            Vector3 ownerPosition = context.Owner.RegionLocation.Position;

            if (target == null || target == owner || target.IsInWorld == false)
            {
                float offsetRadians = MathHelper.ToRadians(90.0f);
                Vector3 direction = Vector3.AxisAngleRotate(owner.Forward, Vector3.Up, offsetRadians);
                targetPositionResult = ownerPosition + direction * (context.Power.GetKnockbackDistance(owner) * StrafeDistanceMult);
                return true;
            }

            if (target == null) return false;

            Vector3 toTarget = target.RegionLocation.Position - ownerPosition;
            float side1 = Vector3.Length2D(toTarget);
            float knockbackDistance = context.Power.GetKnockbackDistance(owner) * StrafeDistanceMult;
            float side2 = knockbackDistance / 2.0f;

            float dirAngle = side1 > side2
                ? MathF.Asin(Math.Min(1.0f, side2 / side1))
                : MathF.Asin(Math.Min(1.0f, side1 / side2));

            float maxAngle = MathHelper.ToRadians(30.0f);
            float angle = Math.Max(MathHelper.Pi - (dirAngle + MathHelper.Pi / 2.0f), maxAngle);

            Vector3 targetDirection = Vector3.Normalize(Vector3.AxisAngleRotate(toTarget, owner.GetUp, angle));

            targetPositionResult = ownerPosition + (targetDirection * knockbackDistance);
            return true;
        }
    }

    public class RandomPositionAroundTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeAngle { get; protected set; }

        public override bool GenerateTargetPosition(in Context context, ref Vector3 targetPositionResult)
        {
            var target = context.Target;
            var owner = context.Owner;
            Vector3 ownerPosition = owner.RegionLocation.Position;

            if (target == null || target == owner || target.IsInWorld == false)
            {
                float angle = MathHelper.ToRadians(90.0f);
                Vector3 direction = Vector3.AxisAngleRotate(owner.Forward, Vector3.Up, angle);
                targetPositionResult = ownerPosition + direction * context.Power.GetKnockbackDistance(owner);
                return true;
            }

            var game = owner.Game;
            if (game == null) return false;
            var random = game.Random;
            if (target == null) return false;

            Vector3 toTarget = target.RegionLocation.Position - ownerPosition;
            float strafeAngle = MathHelper.ToRadians(random.Next() % 2 == 0 ? -StrafeAngle : StrafeAngle);
            Vector3 targetDirection = Vector3.AxisAngleRotate(toTarget, Vector3.Up, strafeAngle);
            targetDirection = target.RegionLocation.Position + targetDirection - ownerPosition;

            targetPositionResult = ownerPosition + targetDirection;
            return true;
        }
    }

    public class FixedRotationPrototype : MovementBehaviorPrototype
    {
        public float RotationSpeed { get; protected set; }
        public float PivotAngle { get; protected set; }
        public int MaxPivotTimeMS { get; protected set; }
        public float PostPivotAcceleration { get; protected set; }

        public override bool GenerateTargetPosition(in Context context, ref Vector3 targetPositionResult)
        {
            targetPositionResult = context.Owner.RegionLocation.Position;
            return true;
        }
    }
}
