using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Rotate : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static Rotate Instance { get; } = new();
        private Rotate() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            var blackboard = ownerController.Blackboard;
            var agent = ownerController.Owner;
            if (agent == null) return;

            var locomotor = agent.Locomotor;
            locomotor?.Stop();

            agent.Properties[PropertyEnum.RotationSpeedOverride] = blackboard.PropertyCollection[PropertyEnum.AIOldRotationOverride];
            blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIOldRotationOverride);
            blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIRotationAccum);
        }

        public void Start(in IStateContext context)
        {
            if (context is not RotateContext rotateContext) return;
            var ownerController = rotateContext.OwnerController;
            if (ownerController == null) return;
            var agent = ownerController.Owner;
            if (agent == null) return;
            var locomotor = agent.Locomotor;
            if (locomotor == null) return;

            locomotor.Stop();

            ownerController.Blackboard.PropertyCollection[PropertyEnum.AIOldRotationOverride] = agent.Properties[PropertyEnum.RotationSpeedOverride];
            agent.Properties[PropertyEnum.RotationSpeedOverride] = rotateContext.SpeedOverride;
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not RotateContext rotateContext) return failResult;
            var ownerController = rotateContext.OwnerController;
            if (ownerController == null) return failResult;
            var agent = ownerController.Owner;
            if (agent == null) return failResult;
            var locomotor = agent.Locomotor;
            if (locomotor == null) return failResult;

            if (rotateContext.RotateTowardsTarget)
                if (GetLookAtPoint(rotateContext, ownerController, agent, out Vector3 lookAtPoint))
                {
                    locomotor.LookAt(lookAtPoint);
                    return StaticBehaviorReturnType.Running;
                }

            if (locomotor.IsLooking)
                return StaticBehaviorReturnType.Running;
            else if (rotateContext.RotateTowardsTarget == false)
            {
                var blackboard = ownerController.Blackboard;
                if (blackboard.PropertyCollection[PropertyEnum.AIRotationAccum] < rotateContext.Degrees)
                    if (GetLookAtPoint(rotateContext, ownerController, agent, out Vector3 lookAtPoint))
                    {
                        locomotor.LookAt(lookAtPoint);
                        return StaticBehaviorReturnType.Running;
                    }
            }

            return StaticBehaviorReturnType.Completed;
        }

        private static bool GetLookAtPoint(RotateContext rotateContext, AIController ownerController, Agent agent, out Vector3 lookAtPoint)
        {
            lookAtPoint = Vector3.Zero;
            if (agent == null || ownerController == null) return false;

            bool result = true;
            if (rotateContext.RotateTowardsTarget)
            {
                var senses = ownerController.Senses;
                var targetEntity = senses.GetCurrentTarget();

                if (targetEntity != null && targetEntity.IsInWorld && targetEntity != agent)
                {
                    lookAtPoint = targetEntity.RegionLocation.Position;
                    lookAtPoint.Z = agent.RegionLocation.Position.Z;
                }
                else
                {
                    result = false;
                }
            }
            else
            {
                var blackboard = ownerController.Blackboard;
                int rotationAccum = blackboard.PropertyCollection[PropertyEnum.AIRotationAccum];
                int degrees = Math.Min(179, rotateContext.Degrees - rotationAccum);
                blackboard.PropertyCollection[PropertyEnum.AIRotationAccum] = rotationAccum + degrees;

                if (rotateContext.Clockwise == false)
                    degrees *= -1;

                Vector3 forward = agent.Forward; 
                Matrix3 rotationMatrix = Matrix3.RotationZ(MathHelper.ToRadians(degrees));
                Vector3 targetPosition = rotationMatrix * forward;
                targetPosition.Z = 0f;

                lookAtPoint = agent.RegionLocation.Position + targetPosition;
            }

            return result;
        }

        public bool Validate(in IStateContext context)
        {
            if (context is not RotateContext) return false;
            var ownerController = context.OwnerController;
            if (ownerController == null) return false;
            var agent = ownerController.Owner;
            if (agent == null) return false;

            if (agent.Locomotor == null)
            {
                Logger.Warn("Agent without a Locomotor is using an AI profile that's trying to rotate! This is not supported. " +
                           $"Set Locomotor.Immobile to False, or assign a different profile.\nAgent: {agent}");
                return false;
            }

            return true;
        }
    }

    public struct RotateContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public bool Clockwise;
        public bool RotateTowardsTarget;
        public int Degrees;
        public float SpeedOverride;

        public RotateContext(AIController ownerController, RotateContextPrototype proto)
        {
            OwnerController = ownerController;
            Clockwise = proto.Clockwise;
            RotateTowardsTarget = proto.RotateTowardsTarget;
            Degrees = proto.Degrees;
            SpeedOverride = proto.SpeedOverride;
        }
    }

}
