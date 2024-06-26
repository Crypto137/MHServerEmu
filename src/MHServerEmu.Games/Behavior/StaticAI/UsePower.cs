using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Powers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.Generators;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class UsePower : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static UsePower Instance { get; } = new();
        private UsePower() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            Agent agent = ownerController.Owner;
            if (agent != null) return;

            if (state == StaticBehaviorReturnType.Interrupted && agent.IsExecutingPower)
            {
                Power activatePower = agent.ActivePower;
                if (activatePower != null) return;

                if (activatePower.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Interrupting) == false)
                    Logger.Warn($"{agent}: is trying to end {activatePower} but something went wrong");
            }

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            blackboard.PropertyCollection.RemovePropertyRange(PropertyEnum.AIPowerStarted);
        }

        public void Start(in IStateContext context)
        {
            if (context is not UsePowerContext powerContext) return;
            AIController aiController = context.OwnerController;
            if (aiController == null) return;
            Agent agent = aiController.Owner;
            if (agent == null) return;

            PowerPrototype powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerContext.Power);
            if (powerProto == null) return;

            if (powerProto.PowerCategory == PowerCategoryType.ComboEffect)
            {
                Logger.Warn($"A procedural AI is attempting to directly execute a combo power.\nAgent: {agent}\nPower: {GameDatabase.GetPrototypeName(powerContext.Power)}");
                return;
            }

            if (powerProto.PowerCategory == PowerCategoryType.ProcEffect)
            {
                Logger.Warn($"A procedural AI is attempting to directly execute a proc effect power.\nAgent: {agent}\nPower: {GameDatabase.GetPrototypeName(powerContext.Power)}");
                return;
            }

            if (Segment.IsNearZero(powerContext.TargetAngleOffset) == false && Power.GetTargetingShape(powerProto) == TargetingShapeType.Self)
            {
                Vector3 forward = agent.Forward;
                Matrix3 matRotation = Matrix3.RotationZ(MathHelper.ToRadians(powerContext.TargetAngleOffset));
                Vector3 toTarget = matRotation * forward;
                Orientation newOrientation = Orientation.FromDeltaVector(toTarget);
                agent.ChangeRegionPosition(null, newOrientation);
            }

            BehaviorBlackboard blackboard = aiController.Blackboard;

            if (powerContext.ChooseRandomTargetPosition)
            {
                if (blackboard.UsePowerTargetPos == Vector3.Zero)
                    Logger.Warn($"StaticAI.UsePower Start() with random target position of (0,0,0). Power=[{GameDatabase.GetPrototypeName(powerContext.Power)}] TargetId=[{blackboard.PropertyCollection[PropertyEnum.AIUsePowerTargetID]}] OwnerAgent=[{agent}]");
            }

            bool isPowerActivated = aiController.AttemptActivatePower(powerContext.Power, blackboard.PropertyCollection[PropertyEnum.AIUsePowerTargetID], blackboard.UsePowerTargetPos);

            blackboard.PropertyCollection[PropertyEnum.AIPowerStarted, powerContext.Power] = isPowerActivated;
            blackboard.PropertyCollection[PropertyEnum.AILastPowerActivated] = powerContext.Power;
            blackboard.UsePowerTargetPos = Vector3.Zero;
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not UsePowerContext) return failResult;
            AIController controller = context.OwnerController;
            if (controller == null) return failResult;
            Agent agent = controller.Owner;
            if (agent == null) return failResult;

            if (agent.IsExecutingPower == false)
            {
                BehaviorBlackboard blackboard = controller.Blackboard;
                if (blackboard.PropertyCollection.HasProperty(PropertyEnum.AIPowerStarted))
                    return StaticBehaviorReturnType.Completed;
                else
                    return StaticBehaviorReturnType.Failed;
            }

            return StaticBehaviorReturnType.Running;
        }

        public bool Validate(in IStateContext context)
        {
            return ValidateInternal(context) == PowerUseResult.Success;
        }

        private static PowerUseResult ValidateInternal(in IStateContext context)
        {
            var genericErrorResult = PowerUseResult.GenericError;
            if (context is not UsePowerContext powerContext) return genericErrorResult;
            PrototypeId powerRef = powerContext.Power;
            AIController controller = context.OwnerController;
            if (controller == null) return genericErrorResult;
            Agent agent = controller.Owner;
            if (agent == null) return genericErrorResult;
            Game game = agent.Game;
            if (game == null) return genericErrorResult;
            Region region = agent.Region;
            if (region == null) return genericErrorResult;

            if (agent.IsExecutingPower) return PowerUseResult.PowerInProgress;

            Power power = agent.GetPower(powerRef);
            if (power == null)
            {
                Logger.Warn($"Agent is trying to use a power that is missing from its power collection.\nAgent: [{agent}]\nPower: [{GameDatabase.GetPrototypeName(powerRef)}]");
                return genericErrorResult;
            }

            BehaviorSensorySystem senses = controller.Senses;
            BehaviorBlackboard blackboard = controller.Blackboard;
            WorldEntity targetWorldEntity = senses.GetCurrentTarget();
            ulong targetIdForPower;

            RegionLocation regionLocation = agent.RegionLocation;
            RegionLocation targetRegionLocation;
            TargetingShapeType targetingShape = power.GetTargetingShape();

            if (targetingShape == TargetingShapeType.TeamUp)
            {
                Logger.Warn($"Agent is trying to use a power that has TeamUp as its TargetingShape (only allowed for Avatars).\nAgent: [{agent}]\nPower: [{GameDatabase.GetPrototypeName(powerRef)}]");
                return genericErrorResult;
            }

            if (powerContext.ForceInvalidTargetActivation)
            {
                targetIdForPower = Entity.InvalidId;
                targetRegionLocation = regionLocation;
            }
            else if (targetingShape == TargetingShapeType.Self)
            {
                targetIdForPower = agent.Id;
                targetRegionLocation = regionLocation;
            }
            else
            {
                if (targetWorldEntity == null || targetWorldEntity.IsInWorld == false)
                    return PowerUseResult.TargetIsMissing;

                if (targetWorldEntity.IsTargetable(agent) == false && targetWorldEntity.Properties[PropertyEnum.AITargetableOverride] == false)
                    return PowerUseResult.BadTarget;

                if (powerContext.TargetsWorldPosition)
                    targetIdForPower = Entity.InvalidId;
                else
                    targetIdForPower = targetWorldEntity.Id;

                if (powerContext.SecondaryTargetSelection != null)
                {
                    var selectionContext = new SelectEntity.SelectEntityContext(controller, powerContext.SecondaryTargetSelection);
                    WorldEntity secondaryTarget = SelectEntity.DoSelectEntity(selectionContext);
                    if (secondaryTarget != null)
                    {
                        if (secondaryTarget.Id == targetIdForPower)
                            return genericErrorResult;

                        targetRegionLocation = secondaryTarget.RegionLocation;
                    }
                    else
                        return PowerUseResult.BadTarget;
                }
                else
                    targetRegionLocation = targetWorldEntity.RegionLocation;

                PowerPrototype powerProto = power.Prototype;
                if (powerProto == null) return genericErrorResult;

                Vector3 targetPosition = targetWorldEntity.RegionLocation.Position;
                if (Power.TargetsAOE(powerProto))
                {
                    if (powerContext.UseMainTargetForAOEActivation)
                    {
                        if (power.IsTargetInAOE(targetWorldEntity, agent, regionLocation.Position, targetPosition, power.GetApplicationRange(), -1, TimeSpan.Zero, powerProto, agent.Properties))
                            return PowerUseResult.OutOfPosition;
                    }
                    else
                    {
                        targetIdForPower = Entity.InvalidId;

                        var targetingReachProto = powerProto.TargetingReach.As<TargetingReachPrototype>();
                        if (targetingReachProto == null) return genericErrorResult;

                        var volume = new Sphere(regionLocation.Position, powerProto.Radius);

                        foreach (WorldEntity target in region.IterateEntitiesInVolume(volume, new EntityRegionSPContext(EntityRegionSPContextFlags.ActivePartition)))
                        {
                            if (target == null)
                            {
                                Logger.Warn($"Invalid target in region {region}!");
                                continue;
                            }

                            if (Power.ValidateAOETarget(target, powerProto, agent, agent.RegionLocation.Position,
                                agent.Alliance, targetingReachProto.RequiresLineOfSight))
                            {
                                targetIdForPower = target.Id;
                                break;
                            }
                        }

                        if (targetIdForPower == Entity.InvalidId)
                            return PowerUseResult.BadTarget;
                    }
                }
            }

            if (regionLocation.RegionId == 0 || targetRegionLocation.RegionId == 0 || regionLocation.RegionId != targetRegionLocation.RegionId)
                return PowerUseResult.TargetIsMissing;

            Vector3 targetPositionForPower;

            if (powerContext.ForceInvalidTargetActivation && Vector3.IsNearZero(blackboard.UsePowerTargetPos) == false)
                targetPositionForPower = blackboard.UsePowerTargetPos;
            else
            {
                if ((power.TargetsAOE() || power.NeedsTarget() == false)
                    && targetingShape != TargetingShapeType.SkillShot
                    && targetingShape != TargetingShapeType.SkillShotAlongGround
                    && targetingShape != TargetingShapeType.Self)
                    targetPositionForPower = targetRegionLocation.ProjectToFloor();
                else
                    targetPositionForPower = targetRegionLocation.Position;

                if (powerContext.ForceInvalidTargetActivation && targetingShape != TargetingShapeType.Self)
                    targetPositionForPower += agent.Forward;

                if (powerContext.ChooseRandomTargetPosition)
                {
                    if (GetRandomTargetPosition(agent, targetWorldEntity, powerContext, ref targetPositionForPower) == false)
                        return PowerUseResult.OutOfPosition;

                    if (targetPositionForPower == Vector3.Zero)
                        Logger.Warn($"StaticAI_UsePower getRandomTargetPosition succeeded with a result of (0,0,0). UsePowerTargetPos=[{blackboard.UsePowerTargetPos}] Power=[{GameDatabase.GetPrototypeName(powerRef)}] OwnerAgent=[{(agent != null ? agent : "NULL")}] TargetIdForPower=[{targetIdForPower}] TargetEntity=[{(targetWorldEntity != null ? targetWorldEntity : "NULL")}]");
                }
                else if (GetLinearTargetPosition(agent, targetWorldEntity, powerContext, ref targetPositionForPower) == false)
                    return PowerUseResult.OutOfPosition;
            }

            if (targetingShape != TargetingShapeType.Self
                && ApplyAngleOffsetToTargetPosition(agent, powerContext, ref targetPositionForPower) == false)
                return PowerUseResult.OutOfPosition;

            if (powerContext.ForceCheckTargetRegionLocation)
            {
                Bounds targetPositionBounds = agent.Bounds;
                targetPositionBounds.Center = targetPositionForPower;

                PositionCheckFlags positionCheckFlags = PositionCheckFlags.CheckCanBlockedEntity | PositionCheckFlags.CheckCanSweepTo;
                if (region.IsLocationClear(targetPositionBounds, agent.GetPathFlags(), positionCheckFlags) == false
                    || agent.CheckCanPathTo(targetPositionForPower) != NaviPathResult.Success)
                    return PowerUseResult.OutOfPosition;
            }

            Power movementPower = agent.GetPower(PowerPrototype.RecursiveGetPowerRefOfPowerTypeInCombo<MovementPowerPrototype>(powerRef));
            if (movementPower != null)
            {
                if (agent.Locomotor == null) return genericErrorResult;

                if (agent.IsInPositionForPower(movementPower, targetWorldEntity, targetRegionLocation.Position) != IsInPositionForPowerResult.Success)
                    return PowerUseResult.OutOfPosition;

                if (powerContext.AllowMovementClipping == false)
                {
                    Vector3 sweepPos = Vector3.Zero;
                    var targetEntityId = Entity.InvalidId;
                    if (targetWorldEntity != null) targetEntityId = targetWorldEntity.Id;
                    if (movementPower.PowerPositionSweep(regionLocation, targetPositionForPower, targetEntityId, sweepPos) == PowerPositionSweepResult.Clipped)
                        return PowerUseResult.OutOfPosition;
                }

                if (agent.IsImmobilized || agent.IsSystemImmobilized)
                    return PowerUseResult.RestrictiveCondition;
            }

            if (targetIdForPower != Entity.InvalidId && targetWorldEntity != null)
            {
                if (powerContext.MinDistanceToTarget > 0f)
                {
                    float distToTargetSq = agent.GetDistanceTo(targetWorldEntity, true);
                    if (distToTargetSq < powerContext.MinDistanceToTarget)
                        return PowerUseResult.OutOfPosition;
                }

                if (powerContext.MaxDistanceToTarget > 0f)
                {
                    float distToTargetSq = agent.GetDistanceTo(targetWorldEntity, true);
                    if (distToTargetSq > powerContext.MaxDistanceToTarget)
                        return PowerUseResult.OutOfPosition;
                }
            }

            PowerUseResult powerUseResult = agent.CanActivatePower(power, targetIdForPower, targetPositionForPower);
            switch (powerUseResult)
            {
                case PowerUseResult.Success:
                    break;

                case PowerUseResult.OutOfPosition:
                    if (powerContext.IgnoreOutOfPositionFailure == false)
                        return PowerUseResult.OutOfPosition;
                    break;

                default:
                    return powerUseResult;
            }

            if (powerContext.RequireOriPriorToActivate
                && CheckAgentOrientation(agent, targetPositionForPower, powerContext.OrientationThreshold) == false)
                return PowerUseResult.OutOfPosition;

            if (targetingShape != TargetingShapeType.Self && power.RequiresLineOfSight() == false && powerContext.ForceIgnoreLOS == false
                && CheckLineOfSightForPower(agent, power, targetWorldEntity, targetPositionForPower) == false)
                return PowerUseResult.OutOfPosition;

            blackboard.PropertyCollection[PropertyEnum.AIUsePowerTargetID] = targetIdForPower;
            blackboard.UsePowerTargetPos = targetPositionForPower;

            return PowerUseResult.Success;
        }

        private static bool CheckLineOfSightForPower(Agent agent, Power power, WorldEntity target, Vector3 targetPosition)
        {
            PowerPrototype powerProto = power.Prototype;
            if (powerProto == null) return false;

            Vector3 position = targetPosition;
            float radius = 0f;
            float padding = 0f;

            if (powerProto is MissilePowerPrototype missilePowerProto)
            {
                if (target == null)
                {
                    Logger.Warn($"{agent}: is trying to check los with an invalid target for missile power {power}");
                    return false;
                }

                radius = missilePowerProto.MaximumMissileBoundsSphereRadius;
                padding = Locomotor.MovementSweepPadding;

                if (radius > 0f)
                {
                    Vector3 toOwner2D = Vector3.Flatten(agent.RegionLocation.Position - targetPosition, Axis.Z);
                    if (Vector3.IsNearZero(toOwner2D) == false)
                        position += Vector3.Normalize(toOwner2D) * (target.Bounds.Radius + radius);
                }
            }

            return agent.LineOfSightTo(position, radius, padding);
        }

        private static bool CheckAgentOrientation(Agent agent, Vector3 targetPosition, float orientationThreshold)
        {
            Vector3 targetDirection2d = new(targetPosition - agent.RegionLocation.Position);
            targetDirection2d.Z = 0f;

            if (Vector3.LengthSqr(targetDirection2d) == 0f) return false;

            targetDirection2d = Vector3.Normalize(targetDirection2d);

            Vector3 agentDirection2d = agent.Forward.To2D();
            agentDirection2d = Vector3.Normalize(agentDirection2d);

            float dotTargetDir = Vector3.Dot2D(agentDirection2d, targetDirection2d);
            float degreeNeeded = Segment.EpsilonTest(dotTargetDir, 1f) ? 0f : MathHelper.ToDegrees(MathF.Acos(dotTargetDir));

            if (orientationThreshold > 0f)
            {
                if (degreeNeeded > orientationThreshold)
                    return false;
            }
            else if (Segment.EpsilonTest(degreeNeeded, 0f, 0.1f) == false)
                return false;

            return true;
        }

        private static bool GetRandomTargetPosition(Agent agent, WorldEntity targetWorldEntity, in UsePowerContext powerContext, ref Vector3 targetPosition)
        {
            WorldEntity worldEntity;
            if (powerContext.ForceInvalidTargetActivation && targetWorldEntity == null)
                worldEntity = agent;
            else
            {
                worldEntity = targetWorldEntity;
                if (worldEntity == null)
                {
                    Logger.Warn($"Invalid target on {agent}");
                    return false;
                }
            }

            Region region = agent.Region;
            if (region == null) return false;

            Bounds bounds = agent.Bounds;
            bounds.Center = worldEntity.RegionLocation.Position;

            float minTargetDistance = powerContext.TargetOffset;
            float maxTargetDistance = minTargetDistance + powerContext.OffsetVarianceMagnitude;
            float minDistance = powerContext.MinDistanceFromOwner;

            DistanceRangePredicate distanceRangePredicat = new(agent.RegionLocation.Position, minDistance, DistanceRangePredicate.Unbound);
            return region.ChooseRandomPositionNearPoint(bounds, Region.GetPathFlagsForEntity(worldEntity.WorldEntityPrototype),
                (PositionCheckFlags.CheckCanBlockedEntity | PositionCheckFlags.CheckCanSweepTo), BlockingCheckFlags.None,
                minTargetDistance, maxTargetDistance, out targetPosition, distanceRangePredicat);
        }

        private static bool GetLinearTargetPosition(Agent agent, WorldEntity targetEntity, in UsePowerContext powerContext, ref Vector3 targetPosition)
        {
            Game game = agent.Game;
            if (game == null) return false;
            GRandom random = game.Random;
            Vector3 position = agent.RegionLocation.Position;

            if (Segment.IsNearZero(powerContext.TargetOffset) == false)
            {
                Logger.Warn($"An ActionUsePower has non-zero values for both TargetOffset and OwnerOffset. Ignoring OwnerOffset.\nAgent: {agent}");
               
                float targetOffsetPlusVariance = powerContext.TargetOffset + (random.NextFloat() * powerContext.OffsetVarianceMagnitude);

                if (targetEntity != null && targetEntity != agent)
                {
                    Vector3 toTarget2D = Vector3.Flatten(targetPosition - agent.RegionLocation.Position, Axis.Z);
                    if (Vector3.IsNearZero(toTarget2D) == false)
                        targetPosition += Vector3.Normalize(toTarget2D) * targetOffsetPlusVariance;
                }
                else
                    targetPosition += (agent.Forward * targetOffsetPlusVariance);
            }
            else if (Segment.IsNearZero(powerContext.OwnerOffset) == false)
            {                
                float offsetVariance = powerContext.OwnerOffset + (random.NextFloat() * powerContext.OffsetVarianceMagnitude);
                targetPosition = position + agent.Forward * (agent.Bounds.Radius + offsetVariance);
            }

            return true;
        }

        private static bool ApplyAngleOffsetToTargetPosition(Agent agent, in UsePowerContext powerContext, ref Vector3 targetPosition)
        {
            if (Segment.IsNearZero(powerContext.TargetAngleOffset) == false)
            {
                Vector3 position = agent.RegionLocation.Position;
                Vector3 toTarget = targetPosition - position;

                if (Vector3.IsNearZero(toTarget) == false)
                {
                    Matrix3 matRotation = Matrix3.RotationZ(MathHelper.ToRadians(powerContext.TargetAngleOffset));
                    targetPosition = position + (matRotation * toTarget);
                }
            }

            return true;
        }
    }

    public class DistanceRangePredicate : RandomPositionPredicate
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public const float Unbound = -1.0f;

        private readonly Vector3 _position;
        private readonly float _minDistanceSq;
        private readonly float _maxDistanceSq;

        public DistanceRangePredicate(Vector3 position, float minDistance, float maxDistance)
        {
            _position = position;

            if (minDistance != Unbound && maxDistance != Unbound && minDistance > maxDistance)
            {
                Logger.Warn($"DistanceRangePredicate min distance of {minDistance} is greater than max distance of {maxDistance}! Swapping for now, but this should be fixed.");
                (maxDistance, minDistance) = (minDistance, maxDistance);
            }

            if (minDistance == Unbound || minDistance < 0.0f)
            {
                if (minDistance < 0.0f)
                    Logger.Warn($"DistanceRangePredicate min distance must be either unbound or >= 0! Current value is {minDistance}. Forcing min to unbound.");
                _minDistanceSq = Unbound;
            }
            else
                _minDistanceSq = minDistance * minDistance;

            if (maxDistance == Unbound || maxDistance <= 0.0f)
            {
                if (maxDistance <= 0.0f)
                    Logger.Warn($"DistanceRangePredicate max distance must be either unbound or > 0! Current value is {maxDistance}. Forcing max to unbound.");
                _maxDistanceSq = Unbound;
            }
            else
                _maxDistanceSq = maxDistance * maxDistance;

            if (minDistance == Unbound && maxDistance == Unbound)
                Logger.Warn("DisplacementRangePredicate's min and max values are both unbound; it's useless!");
        }

        public override bool Test(Vector3 testPosition)
        {
            float distanceSq = Vector3.DistanceSquared(testPosition, _position);
            return (_minDistanceSq == Unbound || distanceSq >= _minDistanceSq)
                && (_maxDistanceSq == Unbound || distanceSq <= _maxDistanceSq);
        }
    }

    public struct UsePowerContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public PrototypeId Power;
        public SelectEntityContextPrototype SecondaryTargetSelection;
        public bool RequireOriPriorToActivate;
        public bool ForceIgnoreLOS;
        public bool ForceCheckTargetRegionLocation;
        public bool ChooseRandomTargetPosition;
        public bool TargetsWorldPosition;
        public bool UseMainTargetForAOEActivation;
        public bool ForceInvalidTargetActivation;
        public bool AllowMovementClipping;
        public bool IgnoreOutOfPositionFailure;
        public float TargetAngleOffset;
        public float TargetOffset;
        public float OwnerOffset;
        public float OrientationThreshold;
        public float OffsetVarianceMagnitude;
        public float MinDistanceFromOwner;
        public float MaxDistanceToTarget;
        public float MinDistanceToTarget;
        public PrototypeId[] DifficultyTierRestrictions;

        public UsePowerContext(AIController ownerController, UsePowerContextPrototype proto)
        {
            OwnerController = ownerController;
            ChooseRandomTargetPosition = proto.ChooseRandomTargetPosition;
            ForceIgnoreLOS = proto.ForceIgnoreLOS;
            ForceCheckTargetRegionLocation = proto.ForceCheckTargetRegionLocation;
            OffsetVarianceMagnitude = proto.OffsetVarianceMagnitude;
            OwnerOffset = proto.OwnerOffset;
            Power = proto.Power;
            RequireOriPriorToActivate = proto.RequireOriPriorToActivate;
            OrientationThreshold = proto.OrientationThreshold;
            TargetAngleOffset = proto.TargetAngleOffset;
            TargetOffset = proto.TargetOffset;
            TargetsWorldPosition = proto.TargetsWorldPosition;
            SecondaryTargetSelection = proto.SecondaryTargetSelection;
            UseMainTargetForAOEActivation = proto.UseMainTargetForAOEActivation;
            MinDistanceFromOwner = proto.MinDistanceFromOwner;
            ForceInvalidTargetActivation = proto.ForceInvalidTargetActivation;
            AllowMovementClipping = proto.AllowMovementClipping;
            IgnoreOutOfPositionFailure = proto.IgnoreOutOfPositionFailure;
            MaxDistanceToTarget = proto.MaxDistanceToTarget;
            MinDistanceToTarget = proto.MinDistanceToTarget;
            DifficultyTierRestrictions = proto.DifficultyTierRestrictions;
        }
    }
}
