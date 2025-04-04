using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Navi;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Flee : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static Flee Instance { get; } = new();
        private Flee() { }

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
            if (context is not FleeContext) return;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

            blackboard.PropertyCollection[PropertyEnum.AIFleeStartTime] = (ulong)game.CurrentTime.TotalMilliseconds;
            locomotor.Stop();
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not FleeContext fleeContext) return failResult;
            var ownerController = context.OwnerController;
            if (ownerController == null) return failResult;
            var blackboard = ownerController.Blackboard;
            var agent = ownerController.Owner;
            if (agent == null) return failResult;
            var game = agent.Game;
            if (game == null) return failResult;
            var locomotor = agent.Locomotor;
            if (locomotor == null) return failResult;
            var region = agent.Region;
            if (region == null) return failResult;
            if (locomotor.IsStuck) return failResult;

            if (locomotor.HasPath)
            {
                var fleeTime = game.CurrentTime - TimeSpan.FromMilliseconds((long)blackboard.PropertyCollection[PropertyEnum.AIFleeStartTime]);
                if (fleeTime >= fleeContext.FleeTime || locomotor.IsPathComplete())
                    return StaticBehaviorReturnType.Completed;
            }
            else
            {
                var agentPosition = agent.RegionLocation.Position;
                var senses = ownerController.Senses;
                var random = game.Random;

                if (fleeContext.FleeTowardAllies && random.NextFloat() <= fleeContext.FleeTowardAlliesPercentChance)
                {
                    if (FleeTowardAllies(agent, fleeContext))
                        return StaticBehaviorReturnType.Running;
                }

                var potentialEnemies = senses.PotentialHostileTargetIds;                
                var averageDirection = Vector3.Zero;

                var entityManager = game.EntityManager;

                foreach (var enemyId in potentialEnemies)
                {
                    var enemy = entityManager.GetEntity<WorldEntity>(enemyId);
                    if (enemy != null && enemy.IsInWorld)
                    {
                        var distEnemy = agentPosition - enemy.RegionLocation.Position;
                        var magnitude = Vector3.Length2D(distEnemy);
                        if (magnitude > 0.0f)
                            distEnemy /= magnitude * magnitude;
                        distEnemy.Z = 0f;
                        averageDirection += distEnemy;
                    }
                }

                var distance = locomotor.GetCurrentSpeed() * random.NextFloat((float)fleeContext.FleeTime.TotalSeconds - fleeContext.FleeTimeVariance, (float)fleeContext.FleeTime.TotalSeconds + fleeContext.FleeTimeVariance);
                var targetPosition = agentPosition + Vector3.SafeNormalize2D(averageDirection) * distance;

                if (fleeContext.FleeHalfAngle != 0.0f)
                {
                    if (FleeTowardsAngle(agent, fleeContext, averageDirection, targetPosition, distance))
                        return StaticBehaviorReturnType.Running;
                    return StaticBehaviorReturnType.Failed;
                }
                else
                {
                    var locomotionOptions = new LocomotionOptions
                    { PathGenerationFlags = PathGenerationFlags.IncompletedPath };

                    if (locomotor.PathTo(targetPosition, locomotionOptions) == false)
                        return StaticBehaviorReturnType.Failed;
                }
            }

            return StaticBehaviorReturnType.Running;
        }

        private static bool FleeTowardsAngle(Agent owner, in FleeContext fleeContext, Vector3 direction, Vector3 targetPosition, float distance)
        {
            var locomotor = owner.Locomotor;
            if (locomotor == null) return false;
            var ownerController = fleeContext.OwnerController;
            if (ownerController == null) return false;
            var game = owner.Game;
            if (game == null) return false;
            var position = owner.RegionLocation.Position;

            List<FleePath> pathResults = ListPool<FleePath>.Instance.Get();

            try
            {
                Vector3? resultNorm = null;
                Vector3 resultPosition = Vector3.Zero;
                var sweepResult = locomotor.SweepFromTo(position, targetPosition, ref resultPosition, ref resultNorm); // debug for sweep On here
                var fleeDistance = Vector3.Distance2D(position, resultPosition);

                if (sweepResult != SweepResult.Failed && fleeDistance > fleeContext.FleeDistanceMin)
                    pathResults.Add(new FleePath(targetPosition, fleeDistance));

                GenerateValidFleeAnglePaths(pathResults, owner, direction, distance, fleeContext);
                if (pathResults.Count == 0)
                    GenerateValidFleeAnglePaths(pathResults, owner, -direction, distance, fleeContext);
                if (pathResults.Count == 0)
                    return false;

                pathResults.Sort();
                var locomotionOptions = new LocomotionOptions
                { PathGenerationFlags = PathGenerationFlags.IncompletedPath };

                foreach (var pathResult in pathResults)
                    if (locomotor.PathTo(pathResult.Position, locomotionOptions))
                        return true;
            }
            finally
            {
                ListPool<FleePath>.Instance.Return(pathResults);
            }

            return false;
        }

        private static void GenerateValidFleeAnglePaths(List<FleePath> pathResults, Agent owner, Vector3 direction, float distance, in FleeContext fleeContext)
        {
            var region = owner.Region;
            if (region == null) return;
            var locomotor = owner.Locomotor;
            if (locomotor == null) return;
            var position = owner.RegionLocation.Position;

            for (int i = 0; i < 4; i++)
            {
                float angle = (i + 1) * (fleeContext.FleeHalfAngle / 4);
                
                var dirSideA = Vector3.SafeNormalize2D(Vector3.AxisAngleRotate(direction, Vector3.ZAxis, MathHelper.ToRadians(angle)));
                var dirSideB = Vector3.SafeNormalize2D(Vector3.AxisAngleRotate(direction, Vector3.ZAxis, MathHelper.ToRadians(-angle)));

                Vector3 sideAPos = Vector3.Zero;
                Vector3 sideBPos = Vector3.Zero;
                Vector3? resultNorm = null;
                var sweepSideA = locomotor.SweepFromTo(position, position + dirSideA * distance, ref sideAPos, ref resultNorm);
                var sweepSideB = locomotor.SweepFromTo(position, position + dirSideB * distance, ref sideBPos, ref resultNorm);

                var distanceSideA = Vector3.Distance2D(position, sideAPos);
                var distanceSideB = Vector3.Distance2D(position, sideBPos);

                if (sweepSideA != SweepResult.Failed && distanceSideA > fleeContext.FleeDistanceMin)
                    pathResults.Add(new FleePath(sideAPos, distanceSideA));

                if (sweepSideB != SweepResult.Failed && distanceSideB > fleeContext.FleeDistanceMin)
                    pathResults.Add(new FleePath(sideBPos, distanceSideB));
            }
        }

        private static bool FleeTowardAllies(Agent owner, in FleeContext fleeContext)
        {
            var region = owner.Region;
            if (region == null) return false;
            var locomotor = owner.Locomotor;
            if (locomotor == null) return false;
            var ownerController = fleeContext.OwnerController;
            if (ownerController == null) return false;
            var game = owner.Game;
            if (game == null) return false;

            var senses = ownerController.Senses;
            var potentialAllies = senses.PotentialAllyTargetIds;
            var curPosition = owner.RegionLocation.Position;

            if (potentialAllies.Count > 0)
            {
                Sphere volume = new Sphere(curPosition, ownerController.AggroRangeAlly);
                foreach (var entity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.ActivePartition)))
                    if (entity is WorldEntity worldEntity && worldEntity.Id != owner.Id && owner.IsFriendlyTo(worldEntity))
                        potentialAllies.Add(worldEntity.Id);
            }
            
            var allies = ListPool<FleeEntityRank>.Instance.Get();

            try
            {
                var entityManager = game.EntityManager;
                foreach (var allyId in potentialAllies)
                {
                    var ally = entityManager.GetEntity<WorldEntity>(allyId);
                    if (ally != null && ally.IsInWorld && !ally.IsDead)
                    {
                        var rankProto = ally.GetRankPrototype();
                        if (rankProto != null)
                            allies.Add(new FleeEntityRank(ally, rankProto.Rank));
                    }
                }

                allies.Sort();

                var locomotionOptions = new LocomotionOptions
                { PathGenerationFlags = PathGenerationFlags.IncompletedPath };

                Vector3? resultNorm = null;
                Vector3 resultPosition = Vector3.Zero;

                foreach (var ally in allies)
                {
                    var entity = ally.Entity;
                    if (entity == null) continue;

                    if (locomotor.SweepFromTo(curPosition, entity.RegionLocation.Position, ref resultPosition, ref resultNorm) != SweepResult.Failed
                        && Vector3.Distance2D(curPosition, resultPosition) > fleeContext.FleeDistanceMin
                        && locomotor.PathTo(resultPosition, locomotionOptions))
                        return true;
                }
            }
            finally
            {
                ListPool<FleeEntityRank>.Instance.Return(allies);
            }

            return false;
        }

        public bool Validate(in IStateContext context)
        {
            if (context is not FleeContext) return false;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return false;
            Agent agent = ownerController.Owner;
            if (agent == null) return false;
            BehaviorSensorySystem senses = ownerController.Senses;
            if (senses.PotentialHostileTargetIds.Count == 0) return false;
            if (agent.CanMove() == false) return false;

            return true;
        }
    }

    public struct FleePath : IComparable<FleePath>
    {
        public Vector3 Position;
        public float Distance;

        public FleePath(Vector3 position, float distance)
        {
            Position = position;
            Distance = distance;
        }

        public int CompareTo(FleePath other)
        {
            return Distance.CompareTo(other.Distance);
        }
    }

    public struct FleeEntityRank : IComparable<FleeEntityRank>
    {
        public WorldEntity Entity;
        public Rank Rank;

        public FleeEntityRank(WorldEntity entity, Rank rank)
        {
            Entity = entity;
            Rank = rank;
        }

        public int CompareTo(FleeEntityRank other)
        {
            return Rank.CompareTo(other.Rank);
        }
    }

    public struct FleeContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public TimeSpan FleeTime;
        public float FleeTimeVariance;
        public float FleeHalfAngle;
        public float FleeDistanceMin;
        public bool FleeTowardAllies;
        public float FleeTowardAlliesPercentChance;

        public FleeContext(AIController ownerController, FleeContextPrototype proto)
        {
            OwnerController = ownerController;
            FleeTime = TimeSpan.FromSeconds(proto.FleeTime);
            FleeTimeVariance = proto.FleeTimeVariance;
            FleeHalfAngle = proto.FleeHalfAngle;
            FleeDistanceMin = proto.FleeDistanceMin;
            FleeTowardAllies = proto.FleeTowardAllies;
            FleeTowardAlliesPercentChance = proto.FleeTowardAlliesPercentChance;
        }
    }

}
