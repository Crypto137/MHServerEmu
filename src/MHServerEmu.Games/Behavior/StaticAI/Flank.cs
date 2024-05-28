using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Navi;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Flank : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static Flank Instance { get; } = new();
        private Flank() { }

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

        public void Start(IStateContext context)
        {
            if (context == null) return;
            if (context is not FlankContext flankContext) return;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null) return;

            locomotor.Stop();

            blackboard.PropertyCollection[PropertyEnum.AIFlankNextThinkTime] = 0L;
            blackboard.LastFlankTargetEntityPos = Vector3.Zero;

            if (flankContext.TimeoutMS > 0)
                blackboard.PropertyCollection[PropertyEnum.AIFlankTimeout] = (long)game.GetCurrentTime().TotalMilliseconds + flankContext.TimeoutMS;
            else
                blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFlankTimeout);
        }

        public StaticBehaviorReturnType Update(IStateContext context)
        {
            var returnType = StaticBehaviorReturnType.Failed;
            if (context == null) return returnType;
            if (context is not FlankContext flankContext) return returnType;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return returnType;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            Agent agent = ownerController.Owner;
            if (agent == null) return returnType;
            Game game = agent.Game;
            if (game == null) return returnType;

            TimeSpan currentTime = game.GetCurrentTime();

            if (blackboard.PropertyCollection.HasProperty(PropertyEnum.AIFlankTimeout)
                && currentTime >= TimeSpan.FromMilliseconds(blackboard.PropertyCollection[PropertyEnum.AIFlankTimeout]))
                return flankContext.FailOnTimeout ? StaticBehaviorReturnType.Failed : StaticBehaviorReturnType.Completed;

            WorldEntity flankTarget = GetFlankTarget(ownerController, flankContext.FlankTo);
            if (flankTarget == null || flankTarget.IsInWorld == false) return returnType;

            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null || locomotor.IsStuck) return returnType;

            if (locomotor.HasPath && locomotor.IsPathComplete())
                return StaticBehaviorReturnType.Completed;

            if (currentTime >= TimeSpan.FromMilliseconds(blackboard.PropertyCollection[PropertyEnum.AIFlankNextThinkTime]))
            {
                blackboard.PropertyCollection[PropertyEnum.AIFlankNextThinkTime] = (long)currentTime.TotalMilliseconds + 250;
                Vector3 targetEntityPos = flankTarget.RegionLocation.Position;
                bool genPath = false;
                bool usePerp = false;

                if (locomotor.HasPath)
                {
                    if (Vector3.DistanceSquared(blackboard.LastFlankTargetEntityPos, targetEntityPos) > 2500.0f)
                    {
                        genPath = true;
                        usePerp = true;
                    }
                }
                else
                    genPath = true;

                if (genPath)
                {
                    Vector3 agentPos = agent.RegionLocation.Position;
                    float agentRadius = agent.Bounds.Radius;
                    float targetRadius = flankTarget.Bounds.Radius;
                    float followRange = agentRadius + targetRadius;

                    FlankOffsetResult offsetResult = GenerateInitialFlankTargetOffset(flankContext, out var targetOffset, locomotor, agentPos, agentRadius, 
                        targetEntityPos, targetRadius, usePerp);

                    if (Vector3.DistanceSquared(targetEntityPos, targetEntityPos + targetOffset) < flankContext.RangeMin * flankContext.RangeMin)
                        return StaticBehaviorReturnType.Failed;

                    if (offsetResult == FlankOffsetResult.NoPath)
                    {
                        locomotor.FollowEntity(flankTarget.Id, followRange);
                    }
                    else if (offsetResult != FlankOffsetResult.Failed)
                    {
                        List<Waypoint> waypoints = new ();

                        if (flankContext.WaypointRadius > 0f)
                        {
                            Logger.Warn($"The AI tree for the following entity has a Flank action with StopAtFlankingWaypoint=True, but a non-zero WaypointRadius, which won't work (using radius 0 instead)\n[{agent}]");
                            if (offsetResult == FlankOffsetResult.Right)
                                waypoints.Add(new Waypoint(targetEntityPos + targetOffset, NaviSide.Right, flankContext.WaypointRadius));
                            else if (offsetResult == FlankOffsetResult.Left)
                                waypoints.Add(new Waypoint(targetEntityPos + targetOffset, NaviSide.Left, flankContext.WaypointRadius));
                        }
                        else
                            waypoints.Add(new Waypoint(targetEntityPos + targetOffset, NaviSide.Point, 0f));

                        if (flankContext.StopAtFlankingWaypoint == false)
                            waypoints.Add(new Waypoint(targetEntityPos + Vector3.SafeNormalize(targetOffset) * (agentRadius + targetRadius), NaviSide.Point, 0f));

                        if (waypoints[^1].Side != NaviSide.Point || waypoints[^1].Radius != 0f)
                        {
                            Logger.Warn($"The AI tree for the following entity has a Flank action that generated a final waypoint that is not a point, which won't work (using radius 0 instead)\n[{agent}]");
                            waypoints[^1] = new Waypoint(waypoints[^1].Point, NaviSide.Point, 0f);
                        }

                        if (locomotor.PathToWaypoints(waypoints) == false)
                            locomotor.FollowEntity(flankTarget.Id, followRange);
                    }
                    else
                        return StaticBehaviorReturnType.Failed;

                    blackboard.LastFlankTargetEntityPos = targetEntityPos;
                }
            }

            return StaticBehaviorReturnType.Running;
        }

        private static FlankOffsetResult GenerateInitialFlankTargetOffset(FlankContext flankContext, out Vector3 targetOffset, Locomotor locomotor, Vector3 position, float radius,
            Vector3 targetPosition, float targetRadius, bool usePerp)
        {   
            targetOffset = Vector3.Zero;   

            var failResult = FlankOffsetResult.Failed;
            AIController ownerController = flankContext.OwnerController;
            if (ownerController == null) return failResult;
            Agent agent = ownerController.Owner;
            if (agent == null) return failResult;
            Game game = agent.Game;
            if (game == null) return failResult;         
            GRandom random = game.Random;

            float offsetMagnitude = flankContext.RangeMin + radius + targetRadius;
            if (flankContext.RangeMin < flankContext.RangeMax)
                offsetMagnitude += random.NextFloat() * (flankContext.RangeMax - flankContext.RangeMin);

            Vector3 targetDist = targetPosition - position;
            if (Vector3.IsNearZero2D(targetDist))
                targetDist = Vector3.RandomUnitVector2D(random);

            targetDist = Vector3.SafeNormalize2D(targetDist);
            float flankingAngle = flankContext.ToTargetFlankingAngle;

            if (flankContext.RandomizeFlankingAngle)
                flankingAngle = random.Next(0, 360);
            
            Transform3 transform = Transform3.BuildTransform(Vector3.Zero, new Orientation(MathHelper.ToRadians(flankingAngle), 0f, 0f));
            Vector3 direction = transform * targetDist;
            transform = Transform3.BuildTransform(Vector3.Zero, new Orientation(MathHelper.ToRadians(-flankingAngle), 0f, 0f));
            Vector3 directionInv = transform * targetDist;

            Vector3 normOut = null;
            bool sideA = true;
            Vector3 sideAPos = Vector3.Zero;
            if (locomotor.SweepFromTo(targetPosition, targetPosition + direction * offsetMagnitude, ref sideAPos, ref normOut) == SweepResult.Failed)
                sideA = false;

            bool sideB = true;
            Vector3 sideBPos = Vector3.Zero;
            if (locomotor.SweepFromTo(targetPosition, targetPosition + directionInv * offsetMagnitude, ref sideBPos, ref normOut) == SweepResult.Failed)
                sideB = false;

            float lengthSideA = Vector3.DistanceSquared2D(targetPosition, sideAPos);
            float lengthSideB = Vector3.DistanceSquared2D(targetPosition, sideBPos);

            if (usePerp)
            {
                if (Vector3.Dot(Vector3.Perp2D(targetDist), agent.Forward) < 0)
                {
                    if (sideA && lengthSideA > 0f)
                    {
                        targetOffset = direction * Vector3.Distance2D(targetPosition, sideAPos);
                        return FlankOffsetResult.Right;
                    }
                    else if (sideB && lengthSideB > 0f)
                    {
                        targetOffset = directionInv * Vector3.Distance2D(targetPosition, sideBPos);
                        return FlankOffsetResult.Left;
                    }
                }
                else
                {
                    if (sideB && lengthSideB > 0f)
                    {
                        targetOffset = directionInv * Vector3.Distance2D(targetPosition, sideBPos);
                        return FlankOffsetResult.Left;
                    }
                    else if (sideA && lengthSideA > 0f)
                    {
                        targetOffset = direction * Vector3.Distance2D(targetPosition, sideAPos);
                        return FlankOffsetResult.Right;
                    }
                }
            }
            else
            {
                if (sideA && lengthSideA > lengthSideB)
                {
                    targetOffset = direction * Vector3.Distance2D(targetPosition, sideAPos);
                    return FlankOffsetResult.Right;
                }
                else if (sideB && lengthSideB > lengthSideA)
                {
                    targetOffset = directionInv * Vector3.Distance2D(targetPosition, sideBPos);
                    return FlankOffsetResult.Left;
                }
                else
                {
                    if (random.NextFloat() < 0.5f)
                    {
                        if (sideA)
                        {
                            targetOffset = direction * Vector3.Distance2D(targetPosition, sideAPos);
                            return FlankOffsetResult.Right;
                        }
                        else if (sideB)
                        {
                            targetOffset = directionInv * Vector3.Distance2D(targetPosition, sideBPos);
                            return FlankOffsetResult.Left;
                        }
                    }
                    else
                    {
                        if (sideB)
                        {
                            targetOffset = directionInv * Vector3.Distance2D(targetPosition, sideBPos);
                            return FlankOffsetResult.Left;
                        }
                        else if (sideA)
                        {
                            targetOffset = direction * Vector3.Distance2D(targetPosition, sideAPos);
                            return FlankOffsetResult.Right;
                        }
                    }
                }
            }

            return FlankOffsetResult.NoPath;
        }

        private static WorldEntity GetFlankTarget(AIController ownerController, FlankToType flankType)
        {
            return flankType switch
            {
                FlankToType.Target => ownerController.TargetEntity,
                FlankToType.AssistedEntity => ownerController.AssistedEntity,
                FlankToType.InteractEntity => ownerController.InteractEntity,
                _ => null,
            };
        }

        public bool Validate(IStateContext context)
        {
            if (context == null) return false;
            if (context is not FlankContext flankContext) return false;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return false;
            Agent agent = ownerController.Owner;
            if (agent == null) return false;

            WorldEntity flankTarget = GetFlankTarget(ownerController, flankContext.FlankTo);
            if (flankTarget == null || flankTarget.IsInWorld == false || agent.CanMove == false) return false;

            return true;
        }
    }

    public enum FlankOffsetResult
    {
        Failed,
        NoPath,
        Left,
        Right,
    }

    public struct FlankContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public float RangeMin;
        public float RangeMax;
        public float WaypointRadius;
        public float ToTargetFlankingAngle;
        public bool StopAtFlankingWaypoint;
        public int TimeoutMS;
        public bool FailOnTimeout;
        public bool RandomizeFlankingAngle;
        public FlankToType FlankTo;

        public FlankContext(AIController ownerController, FlankContextPrototype proto)
        {
            OwnerController = ownerController;
            RangeMin = proto.RangeMin;
            RangeMax = proto.RangeMax;
            WaypointRadius = proto.WaypointRadius;
            ToTargetFlankingAngle = proto.ToTargetFlankingAngle;
            StopAtFlankingWaypoint = proto.StopAtFlankingWaypoint;
            TimeoutMS = proto.TimeoutMS;
            FailOnTimeout = proto.FailOnTimeout;
            RandomizeFlankingAngle = proto.RandomizeFlankingAngle;
            FlankTo = proto.FlankTo;
        }
    }
}
