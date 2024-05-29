using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Flock : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static Flock Instance { get; } = new();
        private Flock() { }

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
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

            locomotor.Stop();

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            blackboard.LastFlockPosition = Vector3.Zero;
            float moveDist = locomotor.GetCurrentSpeed() / 20f;
            blackboard.PropertyCollection[PropertyEnum.AIFlockMinimumMoveDistSq] = moveDist * moveDist;
            blackboard.PropertyCollection[PropertyEnum.AICanFlock] = true;
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not FlockContext flockContext) return failResult;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return failResult;
            Agent agent = ownerController.Owner;
            if (agent == null) return failResult;
            Game game = agent.Game;
            if (game == null) return failResult;

            BehaviorSensorySystem senses = ownerController.Senses;
            List<WorldEntity> allies = senses.GetPopulationGroup();
            bool noAllies = allies.Count <= 1;

            var result = StaticBehaviorReturnType.Running;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            ulong leaderID = blackboard.PropertyCollection[PropertyEnum.AILeaderID];

            if (noAllies == false)
            {
                if (leaderID == Entity.InvalidId)
                {
                    if (SetFlockmatesLeader(blackboard, agent.Id, allies, game) == false)
                        result = StaticBehaviorReturnType.Failed;
                    leaderID = agent.Id;
                }
                else if (leaderID != agent.Id)
                    result = DoFlockFollowerMovement(flockContext, agent, blackboard, game, allies);
            }

            if (noAllies || leaderID == agent.Id)
                result = DoFlockLeaderMovement(flockContext, blackboard, senses, agent, game, noAllies, allies);

            if (result != StaticBehaviorReturnType.Running)
            {
                Locomotor locomotor = agent.Locomotor;
                if (locomotor == null) return failResult;
                locomotor.Stop();
            }

            return result;
        }

        private StaticBehaviorReturnType DoFlockLeaderMovement(in FlockContext flockContext, BehaviorBlackboard blackboard, BehaviorSensorySystem senses, Agent agent, Game game, bool noAllies, List<WorldEntity> allies)
        {
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return StaticBehaviorReturnType.Failed;

            Vector3 agentPosition = agent.RegionLocation.Position;

            if (locomotor.HasPath)
            {
                if (locomotor.IsPathComplete())
                {
                    if (flockContext.SwitchLeaderOnCompletion && noAllies == false)
                        return SwitchLeaderToFurthestAlly(agent, blackboard, allies, game);

                    return StaticBehaviorReturnType.Completed;
                }

                if (Vector3.DistanceSquared2D(agentPosition, blackboard.LastFlockPosition) < blackboard.PropertyCollection[PropertyEnum.AIFlockMinimumMoveDistSq])
                    return StaticBehaviorReturnType.Failed;

                blackboard.LastFlockPosition = agentPosition;
                return StaticBehaviorReturnType.Running;
            }

            blackboard.LastFlockPosition = agentPosition;

            if (flockContext.ChooseRandomPointAsDestination)
            {
                return Wander(flockContext, agent, blackboard, game);
            }
            else if (locomotor.IsFollowingEntity == false)
            {
                var result = Follow(flockContext, agent, senses, game);
                if (result == StaticBehaviorReturnType.Completed && flockContext.SwitchLeaderOnCompletion && noAllies == false)
                    SwitchLeaderToFurthestAlly(agent, blackboard, allies, game);
                return result;
            }

            return StaticBehaviorReturnType.Running;
        }

        private static StaticBehaviorReturnType Follow(in FlockContext flockContext, Agent agent, BehaviorSensorySystem senses, Game game)
        {
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return StaticBehaviorReturnType.Failed;
            WorldEntity target = senses.GetCurrentTarget();
            if (target == null) return StaticBehaviorReturnType.Failed;

            GRandom random = game.Random;
            float followRange = agent.Bounds.Radius + target.Bounds.Radius 
                + flockContext.RangeMin + random.NextFloat() * (flockContext.RangeMax - flockContext.RangeMin);

            LocomotionOptions locomotionOptions = new() { RepathDelay = TimeSpan.FromMilliseconds(250) };
            locomotor.FollowEntity(target.Id, followRange, locomotionOptions, true);

            if (locomotor.IsPathComplete())
                return StaticBehaviorReturnType.Completed;

            return StaticBehaviorReturnType.Running;
        }

        private static StaticBehaviorReturnType Wander(in FlockContext flockContext, Agent agent, BehaviorBlackboard blackboard, Game game)
        {
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return StaticBehaviorReturnType.Failed;
            
            GRandom random = game.Random;
            Vector3 position = agent.RegionLocation.Position;

            Vector3 wanderFrom = blackboard.SpawnPoint;
            if (flockContext.WanderFromPointType == WanderBasePointType.CurrentPosition)
                wanderFrom = position;

            Vector3 wanderTo = wanderFrom + (Vector3.RandomUnitVector2D(random) * random.NextFloat() * flockContext.WanderRadius);
            Vector3 resultNorm = null;
            locomotor.SweepFromTo(wanderFrom, new(wanderTo), ref wanderTo, ref resultNorm); // new for safe ref

            if (locomotor.PathTo(wanderTo, new()) == false)
                return StaticBehaviorReturnType.Failed;

            return StaticBehaviorReturnType.Running;
        }

        private static StaticBehaviorReturnType SwitchLeaderToFurthestAlly(Agent agent, BehaviorBlackboard blackboard, List<WorldEntity> allies, Game game)
        {
            Vector3 curPosition = agent.RegionLocation.Position;

            float maxDist = 0f;
            Agent furthestAlly = null;

            foreach (var allyEntity in allies)
            {
                if (allyEntity is not Agent ally) continue;
                AIController allyController = ally.AIController;
                if (allyController == null) continue;
                BehaviorBlackboard allyBlackboard = allyController.Blackboard;
                if (allyBlackboard.PropertyCollection[PropertyEnum.AICanFlock] == false) continue;

                Vector3 allyPosition = ally.RegionLocation.Position;
                float distToAlly = Vector3.LengthSqr(allyPosition - curPosition);
                if (distToAlly > maxDist)
                {
                    maxDist = distToAlly;
                    furthestAlly = ally;
                }
            }

            if (furthestAlly != null)
            {
                Locomotor locomotor = furthestAlly.Locomotor;
                if (locomotor == null) return StaticBehaviorReturnType.Failed;

                locomotor.Stop();

                if (SetFlockmatesLeader(blackboard, furthestAlly.Id, allies, game) == false)
                    return StaticBehaviorReturnType.Failed;
            }

            return StaticBehaviorReturnType.Completed;
        }

        private static StaticBehaviorReturnType DoFlockFollowerMovement(in FlockContext flockContext, Agent agent, BehaviorBlackboard blackboard, Game game, List<WorldEntity> allies)
        {
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return StaticBehaviorReturnType.Failed;

            Vector3 agentPosition = agent.RegionLocation.Position;
            Vector3 agentAlignment = agent.Forward;

            if (locomotor.HasPath && locomotor.IsPathComplete() == false)
                return StaticBehaviorReturnType.Running;

            float numAllies = allies.Count;
            float agentRadius = agent.Bounds.Radius;

            Vector3 avgSeparation = Vector3.Zero;
            Vector3 avgAlignment = Vector3.Zero;
            Vector3 avgCohesion = Vector3.Zero;

            foreach (WorldEntity allyEntity in allies)
            {
                if (allyEntity is not Agent ally) continue;
                Locomotor allyLocomotor = ally.Locomotor;
                if (allyLocomotor == null) return StaticBehaviorReturnType.Failed;

                Vector3 allyPosition = ally.RegionLocation.Position;
                Vector3 distToAlly = agentPosition - allyPosition;

                avgAlignment += ally.Forward;
                avgCohesion += allyPosition;

                float lengthToAlly = Vector3.Length(distToAlly);
                if (lengthToAlly > flockContext.SeparationThreshold)
                    avgSeparation += (Vector3.Normalize(distToAlly) * agentRadius) / lengthToAlly;
            }

            if (Vector3.Dot(Vector3.Normalize(avgAlignment), agentAlignment) < flockContext.AlignmentThreshold)
            {
                avgAlignment /= numAllies;
                avgAlignment -= agentAlignment;
            }

            if (Vector3.LengthSqr(avgCohesion - agentPosition) < flockContext.CohesionThreshold)
            {
                avgCohesion = Vector3.Zero;
            }
            else
            {
                avgCohesion /= numAllies;
                avgCohesion = Vector3.Normalize(avgCohesion - agentPosition) * locomotor.GetCurrentSpeed();
                avgCohesion -= agentAlignment * locomotor.GetCurrentSpeed();
            }

            Vector3 forceToLeader = Vector3.Zero;
            WorldEntity leader = game.EntityManager.GetEntity<WorldEntity>(blackboard.PropertyCollection[PropertyEnum.AILeaderID]);
            if (leader != null)
                forceToLeader = leader.RegionLocation.Position - agentPosition;

            Vector3 flockOffset = avgAlignment * flockContext.AlignmentWeight +
                                  avgCohesion * flockContext.CohesionWeight +
                                  avgSeparation * flockContext.SeparationWeight +
                                  forceToLeader * flockContext.ForceToLeaderWeight;

            if (Vector3.LengthSqr(flockOffset) > Segment.Epsilon)
                locomotor.PathTo(agentPosition + Vector3.Truncate(flockOffset, flockContext.MaxSteeringForce), new());

            return StaticBehaviorReturnType.Running;
        }

        private static bool SetFlockmatesLeader(BehaviorBlackboard blackboard, ulong leaderId, List<WorldEntity> allies, Game game)
        {
            if (blackboard.PropertyCollection[PropertyEnum.AILeaderID] == Entity.InvalidId)
                blackboard.PropertyCollection[PropertyEnum.AILeaderID] = leaderId;

            WorldEntity leader = game.EntityManager.GetEntity<WorldEntity>(leaderId);
            if (leader == null || leader.IsDead || leader.IsDestroyed() || leader.IsControlledEntity)
            {
                blackboard.PropertyCollection[PropertyEnum.AILeaderID] = Entity.InvalidId;
                return false;
            }

            foreach (var allyEntity in allies)
            {
                if (allyEntity is not Agent ally) continue;
                AIController allyController = ally.AIController;
                if (allyController == null) continue;
                allyController.Blackboard.PropertyCollection[PropertyEnum.AILeaderID] = leaderId;
            }

            return true;
        }

        public bool Validate(in IStateContext context)
        {
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return false;
            Agent agent = ownerController.Owner;
            if (agent == null) return false;
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return false;

            if (locomotor.HasPath && locomotor.IsPathComplete() == false || agent.CanMove == false)
                return false;

            return true;
        }
    }

    public struct FlockContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public float RangeMax;
        public float RangeMin;
        public float SeparationWeight;
        public float SeparationThreshold;
        public float AlignmentWeight;
        public float AlignmentThreshold;
        public float CohesionWeight;
        public float CohesionThreshold;
        public float MaxSteeringForce;
        public float ForceToLeaderWeight;
        public bool SwitchLeaderOnCompletion;
        public bool ChooseRandomPointAsDestination;
        public WanderBasePointType WanderFromPointType;
        public float WanderRadius;

        public FlockContext(AIController ownerController, FlockContextPrototype proto)
        {
            OwnerController = ownerController;
            RangeMax = proto.RangeMax;
            RangeMin = proto.RangeMin;
            SeparationWeight = proto.SeparationWeight;
            AlignmentWeight = proto.AlignmentWeight;
            CohesionWeight = proto.CohesionWeight;
            SeparationThreshold = proto.SeparationThreshold;
            AlignmentThreshold = proto.AlignmentThreshold;
            CohesionThreshold = proto.CohesionThreshold;
            MaxSteeringForce = proto.MaxSteeringForce;
            ForceToLeaderWeight = proto.ForceToLeaderWeight;
            SwitchLeaderOnCompletion = proto.SwitchLeaderOnCompletion;
            ChooseRandomPointAsDestination = proto.ChooseRandomPointAsDestination;
            WanderFromPointType = proto.WanderFromPointType;
            WanderRadius = proto.WanderRadius;
        }
    }
}
