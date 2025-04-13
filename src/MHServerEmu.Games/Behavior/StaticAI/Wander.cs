using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Wander : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static Wander Instance { get; } = new();
        private Wander() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            Agent agent = ownerController.Owner;
            if (agent == null)
            {
                Logger.Warn("Unable to get Owner from AIController.");
                return;
            }

            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null)
            {
                Logger.Warn($"Agent {agent} has no locomotor to stop!");
                return;
            }

            locomotor.Stop();
        }

        public void Start(in IStateContext context)
        {
            if (context is not WanderContext wanderContext) return;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

            locomotor.Stop();
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not WanderContext wanderContext) return failResult;

            AIController ownerController = context.OwnerController;
            if (ownerController == null) return failResult;
            Agent agent = ownerController.Owner;
            if (agent == null) return failResult;
            Game game = agent.Game;
            if (game == null) return failResult;

            Vector3 currentPosition = agent.RegionLocation.Position;

            BehaviorBlackboard blackboard = ownerController.Blackboard;

            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null || locomotor.IsStuck) return failResult;

            if (locomotor.HasPath)
            {
                if (locomotor.IsPathComplete()) return StaticBehaviorReturnType.Completed;
            }
            else
            {
                if (game == null) return failResult;

                GRandom random = game.Random;
                float wanderRange = wanderContext.RangeMin + (random.NextFloat() * (wanderContext.RangeMax - wanderContext.RangeMin));

                Vector3 wanderFrom;
                switch (wanderContext.FromPoint)
                {
                    case WanderBasePointType.SpawnPoint:
                        wanderFrom = blackboard.SpawnPoint;
                        break;

                    case WanderBasePointType.CurrentPosition:
                        wanderFrom = currentPosition;
                        break;

                    case WanderBasePointType.TargetPosition:
                        WorldEntity currentTarget = ownerController.TargetEntity;
                        if (currentTarget == null) return failResult;

                        wanderRange += agent.Bounds.Radius + currentTarget.Bounds.Radius;
                        wanderFrom = currentTarget.RegionLocation.Position;
                        break;

                    default:
                        // Logger.Warn($"The following agent is trying to execute an ActionMoveTo with no valid BasePointType!\n[{agent}]");
                        return failResult;
                }

                Vector3 wanderTo = wanderFrom + (Vector3.RandomUnitVector2D(random) * wanderRange);
                Vector3? resultNorm = null;
                locomotor.SweepFromTo(wanderFrom, wanderTo, ref wanderTo, ref resultNorm);

                bool isWalking = ownerController.GetDesiredIsWalkingState(wanderContext.MovementSpeed);
                LocomotionOptions locomotionOptions = new() { PathGenerationFlags = Navi.PathGenerationFlags.IncompletedPath };

                if (isWalking)
                    locomotionOptions.Flags |= LocomotionFlags.IsWalking;

                if (locomotor.PathTo(wanderTo, locomotionOptions) == false) return failResult;
                agent.DrawPath(EntityHelper.TestOrb.Blue); 
            }

            return StaticBehaviorReturnType.Running;
        }

        public bool Validate(in IStateContext context)
        {
            if (context is not WanderContext) return false;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return false;
            Agent agent = ownerController.Owner;
            if (agent == null) return false;

            if (agent.CanMove() == false) return false;

            return true;
        }
    }

    public struct WanderContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public WanderBasePointType FromPoint;
        public MovementSpeedOverride MovementSpeed;
        public float RangeMax;
        public float RangeMin;

        public WanderContext(AIController ownerController, WanderContextPrototype proto)
        {
            OwnerController = ownerController;
            FromPoint = proto.FromPoint;
            RangeMax = proto.RangeMax;
            RangeMin = proto.RangeMin;
            MovementSpeed = proto.MovementSpeed;
        }
    }
}
