using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Navi;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Orbit : IAIState
    {
        public static Orbit Instance { get; } = new();
        private Orbit() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state) { }

        public void Start(in IStateContext context)
        {
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

            locomotor.Stop();
        }

        private enum State
        {
            MoveToClosestPoint = 0,
            Orbit = 1,
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;

            if (context is not OrbitContext orbitContext) return failResult;
            var ownerController = context.OwnerController;
            if (ownerController == null) return failResult;
            var agent = ownerController.Owner;
            if (agent == null) return failResult;

            var blackboard = ownerController.Blackboard;
            var agentsLocomotor = agent.Locomotor;
            if (agentsLocomotor == null) return failResult;

            if (agentsLocomotor.HasPath)
            {
                bool isStuck = agentsLocomotor.IsStuck;
                if (agentsLocomotor.IsPathComplete() || isStuck)
                {
                    State lastOrbitState = (State)(int)blackboard.PropertyCollection[PropertyEnum.AILastOrbitContextState];
                    if (lastOrbitState == State.MoveToClosestPoint)
                    {
                        blackboard.PropertyCollection[PropertyEnum.AILastOrbitContextState] = (int)State.Orbit;
                        if (ToOrbit(orbitContext, agent) == false) return failResult;
                    }
                    else if (lastOrbitState == State.Orbit)
                    {
                        if (isStuck)
                            return failResult;
                        else
                            return StaticBehaviorReturnType.Completed;
                    }
                }
            }
            else
            {
                if (MoveToClosestPoint(agent) == false) return failResult;
                blackboard.PropertyCollection[PropertyEnum.AILastOrbitContextState] = (int)State.MoveToClosestPoint;
            }

            return StaticBehaviorReturnType.Running;
        }

        private enum PreviousOrbitDirection
        {
            None,
            Clockwise,
            CounterClockwise,
        }

        private static bool ToOrbit(in OrbitContext orbitContext, Agent agent)
        {
            if (agent == null) return false;
            var agentsController = agent.AIController;
            if (agentsController == null) return false;
            var blackboard = agentsController.Blackboard;
            var agentsLocomotor = agent.Locomotor;
            if (agentsLocomotor == null) return false;
            var game = agent.Game;
            if (game == null) return false;
            var targetEntity = agentsController.TargetEntity;
            if (targetEntity == null || targetEntity.IsInWorld == false) return false;

            var targetsPosition = targetEntity.RegionLocation.Position;
            var position = agent.RegionLocation.Position;
            var distance = position - targetsPosition;
            var length = Vector3.Length(distance);
            if (Segment.IsNearZero(length)) return false;

            distance /= length;            
            var transform = Transform3.BuildTransform(Vector3.Zero, new Orientation(MathHelper.ToRadians(orbitContext.ThetaInDegrees), 0f, 0f));
            var direction = transform * distance;
            transform = Transform3.BuildTransform(Vector3.Zero, new Orientation(MathHelper.ToRadians(-orbitContext.ThetaInDegrees), 0f, 0f));
            var directionInv = transform * distance;

            Vector3? normOut = null;
            bool sideA = true;
            Vector3 sideAPos = Vector3.Zero;
            if (agentsLocomotor.SweepFromTo(position, targetsPosition + direction * length, ref sideAPos, ref normOut) == SweepResult.Failed)
                sideA = false;

            bool sideB = true;
            Vector3 sideBPos = Vector3.Zero;
            if (agentsLocomotor.SweepFromTo(position, targetsPosition + directionInv * length, ref sideBPos, ref normOut) == SweepResult.Failed)
                sideB = false;

            float lengthSideA = Vector3.DistanceSquared2D(position, sideAPos);
            float lengthSideB = Vector3.DistanceSquared2D(position, sideBPos);

            var previousOrbitDirection = (PreviousOrbitDirection)(int)blackboard.PropertyCollection[PropertyEnum.AIPreviousOrbitDirection];

            Vector3 sidePosition;
            if (sideA && (lengthSideA > lengthSideB || previousOrbitDirection == PreviousOrbitDirection.Clockwise))
            {
                sidePosition = sideAPos;
                blackboard.PropertyCollection[PropertyEnum.AIPreviousOrbitDirection] = (int)PreviousOrbitDirection.Clockwise;
            }
            else if (sideB && (lengthSideB > lengthSideA || previousOrbitDirection == PreviousOrbitDirection.CounterClockwise))
            {
                sidePosition = sideBPos;
                blackboard.PropertyCollection[PropertyEnum.AIPreviousOrbitDirection] = (int)PreviousOrbitDirection.CounterClockwise;
            }
            else
            {
                if (previousOrbitDirection == PreviousOrbitDirection.Clockwise || game.Random.Next() % 2 == 0)
                {
                    if (sideA)
                    {
                        sidePosition = sideAPos;
                        blackboard.PropertyCollection[PropertyEnum.AIPreviousOrbitDirection] = (int)PreviousOrbitDirection.Clockwise;
                    }
                    else if (sideB)
                    {
                        sidePosition = sideBPos;
                        blackboard.PropertyCollection[PropertyEnum.AIPreviousOrbitDirection] = (int)PreviousOrbitDirection.CounterClockwise;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (sideB)
                    {
                        sidePosition = sideBPos;
                        blackboard.PropertyCollection[PropertyEnum.AIPreviousOrbitDirection] = (int)PreviousOrbitDirection.CounterClockwise;
                    }
                    else if (sideA)
                    {
                        sidePosition = sideAPos;
                        blackboard.PropertyCollection[PropertyEnum.AIPreviousOrbitDirection] = (int)PreviousOrbitDirection.Clockwise;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            var locomotionOptions = new LocomotionOptions
            {
                RepathDelay = TimeSpan.FromSeconds(1.0f),
                PathGenerationFlags = PathGenerationFlags.IncompletedPath
            };
            if (agentsLocomotor.PathTo(sidePosition, locomotionOptions) == false) return false;
            agent.DrawPath((PrototypeId)925659119519994384);
            return true;
        }

        private static bool MoveToClosestPoint(Agent agent)
        {
            if (agent == null) return false;
            var agentsController = agent.AIController;
            if (agentsController == null) return false;
            var agentsLocomotor = agent.Locomotor;
            if (agentsLocomotor == null) return false;
            var targetEntity = agentsController.TargetEntity;
            if (targetEntity == null || targetEntity.IsInWorld == false) return false;

            var locomotionOptions = new LocomotionOptions
            {
                RepathDelay = TimeSpan.FromSeconds(1.0f),
                PathGenerationFlags = PathGenerationFlags.IncompletedPath | PathGenerationFlags.IgnoreSweep
            };
            if (agentsLocomotor.PathTo(targetEntity.RegionLocation.Position, locomotionOptions) == false) return false;
            agent.DrawPath((PrototypeId)925659119519994384);
            return true;
        }

        public bool Validate(in IStateContext context)
        {
            var ownerController = context.OwnerController;
            if (ownerController == null) return false;
            var target = ownerController.TargetEntity;
            if (target == null || target.IsInWorld == false) return false;

            var agent = ownerController.Owner;
            if (agent == null) return false;
            if (agent.CanMove() == false) return false;

            return true;
        }
    }

    public struct OrbitContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public float ThetaInDegrees;

        public OrbitContext(AIController ownerController, OrbitContextPrototype proto)
        {
            OwnerController = ownerController;
            ThetaInDegrees = proto.ThetaInDegrees;
        }
    }

}
