using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public partial class Power
    {
        public PowerUseResult CanActivate(WorldEntity target, Vector3 targetPosition, PowerActivationSettingsFlags flags)
        {
            if (IsToggledOn())
                return PowerUseResult.Success;

            if (IsOnExtraActivation())
                return PowerUseResult.Success;

            PowerUseResult canTriggerResult = CanTrigger(flags);
            if (canTriggerResult != PowerUseResult.Success)
                return canTriggerResult;

            if (NeedsTarget() && IsValidTarget(target) == false)
                return PowerUseResult.BadTarget;

            return PowerUseResult.Success;
        }

        public PowerUseResult CanTrigger(PowerActivationSettingsFlags flags = PowerActivationSettingsFlags.None)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerUseResult.GenericError, "CanTrigger(): powerProto == null");

            if (Segment.IsNearZero(LiveTuningManager.GetLivePowerTuningVar(powerProto, PowerTuningVar.ePTV_PowerEnabled)))
                return PowerUseResult.DisabledByLiveTuning;

            if (Owner == null) return Logger.WarnReturn(PowerUseResult.GenericError, "CanTrigger(): Owner == null");

            if (Owner.IsDead && IsUseableWhileDead() == false)
                return PowerUseResult.OwnerDead;

            int powerChargesMax = Owner.GetPowerChargesMax(powerProto.DataRef);
            if (powerChargesMax > 0)
            {
                if (Owner.GetPowerChargesAvailable(powerProto.DataRef) <= 0)
                    return PowerUseResult.InsufficientCharges;
            }
            else if (GetCooldownTimeRemaining() > TimeSpan.Zero)
            {
                return PowerUseResult.Cooldown;
            }

            if (IsOnExtraActivation(powerProto, Owner))
                return PowerUseResult.Success;

            PowerUseResult ownerCanTriggerPowerResult = Owner.CanTriggerPower(powerProto, this, flags);
            if (ownerCanTriggerPowerResult != PowerUseResult.Success)
                return ownerCanTriggerPowerResult;

            if (IsToggledOn(powerProto, Owner))
                return PowerUseResult.Success;

            if (CheckEnduranceCost() == false)
                return PowerUseResult.InsufficientEndurance;

            if (Properties[PropertyEnum.SecondaryResourceRequired] && CanUseSecondaryResourceEffects() == false)
                return PowerUseResult.InsufficientSecondaryResource;

            if (CheckCanTriggerEval() == false)
                return PowerUseResult.RestrictiveCondition;

            return PowerUseResult.Success;                
        }

        public bool CheckCanTriggerEval()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "powerProto == null");

            if (powerProto.EvalCanTrigger == null)
                return true;

            if (Owner == null) return Logger.WarnReturn(false, "Owner == null");

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, Properties);
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
            contextData.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, Owner.ConditionCollection);
            contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var2, Owner);

            return Eval.RunBool(powerProto.EvalCanTrigger, contextData);
        }

        public PowerPositionSweepResult PowerPositionSweep(RegionLocation regionLocation, Vector3 targetPosition, ulong targetId,
            ref Vector3? resultPosition, bool forceDoNotMoveToExactTargetLocation = false, float rangeOverride = 0f)
        {
            if (Owner == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): Owner == null");

            Region region = regionLocation.Region;
            if (region == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): region == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): powerProto == null");

            resultPosition = targetPosition;

            if (powerProto is MovementPowerPrototype movementPowerProto)
            {
                if (movementPowerProto.PowerMovementPathPct > 0f || movementPowerProto.TeleportMethod != TeleportMethodType.None)
                {
                    Locomotor locomotor = Owner.Locomotor;
                    if (locomotor == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweep(): locomotor == null");

                    bool doNotMoveToExactTargetLocation = forceDoNotMoveToExactTargetLocation || movementPowerProto.MoveToExactTargetLocation == false;
                    float range = Segment.IsNearZero(rangeOverride) ? GetRange() : rangeOverride;

                    Vector3 findPoint = resultPosition.Value;
                    PointOnLineResult result = region.NaviMesh.FindPointOnLineToOccupy(ref findPoint, regionLocation.Position, targetPosition, range,
                        Owner.Bounds, locomotor.PathFlags, movementPowerProto.BlockingCheckFlags, doNotMoveToExactTargetLocation);
                    resultPosition = findPoint;

                    return result switch
                    {
                        PointOnLineResult.Failed    => PowerPositionSweepResult.TargetPositionInvalid,
                        PointOnLineResult.Clipped   => PowerPositionSweepResult.Clipped,
                        PointOnLineResult.Success   => PowerPositionSweepResult.Success,
                        _                           => PowerPositionSweepResult.Error,
                    };
                }
            }

            return PowerPositionSweepInternal(regionLocation, targetPosition, targetId, ref resultPosition, false, false);
        }

        public bool PowerLOSCheck(RegionLocation regionLocation, Vector3 targetPosition, ulong targetId, ref Vector3? resultPosition, bool losCheckAlongGround)
        {
            PowerPositionSweepResult result = PowerPositionSweepInternal(regionLocation, targetPosition, targetId, ref resultPosition, true, losCheckAlongGround);

            if (result == PowerPositionSweepResult.Clipped)
                return Vector3.DistanceSquared(targetPosition, resultPosition.Value) <= PowerPositionSweepPaddingSquared;

            return result == PowerPositionSweepResult.Success;
        }

        public bool IsInRange(WorldEntity target, RangeCheckType checkType)
        {
            if (target == null) return Logger.WarnReturn(false, "IsInRange(): target == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");

            float range = GetRange();
            Vector3 userPosition = Owner.RegionLocation.Position;
            float userRadius = Owner.Bounds.Radius;
            Vector3 targetPosition = target.RegionLocation.Position;
            float targetRadius = target.Bounds.Radius;

            return IsInRangeInternal(powerProto, range, userPosition, userRadius, targetPosition, checkType, targetRadius);
        }

        public bool IsInRange(Vector3 targetPosition, RangeCheckType checkType)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "IsInRange(): powerProto == null");

            float range = GetRange();
            Vector3 userPosition = Owner.RegionLocation.Position;
            float userRadius = Owner.Bounds.Radius;

            return IsInRangeInternal(powerProto, range, userPosition, userRadius, targetPosition, checkType, 0f);
        }

        public bool IsValidTarget(WorldEntity target)
        {
            if (Owner == null) return Logger.WarnReturn(false, "IsValidTarget(): Owner == null");
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsValidTarget(): powerProto == null");
            return IsValidTarget(powerProto, Owner, Owner.Alliance, target);
        }

        public static bool IsValidTarget(PowerPrototype powerProto, WorldEntity user, AlliancePrototype userAllianceProto, WorldEntity target)
        {
            ulong userEntityId = user != null ? user.Id : Entity.InvalidId;

            (bool result, bool noMoreChecksRequired) = IsValidTargetNoCasterEntityChecks(powerProto, userEntityId, userAllianceProto, target);

            if (noMoreChecksRequired)
                return result;

            return IsValidTargetInternal(powerProto, user, userEntityId, userAllianceProto, target);
        }

        public static bool ValidateAOETarget(WorldEntity target, PowerPrototype powerProto, WorldEntity user, Vector3 userPosition,
            AlliancePrototype userAllianceProto, bool requiresLineOfSight)
        {
            if (IsValidTarget(powerProto, user, userAllianceProto, target) == false)
                return false;

            if (requiresLineOfSight == false)
                return true;

            Game game = target.Game;
            if (game == null) return Logger.WarnReturn(false, "ValidateAOETarget(): game == null");

            if (user == null)
            {
                if (target.LineOfSightTo(userPosition) == false)
                    return false;
            }
            else if (user.LineOfSightTo(target) == false)
            {
                return false;
            }

            return true;
        }

        public bool IsValidSituationalTarget(WorldEntity target)
        {
            return _situationalComponent != null;
        }

        public bool CanUseSecondaryResourceEffects()
        {
            WorldEntity ultimateOwner = GetUltimateOwner();

            if (ultimateOwner == null)
                return false;

            return CanUseSecondaryResourceEffects(Properties, ultimateOwner.Properties);
        }

        public static bool CanUseSecondaryResourceEffects(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            // TODO
            return true;
        }

        private static (bool, bool) IsValidTargetNoCasterEntityChecks(PowerPrototype powerProto, ulong userEntityId,
            AlliancePrototype userAllianceProto, WorldEntity target)
        {
            // Validate target ref
            if (target == null)
            {
                if (NeedsTarget(powerProto))
                    return (false, true);

                return (true, true);
            }

            // Validate target status
            if (target.TestStatus(EntityStatus.Destroyed))
                return (false, true);

            if (target.IsUnaffectable && target.Id != userEntityId)
                return (false, true);

            TargetingReachPrototype reachPrototype = powerProto.GetTargetingReach();
            if (reachPrototype == null) return Logger.WarnReturn((false, true), "IsValidTargetNoCasterEntityChecks(): reachPrototype == null");

            if ((reachPrototype.EntityHealthState == EntityHealthState.Alive && target.IsDead) ||
                (reachPrototype.EntityHealthState == EntityHealthState.Dead && target.IsDead == false))
            {
                return (false, true);
            }

            // Validate based on targeting style
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn((false, true), "IsValidTargetNoCasterEntityChecks(): stylePrototype == null");

            if (stylePrototype.TargetingShape == TargetingShapeType.Self)
            {
                if (target.Id != userEntityId)
                    return (false, true);

                if (target.IsInWorld == false)
                    return (false, true);

                if (target.IsSimulated == false)
                    return (false, true);
            }
            else if (target.IsAffectedByPowers() == false)
            {
                return (false, true);
            }

            // Validate player
            Player player = target.GetOwnerOfType<Player>();
            if (player != null && player.IsTargetable(userAllianceProto) == false)
                return (false, true);

            // Check for power immunities
            if (target.HasPowerImmunity && target.Properties[PropertyEnum.ImmuneToPower, powerProto.DataRef])
                return (false, true);

            // Additional user checks are required
            return (false, false);
        }

        private static bool IsValidTargetInternal(PowerPrototype powerProto, WorldEntity user, ulong userEntityId,
            AlliancePrototype userAllianceProto, WorldEntity target)
        {
            // NOTE: In the client this method is called Power::isValidTarget() (starting with a lower case character),
            // but we are calling it IsValidTargetInternal() for clarity and consistency.

            TargetingReachPrototype reachPrototype = powerProto.GetTargetingReach();
            if (reachPrototype == null) return Logger.WarnReturn(false, "IsValidTargetInternal(): reachPrototype == null");

            // Height
            switch (reachPrototype.TargetingHeightType)
            {
                case TargetingHeightType.GroundOnly:
                    if (target.IsHighFlying)
                        return false;
                    break;

                case TargetingHeightType.SameHeight:
                    if (user != null && (user.IsHighFlying != target.IsHighFlying))
                        return false;
                    break;

                case TargetingHeightType.FlyingOnly:
                    if (target.IsHighFlying == false)
                        return false;
                    break;
            }

            // Check if the target is in the front if this power requires it
            if (reachPrototype.TargetsFrontSideOnly && user != null && user.IsInWorld && target.IsInWorld)
            {
                Vector3 distanceToTarget = target.RegionLocation.Position - user.RegionLocation.Position;
                Vector3 userForward = user.Forward;
                float dot2D = Vector3.Dot2D(in distanceToTarget, in userForward);

                float userRadiusSq = user.Bounds.Radius;
                userRadiusSq *= userRadiusSq;

                if (Vector3.LengthSquared2D(distanceToTarget) > userRadiusSq && dot2D < 0)
                    return false;
            }

            // Throwables
            if (powerProto.PowerCategory == PowerCategoryType.ThrowablePower && user != null)
            {
                // Throwable entity cannot be its own target
                if (user.Properties[PropertyEnum.ThrowableOriginatorEntity] == target.Id)
                    return false;
            }

            // Find creators of the owner entity (if there are any)
            Game game = target.Game;
            if (game == null) return Logger.WarnReturn(false, "game == null");

            Agent agentUser = user as Agent;
            Agent creator = null;
            Agent ultimateCreator = null;

            if (user != null && user.HasPowerUserOverride)
            {
                ulong powerUserOverrideId = user.Properties[PropertyEnum.PowerUserOverrideID];
                creator = (powerUserOverrideId != userEntityId)
                    ? game.EntityManager.GetEntity<Agent>(powerUserOverrideId)
                    : agentUser;

                ultimateCreator = creator?.GetMostResponsiblePowerUser<Agent>();
            }

            if (agentUser?.AIController != null || creator?.AIController != null)
            {
                // Run AI-specific checks if needed
                if (TargetMeetsAISpecificConstraints(target, userEntityId, powerProto,
                    userAllianceProto, creator, ultimateCreator) == false)
                {
                    return false;
                }
            }
            else if (user?.CanBePlayerOwned() == true || creator?.CanBePlayerOwned() == true)
            {
                // Run checks for dumb AI-less entities owned by the player
                
                // Find avatar creator
                Avatar avatarCreator = null;
                if (creator != null)
                {
                    avatarCreator = creator as Avatar;
                    if (avatarCreator == null && creator.HasPowerUserOverride)
                    {
                        ulong powerUserOverrideId = creator.Properties[PropertyEnum.PowerUserOverrideID];
                        avatarCreator = game.EntityManager.GetEntity<Avatar>(powerUserOverrideId);
                    }
                }
                else
                {
                    avatarCreator = user as Avatar;
                }

                // Run player-specific checks if found
                if (avatarCreator != null)
                {
                    if (TargetMeetsPlayerSpecificConstraints(target, avatarCreator, reachPrototype, agentUser) == false)
                        return false;
                }
            }
            else
            {
                // Check AI if both user and creator are null - when does this ever happen?
                if (TargetMeetsAISpecificConstraints(target, userEntityId, powerProto,
                    userAllianceProto, creator, ultimateCreator) == false)
                {
                    return false;
                }
            }
            
            // Check situational component
            if (powerProto.SituationalComponent != null && creator != null)
            {
                // NOTE: Is this correct? Shouldn't it be the opposite?
                Power power = creator.GetPower(powerProto.DataRef);
                if (power != null && power.IsSituationalPower && power.IsValidSituationalTarget(target))
                    return false;
            }

            // Run restriction eval checks
            if (TargetPassesRestrictionEval(target, powerProto, user) == false)
                return false;

            // Property restrictions
            if (TargetMeetsRestrictionPropertyConstraints(target, powerProto) == false)
                return false;

            return true;
        }

        private static bool TargetMeetsAISpecificConstraints(WorldEntity target, ulong userEntityId, PowerPrototype powerProto,
            AlliancePrototype userAllianceProto, WorldEntity creator, WorldEntity ultimateCreator)
        {
            TargetingReachPrototype targetingReachProto = powerProto.GetTargetingReach();
            if (targetingReachProto == null) return Logger.WarnReturn(false, "TargetMeetsAISpecificConstraints(): targetingReachProto == null");

            // Check user / creator / ultimate creator
            if (userEntityId == target.Id)
                return targetingReachProto.WillTargetCaster;

            if (creator != null && (creator.Id == target.Id))
                return targetingReachProto.WillTargetCreator;

            if (ultimateCreator != null && (ultimateCreator.Id == target.Id))
                return targetingReachProto.WillTargetUltimateCreator;

            // Check friendly / hostile / destructible
            bool canTargetFriendly = false;
            if (targetingReachProto.TargetsFriendly)
                canTargetFriendly = userAllianceProto != null && userAllianceProto.IsFriendlyTo(target.Alliance);

            bool canTargetEnemy = false;
            if (targetingReachProto.TargetsEnemy)
                canTargetEnemy = userAllianceProto != null && userAllianceProto.IsHostileTo(target.Alliance);

            bool canTargetNonEnemies = false;
            if (targetingReachProto.TargetsNonEnemies)
                canTargetNonEnemies = userAllianceProto != null && userAllianceProto.IsHostileTo(target.Alliance);

            bool canTargetDestructible = false;
            if (targetingReachProto.TargetsDestructibles)
                canTargetDestructible = target.IsDestructible;            

            return canTargetFriendly || canTargetEnemy || canTargetNonEnemies || canTargetDestructible;
        }

        private static bool TargetMeetsPlayerSpecificConstraints(WorldEntity target, Avatar avatarCreator,
            TargetingReachPrototype targetingReach, Agent agentUser)
        {
            if (avatarCreator.Id == target.Id || (agentUser != null && agentUser.Id == target.Id))
                return targetingReach.WillTargetCaster;

            // Check player restrictions
            ulong restrictedToPlayerGuid = target.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (restrictedToPlayerGuid != 0)
            {
                Player player = avatarCreator.GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false,
                    $"TargetMeetsPlayerSpecificConstraints(): An avatar is trying to cast a power without an owning player!\nAvatar: [{avatarCreator}]");

                if (player.DatabaseUniqueId != restrictedToPlayerGuid)
                    return false;
            }

            // Check friendly / hostile / destructible restrictions
            bool canTargetFriendly = false;
            if (targetingReach.TargetsFriendly)
            {
                if (targetingReach.PartyOnly)
                {
                    ulong userPartyId = avatarCreator.PartyId;
                    if (userPartyId != 0)
                        canTargetFriendly = userPartyId == target.PartyId;
                }
                else
                {
                    canTargetFriendly = avatarCreator.IsFriendlyTo(target.Alliance);
                }
            }

            bool canTargetEnemy = false;
            if (targetingReach.TargetsEnemy)
                canTargetEnemy = avatarCreator.IsHostileTo(target.Alliance);

            bool canTargetDestructible = false;
            if (targetingReach.TargetsDestructibles)
                canTargetDestructible = target.IsDestructible;

            return canTargetFriendly || canTargetEnemy || canTargetDestructible;
        }

        private static bool TargetPassesRestrictionEval(WorldEntity target, PowerPrototype powerProto, WorldEntity user)
        {
            if (powerProto.TargetRestrictionEval == null)
                return true;

            Logger.Debug("TargetPassesRestrictionEval()");

            EvalContextData contextData = new(target.Game);
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, powerProto.Properties);
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, target.Properties);
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, user.Properties);
            contextData.SetReadOnlyVar_EntityPtr(EvalContext.Var1, target);

            return Eval.RunBool(powerProto.TargetRestrictionEval, contextData);
        }

        private static bool TargetMeetsRestrictionPropertyConstraints(WorldEntity target, PowerPrototype powerProto)
        {
            if (powerProto.Properties == null)
                return Logger.WarnReturn(false, "TargetMeetsRestrictionPropertyConstraints(): powerProto.Properties == null");

            // Check TargetRestriction properties
            foreach (var kvp in powerProto.Properties.IteratePropertyRange(PropertyEnum.TargetRestriction))
            {
                // NOTE: The client implemenetation uses param cache functionality of the PropertyList.
                // If we run into performance issues, we should consider doing something similar.

                Property.FromParam(kvp.Key, 0, out int targetRestrictionTypeParam);
                var targetRestrictionType = (TargetRestrictionType)targetRestrictionTypeParam;

                switch (targetRestrictionType)
                {
                    case TargetRestrictionType.HealthGreaterThanPercentage:
                        {
                            if (MathHelper.IsBelowOrEqual(target.Properties[PropertyEnum.Health], target.Properties[PropertyEnum.HealthMaxOther], kvp.Value))
                                return false;

                            break;
                        }

                    case TargetRestrictionType.HealthLessThanPercentage:
                        {
                            if (MathHelper.IsAboveOrEqual(target.Properties[PropertyEnum.Health], target.Properties[PropertyEnum.HealthMaxOther], kvp.Value))
                                return false;

                            break;
                        }

                    case TargetRestrictionType.EnduranceGreaterThanPercentage:
                        {
                            bool pass = false;

                            foreach (var targetKvp in target.Properties.IteratePropertyRange(PropertyEnum.Endurance))
                            {
                                Property.FromParam(targetKvp.Key, 0, out int manaType);

                                if (targetKvp.Value.RawFloat > kvp.Value.RawFloat * target.Properties[PropertyEnum.EnduranceMaxOther, manaType])
                                {
                                    pass = true;
                                    break;
                                }
                            }

                            if (pass == false)
                                return false;

                            break;
                        }

                    case TargetRestrictionType.EnduranceLessThanPercentage:
                        {
                            bool pass = false;

                            foreach (var targetKvp in target.Properties.IteratePropertyRange(PropertyEnum.Endurance))
                            {
                                Property.FromParam(targetKvp.Key, 0, out int manaType);

                                if (targetKvp.Value.RawFloat < kvp.Value.RawFloat * target.Properties[PropertyEnum.EnduranceMaxOther, manaType])
                                {
                                    pass = true;
                                    break;
                                }
                            }

                            if (pass == false)
                                return false;

                            break;
                        }

                    case TargetRestrictionType.HealthOrEnduranceGreaterThanPercentage:
                        {
                            bool healthPass = false;
                            bool endurancePass = false;

                            foreach (var targetKvp in target.Properties.IteratePropertyRange(PropertyEnum.Endurance))
                            {
                                Property.FromParam(targetKvp.Key, 0, out int manaType);

                                if (targetKvp.Value.RawFloat > kvp.Value.RawFloat * target.Properties[PropertyEnum.EnduranceMaxOther, manaType])
                                {
                                    endurancePass = true;
                                    break;
                                }
                            }

                            if (MathHelper.IsAbove(target.Properties[PropertyEnum.Health], target.Properties[PropertyEnum.HealthMaxOther], kvp.Value))
                                healthPass = true;

                            if (healthPass == false && endurancePass == false)
                                return false;

                            break;
                        }

                    case TargetRestrictionType.HealthOrEnduranceLessThanPercentage:
                        {
                            bool healthPass = false;
                            bool endurancePass = false;

                            foreach (var targetKvp in target.Properties.IteratePropertyRange(PropertyEnum.Endurance))
                            {
                                Property.FromParam(targetKvp.Key, 0, out int manaType);

                                if (targetKvp.Value.RawFloat < kvp.Value.RawFloat * target.Properties[PropertyEnum.EnduranceMaxOther, manaType])
                                {
                                    endurancePass = true;
                                    break;
                                }
                            }

                            if (MathHelper.IsBelow(target.Properties[PropertyEnum.Health], target.Properties[PropertyEnum.HealthMaxOther], kvp.Value))
                                healthPass = true;

                            if (healthPass == false && endurancePass == false)
                                return false;

                            break;
                        }

                    case TargetRestrictionType.SecondaryResourceLessThanPercentage:
                        {
                            if (target.Properties[PropertyEnum.SecondaryResource] >= kvp.Value.RawFloat * target.Properties[PropertyEnum.SecondaryResourceMax])
                                return false;

                            break;
                        }

                    case TargetRestrictionType.HasKeyword:
                    case TargetRestrictionType.DoesNotHaveKeyword:
                        {
                            Property.FromParam(kvp.Key, 1, out PrototypeId keywordProtoRef);

                            if (keywordProtoRef == PrototypeId.Invalid)
                                return Logger.WarnReturn(false, "TargetMeetsRestrictionPropertyConstraints(): keywordProtoRef == PrototypeId.Invalid");

                            bool hasKeyword = (DataDirectory.Instance.PrototypeIsA<EntityKeywordPrototype>(keywordProtoRef) && target.HasKeyword(keywordProtoRef))
                                || (DataDirectory.Instance.PrototypeIsA<PowerKeywordPrototype>(keywordProtoRef) && target.HasConditionWithKeyword(keywordProtoRef));

                            if (targetRestrictionType == TargetRestrictionType.HasKeyword && hasKeyword == false)
                                return false;
                            else if (targetRestrictionType == TargetRestrictionType.DoesNotHaveKeyword && hasKeyword)
                                return false;

                            break;
                        }

                    case TargetRestrictionType.HasAI:
                        {
                            if (target is not Agent agent || agent.AIController == null)
                                return false;

                            break;
                        }

                    case TargetRestrictionType.IsPrototypeOf:
                        {
                            Property.FromParam(kvp.Key, 2, out PrototypeId entityRef);
                            
                            if (entityRef == PrototypeId.Invalid)
                                return Logger.WarnReturn(false, "TargetMeetsRestrictionPropertyConstraints(): entityRef == PrototypeId.Invalid");

                            if (target.PrototypeDataRef != entityRef)
                                return false;

                            break;
                        }

                    case TargetRestrictionType.HasProperty:
                    case TargetRestrictionType.DoesNotHaveProperty:
                        {
                            Property.FromParam(kvp.Key, 3, out PrototypeId propertyInfoProtoRef);

                            PropertyEnum propertyEnum = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(propertyInfoProtoRef);
                            if (propertyEnum == PropertyEnum.Invalid)
                                return Logger.WarnReturn(false, "TargetMeetsRestrictionPropertyConstraints(): propertyEnum == PropertyEnum.Invalid");

                            if (targetRestrictionType == TargetRestrictionType.HasProperty && target.Properties.HasProperty(propertyEnum) == false)
                                return false;
                            else if (targetRestrictionType == TargetRestrictionType.DoesNotHaveProperty && target.Properties.HasProperty(propertyEnum))
                                return false;

                            break;
                        }
                }
            }

            // Check target rank exclusing / inclusion
            bool hasTargetRankExclusion = powerProto.Properties.HasProperty(PropertyEnum.TargetRankExclusion);
            bool hasTargetRankInclusion = powerProto.Properties.HasProperty(PropertyEnum.TargetRankInclusion);

            if (hasTargetRankExclusion == false && hasTargetRankInclusion == false)
                return true;

            RankPrototype targetRankProto = target.GetRankPrototype();

            if (hasTargetRankExclusion && targetRankProto != null)
            {
                foreach (var kvp in powerProto.Properties.IteratePropertyRange(PropertyEnum.TargetRankExclusion))
                {
                    Property.FromParam(kvp.Key, 0, out int rankParam);

                    if (targetRankProto.Rank == (Rank)rankParam)
                        return false;
                }
            }

            if (hasTargetRankInclusion)
            {
                bool isIncluded = false;

                if (targetRankProto != null)
                {
                    foreach (var kvp in powerProto.Properties.IteratePropertyRange(PropertyEnum.TargetRankInclusion))
                    {
                        Property.FromParam(kvp.Key, 0, out int rankParam);

                        if (targetRankProto.Rank == (Rank)rankParam)
                        {
                            isIncluded = true;
                            break;
                        }
                    }
                }

                if (isIncluded == false)
                    return false;
            }

            return true;
        }

        private static bool IsInRangeInternal(PowerPrototype powerProto, float range, Vector3 userPosition, float userRadius,
            Vector3 targetPosition, RangeCheckType checkType, float targetRadius)
        {
            if (powerProto.Activation == PowerActivationType.Passive)
                return true;

            TargetingStylePrototype targetingPrototype = powerProto.GetTargetingStyle();
            if (targetingPrototype == null) return Logger.WarnReturn(false, "IsInRangeInternal(): targetingPrototype == null");

            if (targetingPrototype.TargetingShape == TargetingShapeType.Self)
                return true;

            if (powerProto.PowerCategory == PowerCategoryType.ProcEffect)
                return true;

            if (powerProto is MovementPowerPrototype movementPowerProto)
            {
                if (movementPowerProto.MoveToExactTargetLocation == false && targetingPrototype.NeedsTarget == false)
                    return true;
            }

            // Distance to the edge of the target
            float distance = Vector3.Distance2D(userPosition, targetPosition) - targetRadius;

            // RangeActivationReduction is not used in GetRange(), and according to PowerPrototype::validateTargetingSettings(),
            // it has something to do with client-server synchronization. It's probably used to have the power activate on the
            // client later to account for latency, so we do not need it on the server I think.
            // UPDATE: Not having this on causes activation desyncs when enemies are stationary and grouped together.
            //if (checkType == RangeCheckType.Activation)
            //    range -= powerProto.RangeActivationReduction;

            // Range cannot be less than user radius. 5 appears to be additional padding
            range = MathF.Max(userRadius, range) + 5f;

            return (distance - range) <= 0f;
        }

        private static bool IsInApplicationRange(WorldEntity target, in Vector3 userPosition, ulong userEntityId, float range, PowerPrototype powerProto)
        {
            if (target == null) return Logger.WarnReturn(false, "IsInApplicationRange(): target == null");

            float userRadius = 0f;
            if (userEntityId != Entity.InvalidId)
            {
                WorldEntity user = target.Game.EntityManager.GetEntity<WorldEntity>(userEntityId);
                if (user != null)
                    userRadius = user.Bounds.Radius;
            }

            Vector3 targetPosition = target.RegionLocation.Position;
            float targetRadius = target.Bounds.Radius;

            return IsInRangeInternal(powerProto, range, userPosition, userRadius, targetPosition, RangeCheckType.Application, targetRadius);
        }

        private PowerPositionSweepResult PowerPositionSweepInternal(RegionLocation regionLocation, Vector3 targetPosition,
            ulong targetId, ref Vector3? resultPosition, bool losCheck, bool losCheckAlongGround)
        {
            if (Owner == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): Owner == null");

            Region region = regionLocation.Region;
            if (region == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): region == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): powerProto == null");

            // This is used multiple times, so we do a cast for it now
            MovementPowerPrototype movementPowerProto = powerProto as MovementPowerPrototype;

            NaviMesh naviMesh = region.NaviMesh;

            // Sweep settings
            Vector3 fromPosition = regionLocation.Position;
            Vector3 toPosition = targetPosition;
            float sweepRadius = 0f;
            PathFlags pathFlags = PathFlags.Power;
            Vector3? resultNormal = null;
            float padding = PowerPositionSweepPadding;
            HeightSweepType heightSweepType = HeightSweepType.None;
            int maximumHeight = short.MaxValue;
            int minimumHeight = short.MinValue;

            bool clipped = false;

            // Determine sweep settings based for the power
            if (losCheck == false && movementPowerProto != null)
            {
                Locomotor locomotor = Owner.Locomotor;
                if (locomotor == null) return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): locomotor == null");

                bool doNotMoveToExactTargetLocation = movementPowerProto.MoveToExactTargetLocation == false;

                PointOnLineResult pointOnLineResult = naviMesh.FindPointOnLineToOccupy(ref toPosition, fromPosition, toPosition, GetRange(),
                    Owner.Bounds, locomotor.PathFlags, movementPowerProto.BlockingCheckFlags, doNotMoveToExactTargetLocation);

                if (pointOnLineResult == PointOnLineResult.Failed)
                    return PowerPositionSweepResult.TargetPositionInvalid;

                clipped = pointOnLineResult == PointOnLineResult.Clipped;
                pathFlags = PathFlags.Walk;
                int movementHeightBonus = movementPowerProto.MovementHeightBonus;

                if (movementHeightBonus != 0 || locomotor.PathFlags.HasFlag(PathFlags.Fly))
                {
                    if (movementHeightBonus > 0)
                        maximumHeight = (int)regionLocation.ProjectToFloor().Z + movementHeightBonus;
                    else
                        minimumHeight = (int)regionLocation.ProjectToFloor().Z + movementHeightBonus;

                    pathFlags |= PathFlags.Fly;
                    heightSweepType = HeightSweepType.Constraint;
                }
            }
            else if (losCheckAlongGround)
            {
                pathFlags = PathFlags.Walk;
            }
            else if (losCheck)
            {
                maximumHeight = (int)MathF.Max(fromPosition.Z + Owner.Bounds.EyeHeight, targetPosition.Z);
                heightSweepType = HeightSweepType.Constraint;
            }

            if (powerProto is MissilePowerPrototype missilePowerProto)
            {
                sweepRadius = missilePowerProto.MaximumMissileBoundsSphereRadius;
            }
            else if (powerProto is SummonPowerPrototype summonPowerProto && losCheck == false)
            {
                if (summonPowerProto.SummonEntityContexts == null)
                    return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): summonPowerProto.SummonEntityContexts == null");

                WorldEntityPrototype nonHotspotSummonEntityPrototype = null;
                float maximumSphereRadius = 0f;

                for (int i = 0; i < summonPowerProto.SummonEntityContexts.Length; i++)
                {
                    WorldEntityPrototype summonedPrototype = summonPowerProto.GetSummonEntity(i, Owner.GetOriginalWorldAsset());
                    if (summonedPrototype == null)
                        return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): summonedPrototype == null");

                    if (summonedPrototype is not HotspotPrototype)
                    {
                        if (summonedPrototype.Bounds == null)
                            return Logger.WarnReturn(PowerPositionSweepResult.Error, "PowerPositionSweepInternal(): summonedPrototype.Bounds == null");

                        float sphereRadius = summonedPrototype.Bounds.GetSphereRadius();
                        if (sphereRadius > maximumSphereRadius)
                        {
                            maximumSphereRadius = sphereRadius;
                            nonHotspotSummonEntityPrototype = summonedPrototype;
                        }
                    }
                }

                if (nonHotspotSummonEntityPrototype != null)
                {
                    Bounds bounds = new(nonHotspotSummonEntityPrototype.Bounds, targetPosition);
                    pathFlags = Region.GetPathFlagsForEntity(nonHotspotSummonEntityPrototype);
                    sweepRadius = bounds.Radius;
                }
            }

            if (powerProto.HeightCheckPadding != 0f)
            {
                if (powerProto.HeightCheckPadding > 0f)
                    maximumHeight = (int)(regionLocation.ProjectToFloor().Z + powerProto.HeightCheckPadding);
                else
                    minimumHeight = (int)(regionLocation.ProjectToFloor().Z + powerProto.HeightCheckPadding);

                pathFlags |= PathFlags.Fly;
                heightSweepType = HeightSweepType.Constraint;
            }

            // Do the first sweep
            SweepResult sweepResult = naviMesh.Sweep(fromPosition, toPosition, sweepRadius, pathFlags, ref resultPosition,
                ref resultNormal, padding, heightSweepType, maximumHeight, minimumHeight, Owner);

            if (sweepResult == SweepResult.Failed)
                return PowerPositionSweepResult.Error;

            if (sweepResult == SweepResult.Success || sweepResult == SweepResult.Clipped)
            {
                if (losCheck)
                {
                    WorldEntity firstHitEntity = region.SweepToFirstHitEntity(fromPosition, toPosition, Owner, targetId,
                        losCheck, sweepRadius + padding, ref resultPosition);

                    if (firstHitEntity != null)
                        sweepResult = SweepResult.Clipped;
                }
                else if (movementPowerProto != null && movementPowerProto.UserNoEntityCollide)
                {
                    int blockFlags = 1 << (int)BoundsMovementPowerBlockType.All;

                    if (movementPowerProto.IsHighFlyingPower == false && movementPowerProto.MovementHeightBonus == 0)
                        blockFlags |= 1 << (int)BoundsMovementPowerBlockType.Ground;

                    WorldEntity firstHitEntity = region.SweepToFirstHitEntity(Owner.Bounds, resultPosition.Value - fromPosition,
                        ref resultPosition, new MovementPowerEntityCollideFunc(blockFlags));

                    if (firstHitEntity != null)
                        sweepResult = SweepResult.Clipped;
                }
            }

            clipped |= sweepResult == SweepResult.Clipped;

            // Do a second sweep if we need more than just LOS
            if (losCheck == false && sweepResult == SweepResult.HeightMap && pathFlags.HasFlag(PathFlags.Fly))
            {
                pathFlags &= ~PathFlags.Fly;
                sweepResult = naviMesh.Sweep(fromPosition, toPosition, sweepRadius, pathFlags, ref resultPosition,
                    ref resultNormal, padding, heightSweepType, maximumHeight, minimumHeight, Owner);

                if (sweepResult == SweepResult.Failed)
                    return PowerPositionSweepResult.Error;

                if (sweepResult == SweepResult.Clipped)
                    return PowerPositionSweepResult.Clipped;
            }

            return clipped ? PowerPositionSweepResult.Clipped : PowerPositionSweepResult.Success;
        }

        private bool CheckEnduranceCost()
        {
            // TODO
            return true;
        }

        private bool CanStartCooldowns()
        {
            if (Owner == null) return Logger.WarnReturn(false, "Owner == null");

            if (Owner.GetPowerChargesMax(PrototypeDataRef) <= 0)
            {
                if (_endCooldownEvent.IsValid == false)
                    return true;

                return IsOnCooldown() == false;
            }

            return true;
        }
        
        private bool CanEndCooldowns()
        {
            return true;
        }

        private bool CanModifyCooldowns()
        {
            return true;
        }

        private bool CanBeUserCanceledNow()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CanBeUserCanceledNow(): powerProto == null");

            if (IsCancelledOnRelease() || IsRecurring())
                return true;

            if (powerProto.CanBeInterrupted == false)
                return false;

            if (_endPowerEvent.IsValid && _endPowerEvent.Get().Flags.HasFlag(EndPowerFlags.ChanneledLoopEnd))
                return false;

            return true;
        }

        private bool CanEndPower(EndPowerFlags flags)
        {
            if (Owner == null) return Logger.WarnReturn(true, "CanEndPower(): Owner == null");

            if (flags.HasFlag(EndPowerFlags.Unassign) ||
                flags.HasFlag(EndPowerFlags.Interrupting) ||
                flags.HasFlag(EndPowerFlags.Force))
            {
                return true;
            }

            if (flags.HasFlag(EndPowerFlags.ExitWorld) && Owner.IsInWorld == false)
                return true;

            if (_activationPhase == PowerActivationPhase.Inactive)
                return false;

            if (_activationPhase == PowerActivationPhase.LoopEnding && flags.HasFlag(EndPowerFlags.ChanneledLoopEnd) == false)
                return false;

            if (IsHighFlyingPower() && Owner.CheckLandingSpot(this) == false)
                return false;

            return true;
        }
    }
}
