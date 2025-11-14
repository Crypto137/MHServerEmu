using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public enum IsInPositionForPowerResult
    {
        Error,
        Success,
        BadTargetPosition,
        OutOfRange,
        NoPowerLOS
    }

    public enum PowersRespecReason    // UnrealGameAdapter::ShowPowersRespecNotificationDialog()
    {
        Invalid,
        PlayerRequest,
        Prestige,
        VersionOutOfDate,
        PointTotalInvalid,
        SpecificPower
    }

    public class Agent : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly EventPointer<WakeStartEvent> _wakeStartEvent = new();
        private readonly EventPointer<WakeEndEvent> _wakeEndEvent = new();
        private readonly EventPointer<ExitCombatEvent> _exitCombatEvent = new();
        private readonly EventPointer<MovementStartedEvent> _movementStartedEvent = new();
        private readonly EventPointer<MovementStoppedEvent> _movementStoppedEvent = new();
        private readonly EventPointer<RespawnControlledAgentEvent> _respawnControlledAgentEvent = new();

        private TimeSpan _hitReactionCooldownEnd = TimeSpan.Zero;

        public AIController AIController { get; private set; }
        public AgentPrototype AgentPrototype { get => Prototype as AgentPrototype; }
        public override bool IsTeamUpAgent { get => AgentPrototype is AgentTeamUpPrototype; }
        public Avatar TeamUpOwner { get => Game.EntityManager.GetEntity<Avatar>(Properties[PropertyEnum.TeamUpOwnerId]); }
        public override int Throwability { get => Properties[PropertyEnum.Throwability]; }
        public bool IsVisibleWhenDormant { get => AgentPrototype.WakeStartsVisible; }
        public override bool IsWakingUp { get => _wakeEndEvent.IsValid; }
        public override bool IsDormant { get => base.IsDormant || IsWakingUp; }
        public virtual bool IsAtLevelCap { get => CharacterLevel >= GetTeamUpLevelCap(); }

        public override AOINetworkPolicyValues CompatibleReplicationChannels
        {
            // Make sure temporary controlled agents (e.g. Magik's Eternal Servitude) are always replicated to the client
            get => base.CompatibleReplicationChannels | (Properties.HasProperty(PropertyEnum.ControlledAgentHasSummonDur) ? AOINetworkPolicyValues.AOIChannelOwner : 0);
        }

        public Agent(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            AgentPrototype agentProto = GameDatabase.GetPrototype<AgentPrototype>(settings.EntityRef);
            if (agentProto == null) return Logger.WarnReturn(false, "Initialize(): agentProto == null");
            
            if (agentProto.Locomotion.Immobile == false)
                Locomotor = new();

            // GetPowerCollectionAllocateIfNull()
            base.Initialize(settings);

            // InitPowersCollection
            InitLocomotor(settings.LocomotorHeightOverride);

            // Wait in dormant while play start animation
            if (agentProto.WakeRange > 0.0f || agentProto.WakeDelayMS > 0) SetDormant(true);

            Properties[PropertyEnum.InitialCharacterLevel] = CharacterLevel;

            // When Gazillion implemented DCL, it looks like they made it switchable at first (based on Eval::runIsDynamicCombatLevelEnabled),
            // so all agents need to have their default non-DCL health base curves overriden with new DCL ones.
            if (CanBePlayerOwned() == false)
            {
                CurveId healthBaseCurveDcl = agentProto.MobHealthBaseCurveDCL;
                if (healthBaseCurveDcl == CurveId.Invalid) return Logger.WarnReturn(false, "Initialize(): healthBaseCurveDcl == CurveId.Invalid");

                PropertyId indexPropertyId = Properties.GetIndexPropertyIdForCurveProperty(PropertyEnum.HealthBase);
                if (indexPropertyId == PropertyId.Invalid) return Logger.WarnReturn(false, "Initialize(): curveIndexPropertyId == PropertyId.Invalid");

                PropertyInfo healthBasePropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.HealthBase);

                Properties.SetCurveProperty(PropertyEnum.HealthBase, healthBaseCurveDcl, indexPropertyId,
                    healthBasePropertyInfo, SetPropertyFlags.None, true);
            }
 
            return true;
        }

        public override bool ApplyInitialReplicationState(ref EntitySettings settings)
        {
            if (base.ApplyInitialReplicationState(ref settings) == false)
                return false;

            if (IsTeamUpAgent && settings.ArchiveData != null && settings.InventoryLocation != null)
            {
                Player player = Game.EntityManager.GetEntity<Player>(settings.InventoryLocation.ContainerId);
                if (player != null)
                    TryLevelUp(player, true);
            }

            return true;
        }

        #region World and Positioning

        public override bool CanRotate()
        {
            Player ownerPlayer = GetOwnerOfType<Player>();
            if (IsInKnockback || IsInKnockdown || IsInKnockup || IsImmobilized || IsImmobilizedByHitReact
                || IsSystemImmobilized || IsStunned || IsMesmerized || NPCAmbientLock
                || (ownerPlayer != null && ownerPlayer.IsFullscreenObscured))
                return false;
            return true;
        }

        public override bool CanMove()
        {
            Player ownerPlayer = GetOwnerOfType<Player>();
            if (base.CanMove() == false || HasMovementPreventionStatus || IsSystemImmobilized
                || (ownerPlayer != null && ownerPlayer.IsFullscreenObscured))
                return false;

            Power power = GetThrowablePower();
            if (power != null && power.PrototypeDataRef != ActivePowerRef)
                return false;

            return true;
        }

        private bool InitLocomotor(float height = 0.0f)
        {
            if (Locomotor != null)
            {
                AgentPrototype agentPrototype = AgentPrototype;
                if (agentPrototype == null) return false;

                Locomotor.Initialize(agentPrototype.Locomotion, this, height);
                Locomotor.SetGiveUpLimits(8.0f, TimeSpan.FromMilliseconds(250));
            }
            return true;
        }

        #endregion

        #region Powers

        public PowerUseResult ActivatePerformPower(PrototypeId powerRef)
        {
            if (this is Avatar) return PowerUseResult.GenericError;
            if (powerRef == PrototypeId.Invalid) return PowerUseResult.AbilityMissing;

            var powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerRef);
            if (powerProto == null) return PowerUseResult.GenericError;

            if (HasPowerInPowerCollection(powerRef) == false)
            {
                PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
                var power = AssignPower(powerRef, indexProps);
                if (power == null) return PowerUseResult.GenericError;
            }

            if (powerProto.Activation != PowerActivationType.Passive)
            {
                var power = GetPower(powerRef);
                if (power == null) return PowerUseResult.AbilityMissing;

                if (powerProto.IsToggled && power.IsToggledOn()) return PowerUseResult.Success;
                var result = CanActivatePower(power, InvalidId, Vector3.Zero);
                if (result != PowerUseResult.Success) return result;

                PowerActivationSettings powerSettings = new(Id, Vector3.Zero, RegionLocation.Position);
                powerSettings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
                return ActivatePower(powerRef, ref powerSettings);
            }

            return PowerUseResult.Success;
        }

        public void RemoveMissionActionReferencedPowers(PrototypeId missionRef)
        {
            if (missionRef == PrototypeId.Invalid) return;
            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
            if (missionProto == null) return;
            var referencedPowers = missionProto.MissionActionReferencedPowers;
            if (referencedPowers == null) return;
            foreach (var referencedPower in referencedPowers)
                UnassignPower(referencedPower);
        }

        protected override void ResurrectFromOther(WorldEntity ultimateOwner)
        {
            Properties[PropertyEnum.NoLootDrop] = true;
            Properties[PropertyEnum.NoExpOnDeath] = true;
            Resurrect();
        }

        public virtual bool Resurrect()
        {
            if (SummonDecremented) return false;

            // Cancel cleanup events
            CancelExitWorldEvent();
            CancelKillEvent();
            CancelDestroyEvent();

            // Reset properties
            Properties[PropertyEnum.Health] = Properties[PropertyEnum.HealthMaxOther];
            Properties[PropertyEnum.WeaponMissing] = false;
            Properties[PropertyEnum.NoEntityCollide] = false;

            TagPlayers.Clear();

            // Reset state
            PrototypeId stateRef = Properties[PropertyEnum.EntityState];
            if (stateRef != PrototypeId.Invalid)
            {
                var stateProto = GameDatabase.GetPrototype<EntityStatePrototype>(stateRef);
                if (stateProto == null || stateProto.AppearanceEnum != EntityAppearanceEnum.Dead) return false;

                if (WorldEntityPrototype.PostKilledState != null && WorldEntityPrototype.PostKilledState is StateSetPrototype setState)
                {
                    if (setState.State == stateRef)
                        SetState(PrototypeId.Invalid);
                }
            }

            // Send resurrection message
            var resurrectMessage = NetMessageOnResurrect.CreateBuilder()
                .SetTargetId(Id)
                .Build();

            Game.NetworkManager.SendMessageToInterested(resurrectMessage, this, AOINetworkPolicyValues.AOIChannelProximity);

            if (IsInWorld)
            {
                // Activate resurrection power
                if (AgentPrototype.OnResurrectedPower != PrototypeId.Invalid)
                {
                    PowerActivationSettings settings = new(Id, RegionLocation.Position, RegionLocation.Position);
                    settings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
                    ActivatePower(AgentPrototype.OnResurrectedPower, ref settings);
                }

                // Reactivate passive and toggled powers
                TryAutoActivatePowersInCollection();
            }

            if (CanBePlayerOwned() == false)
                AIController?.OnAIResurrect();

            // Resurrect event
            if (IsInWorld) Region?.EntityResurrectEvent.Invoke(new(this));

            return true;
        }

        public virtual bool HasPowerWithKeyword(PowerPrototype powerProto, PrototypeId keywordProtoRef)
        {
            KeywordPrototype keywordPrototype = GameDatabase.GetPrototype<KeywordPrototype>(keywordProtoRef);
            if (keywordPrototype == null) return Logger.WarnReturn(false, "HasPowerWithKeyword(): keywordPrototype == null");
            return powerProto.HasKeyword(keywordPrototype);
        }

        public IsInPositionForPowerResult IsInPositionForPower(Power power, WorldEntity target, Vector3 targetPosition)
        {
            var targetingProto = power.TargetingStylePrototype;
            if (targetingProto == null) return IsInPositionForPowerResult.Error;

            if (targetingProto.TargetingShape == TargetingShapeType.Self)
                return IsInPositionForPowerResult.Success;

            if (power.IsOnExtraActivation())
                return IsInPositionForPowerResult.Success;

            if (power.IsOwnerCenteredAOE() && (targetingProto.MovesToRangeOfPrimaryTarget == false || target == null))
                return IsInPositionForPowerResult.Success;

            Vector3 position = targetPosition;
            if (target != null && target.IsInWorld)
                if (power.Prototype is MissilePowerPrototype)
                {
                    float padding = target.Bounds.Radius - 1.0f;
                    Vector3 targetPos = target.RegionLocation.Position;
                    Vector3 targetDir = Vector3.SafeNormalize2D(RegionLocation.Position - targetPos);
                    position = targetPos + targetDir * padding;
                }

            if (IsInRangeToActivatePower(power, target, position) == false)
                return IsInPositionForPowerResult.OutOfRange;

            if (power.RequiresLineOfSight())
            {               
                Vector3? resultPosition = new();
                ulong targetId = (target != null ? target.Id : InvalidId);
                if (power.PowerLOSCheck(RegionLocation, position, targetId, ref resultPosition, power.LOSCheckAlongGround()) == false)
                    return IsInPositionForPowerResult.NoPowerLOS;
            }

            if (power.Prototype is SummonPowerPrototype summonPowerProto)
            {
                var summonedProto = summonPowerProto.GetSummonEntity(0, GetOriginalWorldAsset());
                if (summonedProto == null) return IsInPositionForPowerResult.Error;

                var summonContext = summonPowerProto.GetSummonEntityContext(0);
                if (summonContext == null) return IsInPositionForPowerResult.Error;

                var bounds = new Bounds(summonedProto.Bounds, position);

                var pathFlags = Region.GetPathFlagsForEntity(summonedProto);
                if (summonContext.PathFilterOverride != LocomotorMethod.None)
                    pathFlags = Locomotor.GetPathFlags(summonContext.PathFilterOverride);

                var region = Region;
                if (region == null) return IsInPositionForPowerResult.Error;
                if (summonContext.IgnoreBlockingOnSpawn == false && summonedProto.Bounds.CollisionType == BoundsCollisionType.Blocking)
                {
                    if (region.IsLocationClear(bounds, pathFlags, PositionCheckFlags.CanBeBlockedEntity) == false)
                        return IsInPositionForPowerResult.BadTargetPosition;
                }
                else if (pathFlags != 0)
                {
                    if (region.IsLocationClear(bounds, pathFlags, PositionCheckFlags.None) == false)
                        return IsInPositionForPowerResult.BadTargetPosition;
                }
            }

            return IsInPositionForPowerResult.Success;
        }

        public virtual PowerUseResult CanActivatePower(Power power, ulong targetId, Vector3 targetPosition,
            PowerActivationSettingsFlags flags = PowerActivationSettingsFlags.None, ulong itemSourceId = 0)
        {
            var powerRef = power.PrototypeDataRef;
            var powerProto = power.Prototype;
            if (powerProto == null)
            {
                Logger.Warn($"Unable to get the prototype for a power! Power: [{power}]");
                return PowerUseResult.AbilityMissing;
            }

            var targetingProto = powerProto.GetTargetingStyle();
            if (targetingProto == null)
            {
                Logger.Warn($"Unable to get the targeting prototype for a power! Power: [{power}]");
                return PowerUseResult.GenericError;
            }

            if (IsSimulated == false) return PowerUseResult.OwnerNotSimulated;
            if (GetPower(powerRef) == null) return PowerUseResult.AbilityMissing;

            if (targetingProto.TargetingShape == TargetingShapeType.Self)
            {
                targetId = Id;
            }
            else
            {
                if (IsInWorld == false)
                    return PowerUseResult.RestrictiveCondition;
            }

            var triggerResult = CanTriggerPower(powerProto, power, flags);
            if (triggerResult != PowerUseResult.Success)
                return triggerResult;

            if (power.IsExclusiveActivation())
            {
                if (IsExecutingPower)
                {
                    var activePower = GetPower(ActivePowerRef);
                    if (activePower == null)
                    {
                        Logger.Warn($"Agent has m_activePowerRef set, but is missing the power in its power collection! Power: [{GameDatabase.GetPrototypeName(ActivePowerRef)}] Agent: [{this}]");
                        return PowerUseResult.PowerInProgress;
                    }

                    if (activePower.IsTravelPower())
                    {
                        if (activePower.IsEnding == false)
                            activePower.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Interrupting);
                    }
                    else
                        return PowerUseResult.PowerInProgress;
                }

                if (Game == null) return PowerUseResult.GenericError;
                TimeSpan activateTime = Game.CurrentTime - power.LastActivateGameTime;
                TimeSpan animationTime = power.GetAnimationTime();
                if (activateTime < animationTime)
                    return PowerUseResult.MinimumReactivateTime;
            }

            WorldEntity target = null;
            if (targetId != InvalidId)
                target = Game.EntityManager.GetEntity<WorldEntity>(targetId);

            if (power.IsItemPower())
            {
                if (itemSourceId == InvalidId)
                {
                    Logger.Warn($"Power is an ItemPower but no itemSourceId specified - {power}");
                    return PowerUseResult.ItemUseRestricted;
                }

                var item = Game.EntityManager.GetEntity<Item>(itemSourceId);
                if (item == null) return PowerUseResult.ItemUseRestricted;

                var powerUse = flags.HasFlag(PowerActivationSettingsFlags.AutoActivate) == false;
                if (powerRef == item.OnUsePower && item.CanUse(this, powerUse) == false)
                    return PowerUseResult.ItemUseRestricted;
            }

            var result = IsInPositionForPower(power, target, targetPosition);
            if (result == IsInPositionForPowerResult.OutOfRange || result == IsInPositionForPowerResult.NoPowerLOS)
                return PowerUseResult.OutOfPosition;
            else if (result == IsInPositionForPowerResult.BadTargetPosition)
                return PowerUseResult.BadTarget;

            return power.CanActivate(target, targetPosition, flags);
        }

        public override PowerUseResult CanTriggerPower(PowerPrototype powerProto, Power power, PowerActivationSettingsFlags flags)
        {
            // Agent-specific validation
            if (powerProto.Activation != PowerActivationType.Passive &&
                Power.IsProcEffect(powerProto) == false &&
                Power.IsComboEffect(powerProto) == false)
            {
                // Check if in world (NOTE: This is validated in a separate method called CanExecutePowers() in the client)
                if (IsInWorld == false)
                    return PowerUseResult.RestrictiveCondition;

                // Check for power-specific locks
                if (Properties[PropertyEnum.SinglePowerLock, powerProto.DataRef])
                    return PowerUseResult.RestrictiveCondition;

                // Check for status effects that would prevent using this power
                if (powerProto.Properties == null)
                    return Logger.WarnReturn(PowerUseResult.GenericError, "CanTriggerPower(): powerProto.Properties == null");

                if ((HasPowerPreventionStatus() || HasAIControlPowerLock) &&
                    powerProto.Properties[PropertyEnum.NegStatusUsable] == false &&
                    powerProto.PowerCategory != PowerCategoryType.ThrowablePower &&
                    powerProto.PowerCategory != PowerCategoryType.ThrowableCancelPower)
                {
                    return PowerUseResult.RestrictiveCondition;
                }

                // Check for tutorial locks
                if (IsInTutorialPowerLock && powerProto.PowerCategory != PowerCategoryType.GameFunctionPower)
                    return PowerUseResult.RestrictiveCondition;

                // Check for keyword locks
                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowerLockForPowerKeyword))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);

                    if (HasPowerWithKeyword(powerProto, keywordProtoRef))
                        return PowerUseResult.RestrictiveCondition;
                }
            }

            PowerUseResult result = base.CanTriggerPower(powerProto, power, flags);
            if (result != PowerUseResult.Success)
                return result;

            // Do not allow user input to activate powers during fullscreen movies
            if (flags.HasFlag(PowerActivationSettingsFlags.Item) == false &&
                powerProto.Activation != PowerActivationType.Passive &&
                powerProto.PowerCategory != PowerCategoryType.ComboEffect &&
                powerProto.PowerCategory != PowerCategoryType.ProcEffect &&
                (powerProto.IsToggled && flags.HasFlag(PowerActivationSettingsFlags.AutoActivate)) == false)
            {
                Player player = GetOwnerOfType<Player>();
                if (player != null && player.IsFullscreenObscured)
                    return PowerUseResult.FullscreenMovie;
            }

            return PowerUseResult.Success;
        }

        public bool HasPowerPreventionStatus()
        {
            return IsInKnockback
            || IsInKnockdown
            || IsInKnockup
            || IsStunned
            || IsMesmerized
            || NPCAmbientLock
            || IsInPowerLock;
        }

        public override TimeSpan GetAbilityCooldownTimeRemaining(PowerPrototype powerProto)
        {
            if (AIController != null && powerProto.PowerCategory == PowerCategoryType.NormalPower)
            {
                Game game = Game;
                if (game == null) return Logger.WarnReturn(TimeSpan.Zero, "GetAbilityCooldownTimeRemaining(): game == null");

                PropertyCollection blackboardProperties = AIController.Blackboard.PropertyCollection;
                long aiCooldownTime = blackboardProperties[PropertyEnum.AIProceduralPowerSpecificCDTime, powerProto.DataRef];
                return TimeSpan.FromMilliseconds(aiCooldownTime) - game.CurrentTime;
            }

            return base.GetAbilityCooldownTimeRemaining(powerProto);
        }

        public override TimeSpan GetPowerInterruptCooldown(PowerPrototype powerProto)
        {
            TimeSpan interruptCooldownMax = TimeSpan.Zero;

            // Check interrupt cooldowns for triggered powers
            if (powerProto.ActionsTriggeredOnPowerEvent.HasValue())
            {
                foreach (PowerEventActionPrototype triggeredPowerEvent in powerProto.ActionsTriggeredOnPowerEvent)
                {
                    if (triggeredPowerEvent.EventAction != PowerEventActionType.UsePower)
                        continue;

                    switch (triggeredPowerEvent.PowerEvent)
                    {
                        case PowerEventType.OnContactTime:
                        case PowerEventType.OnPowerApply:
                        case PowerEventType.OnPowerEnd:
                        case PowerEventType.OnPowerStart:
                            if (triggeredPowerEvent.Power == powerProto.DataRef)
                            {
                                Logger.Warn($"GetPowerInterruptCooldown(): Infinite power loop detected in {powerProto}!");
                                continue;
                            }

                            PowerPrototype triggeredPowerProto = triggeredPowerEvent.Power.As<PowerPrototype>();
                            if (triggeredPowerProto == null)
                            {
                                Logger.Warn("GetPowerInterruptCooldown(): triggeredPowerProto == null");
                                continue;
                            }

                            interruptCooldownMax = Clock.Max(interruptCooldownMax, GetPowerInterruptCooldown(triggeredPowerProto));
                            break;
                    }
                }
            }

            // Check interrupt cooldown for the power itself
            Power power = GetPower(powerProto.DataRef);
            if (power != null && power.WasLastActivateInterrupted)
            {
                AgentPrototype agentProto = AgentPrototype;
                if (agentProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetPowerInterruptCooldown(): agentProto == null");

                BehaviorProfilePrototype behaviorProfile = agentProto.BehaviorProfile;
                if (behaviorProfile != null)
                    interruptCooldownMax = Clock.Max(interruptCooldownMax, TimeSpan.FromMilliseconds(behaviorProfile.InterruptCooldownMS));
            }

            return interruptCooldownMax;
        }

        public bool StartThrowing(ulong entityId)
        {
            if (Properties[PropertyEnum.ThrowableOriginatorEntity] == entityId) return true;

            // Validate entity
            var throwableEntity = Game.EntityManager.GetEntity<WorldEntity>(entityId);
            if (throwableEntity == null || throwableEntity.IsAliveInWorld == false)
            {
                // Cancel pending throw action on the client set in CAvatar::StartThrowing()
                // NOTE: AvatarIndex can be hardcoded to 0 because we don't have couch coop (yet?)
                if (this is Avatar)
                {
                    var player = GetOwnerOfType<Player>();
                    player.SendMessage(NetMessageCancelPendingActionToClient.CreateBuilder().SetAvatarIndex(0).Build());
                }

                return Logger.WarnReturn(false, "StartThrowing(): Invalid throwable entity");
            }

            // Make sure we are not throwing something already
            Power throwablePower = GetThrowablePower();
            if (throwablePower != null)
                UnassignPower(throwablePower.PrototypeDataRef);

            Power throwableCancelPower = GetThrowableCancelPower();
            if (throwableCancelPower != null)
                UnassignPower(throwableCancelPower.PrototypeDataRef);

            // Do avatar-specific validation for the entity we are about to throw
            if (CanThrow(throwableEntity) == false)
                return false;

            // Record throwable entity in agent's properties
            Properties[PropertyEnum.ThrowableOriginatorEntity] = entityId;
            Properties[PropertyEnum.ThrowableOriginatorAssetRef] = throwableEntity.GetEntityWorldAsset();

            // Assign throwable powers
            PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
            PrototypeId throwableCancelPowerRef = throwableEntity.Properties[PropertyEnum.ThrowableRestorePower];
            AssignPower(throwableCancelPowerRef, indexProps);
            PrototypeId throwablePowerRef = throwableEntity.Properties[PropertyEnum.ThrowablePower];
            AssignPower(throwablePowerRef, indexProps);

            // Invoke event if needed
            if (this is Avatar && throwableEntity.IsInWorld)
            {
                Player player = GetOwnerOfType<Player>();
                throwableEntity.Region.ThrowablePickedUpEvent.Invoke(new(player, throwableEntity));
            }

            // Remove the entity we are throwing from the world
            throwableEntity.ExitWorld();
            throwableEntity.ConditionCollection?.RemoveAllConditions(true);

            // start throwing from AI
            AIController?.OnAIStartThrowing(throwableEntity, throwablePowerRef, throwableCancelPowerRef);

            return true;
        }

        public bool TryRestoreThrowable()
        {
            // Return throwable entity to the world if throwing was cancelled
            ulong throwableEntityId = Properties[PropertyEnum.ThrowableOriginatorEntity];
            if (IsInWorld && throwableEntityId != 0)
            {
                var throwableEntity = Game.EntityManager.GetEntity<WorldEntity>(throwableEntityId);
                if (throwableEntity != null && throwableEntity.IsInWorld == false)
                {
                    Region region = Game.RegionManager.GetRegion(throwableEntity.ExitWorldRegionLocation.RegionId);

                    if (region != null)
                    {
                        Vector3 exitPosition = throwableEntity.ExitWorldRegionLocation.Position;
                        Orientation exitOrientation = throwableEntity.ExitWorldRegionLocation.Orientation;
                        throwableEntity.EnterWorld(region, exitPosition, exitOrientation);
                    }
                    else
                    {
                        throwableEntity.Destroy();
                    }
                }
            }

            // Clean up throwable entity data
            Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorEntity);
            Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorAssetRef);

            return true;
        }

        protected virtual bool CanThrow(WorldEntity throwableEntity)
        {
            // Overriden in Avatar
            return true;
        }

        protected override PowerUseResult ActivatePower(Power power, ref PowerActivationSettings settings)
        {
            PowerUseResult result = base.ActivatePower(power, ref settings);

            if (result == PowerUseResult.Success)
            {
                // Set power as active
                if (power.IsExclusiveActivation())
                {
                    if (IsInWorld)
                        ActivePowerRef = power.PrototypeDataRef;
                    else
                        Logger.Warn($"ActivatePower(): Trying to set the active power for an Agent that is not in the world. " +
                            $"Check to see if there's *anything* that can happen in the course of executing the power that can take them out of the world.\n Agent: {this}");
                }

                // Try to activate OnPowerUse procs
                if (settings.Flags.HasFlag(PowerActivationSettingsFlags.NoOnPowerUseProcs) == false)
                {
                    switch (power.GetPowerCategory())
                    {
                        case PowerCategoryType.ComboEffect:
                            TryActivateOnPowerUseProcs(ProcTriggerType.OnPowerUseComboEffect, power, ref settings);
                            break;

                        case PowerCategoryType.ItemPower:
                            TryActivateOnPowerUseProcs(ProcTriggerType.OnPowerUseConsumable, power, ref settings);
                            break;

                        case PowerCategoryType.GameFunctionPower:
                            TryActivateOnPowerUseProcs(ProcTriggerType.OnPowerUseGameFunction, power, ref settings);
                            break;

                        case PowerCategoryType.NormalPower:
                            TryActivateOnPowerUseProcs(ProcTriggerType.OnPowerUseNormal, power, ref settings);
                            break;
                    }
                }
            }
            else
            {
                // Extra activation failing is valid
                if (result != PowerUseResult.ExtraActivationFailed)
                {
                    Logger.Warn($"ActivatePower(): Power [{power}] for entity [{this}] failed to properly activate. Result = {result}");
                    ActivePowerRef = PrototypeId.Invalid;
                }

                // Recover from throwing if failed to throw for whatever reason
                if (power == GetThrowablePower())
                    UnassignPower(power.PrototypeDataRef);
            }

            return result;
        }

        private static bool IsInRangeToActivatePower(Power power, WorldEntity target, Vector3 position)
        {
            if (target != null && power.AlwaysTargetsMousePosition() == false)
            {
                if (target.IsInWorld == false) return false;
                return power.IsInRange(target, RangeCheckType.Activation);
            }
            else if (power.IsMelee())
                return true;

            return power.IsInRange(position, RangeCheckType.Activation);
        }

        protected void RefreshDependentPassivePowers(PowerPrototype powerProto, int rank)
        {
            if (powerProto.RefreshDependentPassivePowers.IsNullOrEmpty())
                return;

            foreach (PrototypeId powerProtoRef in powerProto.RefreshDependentPassivePowers)
            {
                Power power = GetPower(powerProtoRef);
                if (power == null)
                    continue;

                power.Rank = rank;
                power.ScheduleIndexPropertiesReapplication(PowerIndexPropertyFlags.PowerRank);
            }
        }

        /// <summary>
        /// Activates passive powers and toggled powers that were previous on.
        /// </summary>
        private void TryAutoActivatePowersInCollection()
        {
            if (PowerCollection == null)
                return;

            // Need to use a temporary list here because activating a power can add a condition that will assign a proc power
            List<Power> powerList = ListPool<Power>.Instance.Get();

            foreach (var kvp in PowerCollection)
                powerList.Add(kvp.Value.Power);

            foreach (Power power in powerList)
                TryAutoActivatePower(power);

            ListPool<Power>.Instance.Return(powerList);
        }

        /// <summary>
        /// Activates the provided power if it's a passive power or a toggle power that was previosuly toggled on.
        /// </summary>
        private bool TryAutoActivatePower(Power power)
        {
            if (IsInWorld == false || IsSimulated == false || IsDead)
                return false;

            PowerPrototype powerProto = power?.Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TryAutoActivatePower(): powerProto == null");

            bool wasToggled = false;
            bool shouldActivate = false;

            if (power.IsToggledOn() || power.IsToggleInPrevRegion())
            {
                wasToggled = true;

                Properties[PropertyEnum.PowerToggleOn, power.PrototypeDataRef] = false;
                Properties[PropertyEnum.PowerToggleInPrevRegion, power.PrototypeDataRef] = false;

                shouldActivate = powerProto.PowerCategory != PowerCategoryType.ProcEffect;
            }

            shouldActivate |= power.GetActivationType() == PowerActivationType.Passive;

            if (shouldActivate == false)
                return false;

            TargetingStylePrototype targetingStyleProto = powerProto.GetTargetingStyle();
            ulong targetId = targetingStyleProto.TargetingShape == TargetingShapeType.Self ? Id : InvalidId;
            Vector3 position = RegionLocation.Position;

            PowerActivationSettings settings = new(targetId, position, position);
            settings.Flags |= PowerActivationSettingsFlags.NoOnPowerUseProcs | PowerActivationSettingsFlags.AutoActivate;

            // Extra settings for combo/item powers
            if (power.IsComboEffect())
            {
                settings.TriggeringPowerRef = power.Properties[PropertyEnum.TriggeringPowerRef, power.PrototypeDataRef];
            }
            else if (power.IsItemPower() && this is Avatar avatar)
            {
                settings.ItemSourceId = avatar.FindOwnedItemThatGrantsPower(power.PrototypeDataRef);
                if (settings.ItemSourceId == InvalidId)
                    return Logger.WarnReturn(false, "TryAutoActivatePower(): settings.ItemSourceId == InvalidId");
            }

            PowerUseResult result = CanActivatePower(power, settings.TargetEntityId, settings.TargetPosition, settings.Flags, settings.ItemSourceId);
            if (result == PowerUseResult.Success)
            {
                result = ActivatePower(power, ref settings);
                if (result != PowerUseResult.Success)
                    Logger.Warn($"TryAutoActivatePower(): Failed to auto-activate power [{powerProto}] for [{this}] for reason [{result}]");
            }
            else if (result == PowerUseResult.RegionRestricted && wasToggled)
            {
                Properties[PropertyEnum.PowerToggleInPrevRegion, power.PrototypeDataRef] = true;
            }

            return result == PowerUseResult.Success;
        }

        #endregion

        #region Power Ranks

        public int GetPowerRank(PrototypeId powerProtoRef)
        {
            if (powerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(0, "GetPowerRank(): powerProtoRef == PrototypeId.Invalid");
            return Properties[PropertyEnum.PowerRankCurrentBest, powerProtoRef];
        }

        public int GetPowerRankBase(PrototypeId powerProtoRef)
        {
            if (powerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(0, "GetPowerRank(): powerProtoRef == PrototypeId.Invalid");
            return Properties[PropertyEnum.PowerRankBase, powerProtoRef];
        }

        public int ComputePowerRank(ref PowerProgressionInfo powerInfo, int specIndex, out int rankBase)
        {
            rankBase = PowerProgressionInfo.RankLocked;

            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(0, "ComputePowerRank(): this is not Avatar && IsTeamUpAgent == false");

            PowerPrototype powerProto = powerInfo.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(0, "ComputePowerRank(): powerProto == null");

            rankBase = ComputePowerRankBase(ref powerInfo, specIndex);

            int rankCurrentBest = Math.Max(0, rankBase);

            if (powerInfo.IsInPowerProgression == false && powerProto.PowerCategory == PowerCategoryType.NormalPower && powerProto.UsableByAll == false)
                return rankCurrentBest;

            if (powerInfo.IsInPowerProgression == false || powerInfo.CanBeRankedUp() == false)
                return rankCurrentBest;

            PrototypeId powerTabRef = powerInfo.PowerTabRef;
            PrototypeId[] antireqs = powerInfo.AntirequisitePowerRefs;

            // +rank works only for powers that are already at rank 1 or above
            int powerBoost = 0;
            bool canBeBoosted = rankBase > 0 && Properties.HasProperty(PropertyEnum.PowerBoost);

            // Ultimates and powers with antireqs cannot be granted
            int powerGrantRank = 0;
            bool canBeGranted = powerProto.IsUltimate == false && antireqs.IsNullOrEmpty() && Properties.HasProperty(PropertyEnum.PowerGrantRank);

            // Get boosts (+rank)
            if (canBeBoosted)
            {
                powerBoost += Properties[PropertyEnum.PowerBoost, powerInfo.PowerRef];

                // Ultimates are not affected by +all and +tab boosts
                if (powerProto.IsUltimate == false)
                {
                    powerBoost += Properties[PropertyEnum.PowerBoost, PrototypeId.Invalid];

                    if (powerTabRef != PrototypeId.Invalid)
                        powerBoost += Properties[PropertyEnum.PowerBoost, powerTabRef, PrototypeDataRef];
                }
            }

            // Get grant
            if (canBeGranted)
            {
                powerGrantRank = Properties[PropertyEnum.PowerGrantRank, powerInfo.PowerRef];

                powerGrantRank = Math.Max(powerGrantRank, Properties[PropertyEnum.PowerGrantRank, PrototypeId.Invalid]);

                if (powerTabRef != PrototypeId.Invalid)
                    powerGrantRank = Math.Max(powerGrantRank, Properties[PropertyEnum.PowerGrantRank, powerTabRef, PrototypeDataRef]);
            }

            // Keyword bonuses
            if (canBeBoosted || canBeGranted)
            {
                PrototypeId[] keywords = powerProto.Keywords;

                // Check for keyword overrides from mapped power
                PrototypeId mappedPowerRef = powerInfo.MappedPowerRef;
                if (mappedPowerRef != PrototypeId.Invalid)
                {
                    PowerPrototype mappedPowerProto = mappedPowerRef.As<PowerPrototype>();
                    if (mappedPowerProto == null) return Logger.WarnReturn(0, "ComputePowerRank(): mappedPowerProto == null");

                    keywords = mappedPowerProto.Keywords;
                }

                if (keywords.HasValue())
                {
                    PrototypeId ultimatePowerKeyword = GameDatabase.KeywordGlobalsPrototype.UltimatePowerKeyword;

                    foreach (PrototypeId keywordProtoRef in keywords)
                    {
                        if (canBeBoosted && (powerProto.IsUltimate == false || keywordProtoRef == ultimatePowerKeyword))
                            powerBoost += Properties[PropertyEnum.PowerBoost, keywordProtoRef];

                        if (canBeGranted)
                            powerGrantRank = Math.Max(powerGrantRank, Properties[PropertyEnum.PowerGrantRank, keywordProtoRef]);
                    }
                }
            }

            // Cap power boost (+30 for a total of rank 50)
            powerBoost = Math.Min(GameDatabase.AdvancementGlobalsPrototype.PowerBoostMax, powerBoost);

            // Return final best rank
            rankCurrentBest = Math.Max(rankCurrentBest + powerBoost, powerGrantRank);
            return rankCurrentBest;
        }

        protected virtual int ComputePowerRankBase(ref PowerProgressionInfo powerInfo, int specIndex)
        {
            int rankBase = PowerProgressionInfo.RankLocked;

            // Do not apply bonuses to non-progression powers
            if (powerInfo.IsInPowerProgression == false)
                return GetPowerRankBase(powerInfo.PowerRef);

            if (powerInfo.IsUltimatePower)
                rankBase = Properties[PropertyEnum.AvatarPowerUltimatePoints];
            else if (CharacterLevel >= powerInfo.GetRequiredLevel())
                rankBase = powerInfo.GetStartingRank();

            int rankMax = GetMaxPossibleRankForPowerAtCurrentLevel(ref powerInfo, specIndex);

            if (Properties[PropertyEnum.PowersUnlockAll])
                return Math.Max(1, rankMax);

            rankBase += powerInfo.GetStartingRank();
            return Math.Min(rankBase, rankMax);
        }

        public int GetMaxPossibleRankForPowerAtCurrentLevel(ref PowerProgressionInfo powerInfo, int specIndex)
        {
            return GetMaxPossibleRankForPowerAtLevel(ref powerInfo, specIndex, CharacterLevel, out _, out _);
        }

        public int GetMaxPossibleRankForPowerAtLevel(ref PowerProgressionInfo powerInfo, int specIndex, int level, out bool filteredByPrereq, out bool filteredByAntireq)
        {
            filteredByPrereq = false;
            filteredByAntireq = false;

            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(0, "GetMaxPossibleRankForPowerAtLevel(): this is not Avatar && IsTeamUpAgent == false");

            PowerPrototype powerProto = powerInfo.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(0, "GetMaxPossibleRankForPowerAtLevel(): powerProto == null");

            if (powerInfo.IsInPowerProgression == false)
            {
                if (powerProto.UsableByAll == false)
                    return PowerProgressionInfo.RankLocked;

                return GetPowerRankBase(powerInfo.PowerRef);
            }

            if (powerInfo.GetRequiredLevel() > level)
                return PowerProgressionInfo.RankLocked;

            if (Properties[PropertyEnum.PowersUnlockAll] == false)
            {
                // Check prerequisites
                PrototypeId[] prereqs = powerInfo.PrerequisitePowerRefs;
                if (prereqs.HasValue())
                {
                    foreach (PrototypeId prereqProtoRef in prereqs)
                    {
                        if (GetPowerProgressionInfo(prereqProtoRef, out PowerProgressionInfo preReqPowerInfo) == false)
                            return Logger.WarnReturn(0, "GetMaxPossibleRankForPowerAtLevel(): GetPowerProgressionInfo(prereqProtoRef, out PowerProgressionInfo preReqPowerInfo) == false");

                        if (preReqPowerInfo.GetRequiredLevel() > level)
                        {
                            filteredByPrereq = true;
                            return 0;
                        }
                    }
                }

                // Check antirequisites
                PrototypeId[] antireqs = powerInfo.AntirequisitePowerRefs;
                if (antireqs.HasValue())
                {
                    foreach (PrototypeId antireqProtoRef in antireqs)
                    {
                        if (GetPowerProgressionInfo(antireqProtoRef, out PowerProgressionInfo antiReqPowerInfo) == false)
                            return Logger.WarnReturn(0, "GetMaxPossibleRankForPowerAtLevel(): GetPowerProgressionInfo(antireqProtoRef, out PowerProgressionInfo antiReqPowerInfo) == false");

                        // Shouldn't this be <=?
                        if (antiReqPowerInfo.GetRequiredLevel() < level)
                        {
                            filteredByAntireq = true;
                            return 0;
                        }
                    }
                }
            }

            if (powerInfo.CanBeRankedUp() == false)
                return powerInfo.GetStartingRank();

            Curve maxRankAtCharLevelCurve = powerInfo.GetMaxRankCurve();
            if (maxRankAtCharLevelCurve == null) return Logger.WarnReturn(0, "GetMaxPossibleRankForPowerAtLevel(): maxRankAtCharLevelCurve == null");

            return maxRankAtCharLevelCurve.GetIntAt(level);
        }

        protected virtual bool UpdatePowerRank(ref PowerProgressionInfo powerInfo, bool forceUnassign)
        {
            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(false, "UpdatePowerRank(): this is not Avatar && IsTeamUpAgent == false");

            PowerPrototype powerProto = powerInfo.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "UpdatePowerRank(): powerProto == null");

            if (powerInfo.IsTalent) return Logger.WarnReturn(false, "UpdatePowerRank(): powerInfo.IsTalent");

            // Check if this is a team-up passive that applies to the owner avatar
            Agent powerOwner = this;
            bool computeRank = true;

            if (IsTeamUpAgent && powerProto.Activation == PowerActivationType.Passive)
            {
                // We may not have an owner yet early in the initialization process
                Avatar teamUpOwner = TeamUpOwner;
                if (teamUpOwner == null)
                    return false;

                // Make sure this team-up is selected
                computeRank &= teamUpOwner.CurrentTeamUpAgent == this;

                bool isPassivePowerOnAvatarWhileAway = powerInfo.IsPassivePowerOnAvatarWhileAway;
                bool isPassivePowerOnAvatarWhileSummoned = powerInfo.IsPassivePowerOnAvatarWhileSummoned;

                if (isPassivePowerOnAvatarWhileAway || isPassivePowerOnAvatarWhileSummoned)
                {
                    // Override power owner and check if team-up status (away or summoned) matches what the passive requires
                    powerOwner = teamUpOwner;

                    if (IsAliveInWorld && TestStatus(EntityStatus.ExitingWorld) == false)
                        computeRank &= isPassivePowerOnAvatarWhileSummoned;
                    else
                        computeRank &= isPassivePowerOnAvatarWhileAway;
                }
;
            }

            if (powerOwner == null) return Logger.WarnReturn(false, "UpdatePowerRank(): powerOwner == null");

            // No need to assign/unassign powers if the owner is not in the world
            if (powerOwner.IsInWorld == false || powerOwner.TestStatus(EntityStatus.ExitingWorld))
                return false;

            int rankBase = -1;
            int rankCurrentBest = 0;

            if (computeRank)
                rankCurrentBest = ComputePowerRank(ref powerInfo, GetPowerSpecIndexActive(), out rankBase);

            // Do the actual rank update
            return powerOwner.DoPowerRankUpdate(ref powerInfo, forceUnassign, rankBase, rankCurrentBest);
        }

        private bool DoPowerRankUpdate(ref PowerProgressionInfo powerInfo, bool forceUnassign, int rankBase, int rankCurrentBest)
        {
            PowerPrototype powerProto = powerInfo.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "UpdatePowerRank(): powerProto == null");

            PrototypeId powerProtoRef = powerProto.DataRef;

            int rankOldBest = GetPowerRank(powerProtoRef);
            Properties[PropertyEnum.PowerRankBase, powerProtoRef] = rankBase;

            // Unassign if needed
            bool reassign = false;
            if (forceUnassign)
            {
                bool unassigned = UnassignPower(powerProtoRef);
                RefreshDependentPassivePowers(powerProto, 0);

                if (unassigned && rankCurrentBest > 0)
                    reassign = true;
            }

            // Turn off toggle powers if the new rank is 0
            if (rankCurrentBest == 0)
            {
                Properties.RemoveProperty(new(PropertyEnum.PowerToggleOn, powerProtoRef));
                Properties.RemoveProperty(new(PropertyEnum.PowerToggleInPrevRegion, powerProtoRef));
            }

            // Early exit if nothing more to do
            if (reassign == false && rankCurrentBest == rankOldBest)
                return false;

            Properties[PropertyEnum.PowerRankCurrentBest, powerProtoRef] = rankCurrentBest;

            Power power = GetPower(powerProtoRef);
            if (rankCurrentBest > 0)
            {
                // We are gaining or refreshing a power
                if (power != null)
                {
                    // We are refreshing an existing power
                    power.Rank = rankCurrentBest;
                    power.ScheduleIndexPropertiesReapplication(PowerIndexPropertyFlags.PowerRank);
                }
                else
                {
                    // We are gaining a new power
                    PowerIndexProperties indexProps = new(rankCurrentBest, CharacterLevel, CombatLevel);
                    AssignPower(powerProtoRef, indexProps);
                }
            }
            else
            {
                // We are losing the power
                if (power != null)
                    UnassignPower(powerProtoRef);
            }

            RefreshDependentPassivePowers(powerProto, rankCurrentBest);
            return true;
        }

        private bool UpdatePowerBoost(PrototypeId boostParamProtoRef)
        {
            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(false, "UpdatePowerBoost(): this is not Avatar && IsTeamUpAgent == false");

            Prototype boostParamProto = boostParamProtoRef.As<Prototype>();

            bool isBoostToAll = boostParamProtoRef == PrototypeId.Invalid;
            bool isBoostToTab = boostParamProto is PowerProgTableTabRefPrototype;
            bool isBoostToKeyword = boostParamProto is KeywordPrototype;

            if (isBoostToAll == false && isBoostToTab == false && isBoostToKeyword == false)
            {
                // This is a boost to a specific power
                GetPowerProgressionInfo(boostParamProtoRef, out PowerProgressionInfo powerInfo);

                // Non-power progression powers and talents are not affected by boosts
                if (powerInfo.IsInPowerProgression && powerInfo.IsTalent == false)
                    UpdatePowerRank(ref powerInfo, false);

                return true;
            }

            // This is a boost to multiple powers
            List<PowerProgressionInfo> powerInfoList = ListPool<PowerProgressionInfo>.Instance.Get();
            GetPowerProgressionInfos(powerInfoList);

            for (int i = 0; i < powerInfoList.Count; i++)
            {
                PowerProgressionInfo powerInfo = powerInfoList[i];

                PowerPrototype powerProto = powerInfo.PowerPrototype;
                if (powerProto == null)
                {
                    Logger.Warn("UpdatePowerBoost(): powerProto == null");
                    continue;
                }

                // Ultimates are not affected by boosts to all/tab
                if (powerInfo.IsUltimatePower && (isBoostToAll || isBoostToTab))
                    continue;

                // Check tab
                if (isBoostToTab && powerInfo.PowerTabRef != boostParamProtoRef)
                    continue;

                // Check keywords
                if (isBoostToKeyword)
                {
                    PowerPrototype keywordSourceProto = powerProto;

                    // Check for mapped power overrides
                    PrototypeId mappedPowerRef = powerInfo.MappedPowerRef;
                    if (mappedPowerRef != PrototypeId.Invalid)
                    {
                        keywordSourceProto = mappedPowerRef.As<PowerPrototype>();
                        if (keywordSourceProto == null)
                        {
                            Logger.Warn("UpdatePowerBoost(): keywordSourceProto == null");
                            continue;
                        }
                    }

                    if (HasPowerWithKeyword(keywordSourceProto, boostParamProtoRef) == false)
                        continue;
                }

                // All checks are okay, do the update
                UpdatePowerRank(ref powerInfo, false);
            }

            ListPool<PowerProgressionInfo>.Instance.Return(powerInfoList);
            return true;
        }

        private bool UpdatePowerGrant(PrototypeId grantParamProtoRef)
        {
            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(false, "UpdatePowerGrant(): this is not Avatar && IsTeamUpAgent == false");

            // This probably shouldn't be happening in 1.52
            Logger.Debug($"UpdatePowerGrant(): {grantParamProtoRef.GetName()} for [{this}]");

            Prototype grantParamProto = grantParamProtoRef.As<Prototype>();

            bool isGrantAll = grantParamProtoRef == PrototypeId.Invalid;
            bool isGrantTab = grantParamProto is PowerProgTableTabRefPrototype;
            bool isGrantKeyword = grantParamProto is KeywordPrototype;

            if (isGrantAll == false && isGrantTab == false && isGrantKeyword == false)
            {
                // This is a grant of a specific power
                GetPowerProgressionInfo(grantParamProtoRef, out PowerProgressionInfo powerInfo);

                DoPowerGrantUpdate(ref powerInfo);
                return true;
            }

            // This is a grant of multiple powers
            List<PowerProgressionInfo> powerInfoList = ListPool<PowerProgressionInfo>.Instance.Get();
            GetPowerProgressionInfos(powerInfoList);

            for (int i = 0; i < powerInfoList.Count; i++)
            {
                PowerProgressionInfo powerInfo = powerInfoList[i];

                PowerPrototype powerProto = powerInfo.PowerPrototype;
                if (powerProto == null)
                {
                    Logger.Warn("UpdatePowerGrant(): powerProto == null");
                    continue;
                }

                // Skip powers that cannot be grants
                if (powerInfo.IsUltimatePower)
                    continue;

                if (powerProto.IsTravelPower)
                    continue;

                if (powerInfo.AntirequisitePowerRefs.HasValue())
                    continue;

                // Check tab
                if (isGrantTab && powerInfo.PowerTabRef != grantParamProtoRef)
                    continue;

                // Check keywords
                if (isGrantKeyword && HasPowerWithKeyword(powerProto, grantParamProtoRef) == false)
                    continue;

                // All checks are okay, do the update
                DoPowerGrantUpdate(ref powerInfo);
            }

            ListPool<PowerProgressionInfo>.Instance.Return(powerInfoList);
            return true;
        }

        private bool DoPowerGrantUpdate(ref PowerProgressionInfo powerInfo)
        {
            PowerPrototype powerProto = powerInfo.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "DoPowerGrantUpdate(): powerProto == null");

            int rankBefore = GetPowerRank(powerInfo.PowerRef);
            
            if (UpdatePowerRank(ref powerInfo, false) == false)
                return false;

            // Show HUD tutorial for power-granting items if needed
            if (rankBefore <= 0 && powerProto.Activation != PowerActivationType.Passive)
            {
                Player player = GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, "DoPowerGrantUpdate(): player == null");

                player.ShowHUDTutorial(GameDatabase.UIGlobalsPrototype.PowerGrantItemTutorialTip.As<HUDTutorialPrototype>());
            }

            return true;
        }

        #endregion

        #region Power Progression

        public virtual int GetLatestPowerProgressionVersion()
        {
            if (IsTeamUpAgent == false) return 0;
            if (Prototype is not AgentTeamUpPrototype teamUpProto) return 0;
            return teamUpProto.PowerProgressionVersion;
        }

        public virtual bool HasPowerInPowerProgression(PrototypeId powerRef)
        {
            if (IsTeamUpAgent)
                return GameDataTables.Instance.PowerOwnerTable.GetTeamUpPowerProgressionEntry(PrototypeDataRef, powerRef) != null;

            return false;
        }

        public virtual bool GetPowerProgressionInfo(PrototypeId powerProtoRef, out PowerProgressionInfo powerInfo)
        {
            // Note: this implementation is meant only for team-up agents

            powerInfo = new();

            if (powerProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): powerProtoRef == PrototypeId.Invalid");

            AgentTeamUpPrototype teamUpProto = PrototypeDataRef.As<AgentTeamUpPrototype>();
            if (teamUpProto == null)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): teamUpProto == null");

            TeamUpPowerProgressionEntryPrototype powerProgEntry = GameDataTables.Instance.PowerOwnerTable.GetTeamUpPowerProgressionEntry(teamUpProto.DataRef, powerProtoRef);
            if (powerProgEntry != null)
                powerInfo.InitForTeamUp(powerProgEntry);
            else
                powerInfo.InitNonProgressionPower(powerProtoRef);

            return powerInfo.IsValid;
        }

        public virtual bool GetPowerProgressionInfos(List<PowerProgressionInfo> powerInfoList)
        {
            AgentTeamUpPrototype teamUpProto = PrototypeDataRef.As<AgentTeamUpPrototype>();
            if (teamUpProto == null)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): teamUpProto == null");

            if (teamUpProto.PowerProgression.HasValue())
            {
                foreach (TeamUpPowerProgressionEntryPrototype powerProgEntry in teamUpProto.PowerProgression)
                {
                    if (powerProgEntry.Power == PrototypeId.Invalid)
                    {
                        Logger.Warn("GetPowerProgressionInfos(): powerProgEntry.Power == PrototypeId.Invalid");
                        continue;
                    }

                    PowerProgressionInfo powerInfo = new();
                    powerInfo.InitForTeamUp(powerProgEntry);
                    powerInfoList.Add(powerInfo);
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the rank of all powers in the power progression for this avatar or team-up.
        /// </summary>
        /// <remarks>
        /// Calling this can lead to powers being both assigned and unassigned.
        /// </remarks>
        protected bool UpdatePowerProgressionPowers(bool forceUnassign)
        {
            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(false, "UpdatePowerProgressionPowers(): this is not Avatar && IsTeamUpAgent == false");

            List<PowerProgressionInfo> powerInfoList = ListPool<PowerProgressionInfo>.Instance.Get();
            GetPowerProgressionInfos(powerInfoList);

            for (int i = 0; i < powerInfoList.Count; i++)
            {
                PowerProgressionInfo powerInfo = powerInfoList[i];

                // Talents have their own thing
                if (powerInfo.IsTalent)
                {
                    Logger.Warn("UpdatePowerProgressionPowers(): powerInfo.IsTalent");
                    continue;
                }

                UpdatePowerRank(ref powerInfo, forceUnassign);
            }

            ListPool<PowerProgressionInfo>.Instance.Return(powerInfoList);
            return true;
        }

        #endregion

        #region Multi-Spec

        public int GetPowerSpecIndexActive()
        {
            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(0, "GetPowerSpecIndexActive(): this is not Avatar && IsTeamUpAgent == false");

            return Properties[PropertyEnum.PowerSpecIndexActive];
        }

        public virtual int GetPowerSpecIndexUnlocked()
        {
            if (IsTeamUpAgent == false) return Logger.WarnReturn(0, "GetPowerSpecIndexUnlocked(): IsTeamUpAgent == false");

            return GameDatabase.AdvancementGlobalsPrototype.MaxPowerSpecIndexForTeamUps;
        }

        public virtual bool RespecPowerSpec(int specIndex, PowersRespecReason reason, bool skipValidation = false, PrototypeId powerProtoRef = PrototypeId.Invalid)
        {
            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(false, "RespecPowerSpec(): this is not Avatar && IsTeamUpAgent == false");

            if (skipValidation == false && CanRespecPowers() == false)
                return false;

            // Lock powers (V48_TODO: is this where in pre-BUE power points should be unassigned?)
            if (specIndex == GetPowerSpecIndexActive())
                UpdatePowerProgressionPowers(true);

            // Clean up previous respecs
            List<PropertyId> removeList = ListPool<PropertyId>.Instance.Get();
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowersRespecResult, specIndex))
                removeList.Add(kvp.Key);

            foreach (PropertyId propId in removeList)
                Properties.RemoveProperty(propId);

            ListPool<PropertyId>.Instance.Return(removeList);

            // Set the new respec
            if (powerProtoRef == PrototypeId.Invalid)
                powerProtoRef = GameDatabase.GlobalsPrototype.PowerPrototype;

            Properties[PropertyEnum.PowersRespecResult, specIndex, (int)reason, powerProtoRef] = true;

            return true;
        }

        public bool CanRespecPowers()
        {
            if (this is not Avatar && IsTeamUpAgent == false) return Logger.WarnReturn(false, "CanRespecPowers(): this is not Avatar && IsTeamUpAgent == false");

            // Check for hub/training room overrides that always allow to respec
            if (IsInWorld)
            {
                Region region = Region;
                if (region == null) return Logger.WarnReturn(false, "CanRespecPowers(): region == null");

                RegionPrototype regionProto = region.Prototype;
                if (regionProto == null) return Logger.WarnReturn(false, "CanRespecPowers(): regionProto == null");

                if (regionProto.SynergyEditAllowed)
                    return true;
            }

            if (Properties[PropertyEnum.IsInCombat])
                return false;

            // Team-ups need to check their owner avatar because some of their powers are assigned to the owner as procs
            if (IsTeamUpAgent)
            {
                Avatar teamUpOwner = TeamUpOwner;
                if (teamUpOwner != null)
                    return teamUpOwner.CanRespecPowers();
            }

            return true;
        }

        #endregion

        #region Combat State

        public override bool EnterCombat()
        {
            if (TestStatus(EntityStatus.ExitingWorld))
                return false;

            AgentPrototype agentProto = AgentPrototype;
            TimeSpan inCombatTime = TimeSpan.FromMilliseconds(agentProto.InCombatTimerMS);
            
            // If already in combat, restart combat timer
            if (Properties[PropertyEnum.IsInCombat])
            {
                Game.GameEventScheduler.RescheduleEvent(_exitCombatEvent, inCombatTime);
                return true;
            }

            // Enter combat if not currently in combat
            ScheduleEntityEvent(_exitCombatEvent, inCombatTime);

            Properties[PropertyEnum.IsInCombat] = true;
            TryActivateOnInCombatProcs();
            TriggerEntityActionEvent(EntitySelectorActionEventType.OnEnteredCombat);

            return true;
        }

        public bool ExitCombat()
        {
            if (Properties[PropertyEnum.IsInCombat] == false)
                return Logger.WarnReturn(false, $"ExitCombat(): Agent [{this}] is not in combat");

            if (_exitCombatEvent.IsValid)
                Game.GameEventScheduler.CancelEvent(_exitCombatEvent);

            Properties[PropertyEnum.IsInCombat] = false;
            TryActivateOnOutCombatProcs();
            TriggerEntityActionEvent(EntitySelectorActionEventType.OnExitedCombat);

            return true;
        }

        #endregion

        #region Leveling

        public virtual void InitializeLevel(int newLevel)
        {
            int oldLevel = CharacterLevel;
            CharacterLevel = newLevel;

            Properties[PropertyEnum.ExperiencePoints] = 0;
            Properties[PropertyEnum.ExperiencePointsNeeded] = GetLevelUpXPRequirement(newLevel);

            OnLevelUp(oldLevel, newLevel);
        }

        public virtual long AwardXP(long amount, long minAmount, bool showXPAwardedText)
        {
            if (this is not Avatar && IsTeamUpAgent == false)
                return 0;

            // Only entities owned by players can earn experience
            Player owner = GetOwnerOfType<Player>();
            if (owner == null) return Logger.WarnReturn(0, "AwardXP(): owner == null");

            // Apply cosmic prestige penalty
            long awardedAmount = Math.Max((long)(amount * GetPrestigeXPFactor()), minAmount);
            if (awardedAmount <= 0)
                return 0;

            if (IsAtLevelCap == false)
            {
                Properties[PropertyEnum.ExperiencePoints] += awardedAmount;
                TryLevelUp(owner);
            }

            if (showXPAwardedText && owner.InterestedInEntity(this, AOINetworkPolicyValues.AOIChannelOwner))
            {
                owner.SendMessage(NetMessageShowXPAwardedText.CreateBuilder()
                    .SetXpAwarded(awardedAmount)
                    .SetAgentId(Id)
                    .Build());
            }

            return amount;
        }

        public virtual long GetLevelUpXPRequirement(int level)
        {
            if (IsTeamUpAgent == false) return Logger.WarnReturn(0, "GetLevelUpXPRequirement(): IsTeamUpAgent == false");

            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            if (advancementProto == null) return Logger.WarnReturn(0, "GetLevelUpXPRequirement(): advancementProto == null");

            return advancementProto.GetTeamUpLevelUpXPRequirement(level);
        }

        public virtual float GetPrestigeXPFactor()
        {
            return 1f;
        }

        public virtual int TryLevelUp(Player owner, bool isInitializing = false)
        {
            int oldLevel = CharacterLevel;
            int newLevel = oldLevel;

            long xp = Properties[PropertyEnum.ExperiencePoints];
            long xpNeeded = Properties[PropertyEnum.ExperiencePointsNeeded];

            int levelCap = owner.GetLevelCapForCharacter(PrototypeDataRef);
            while (newLevel < levelCap && xp >= xpNeeded)
            {
                xp -= xpNeeded;
                newLevel++;
                xpNeeded = GetLevelUpXPRequirement(newLevel);
            }

            int levelDelta = newLevel - oldLevel;
            if (levelDelta != 0)
            {
                CharacterLevel = newLevel;
                Properties[PropertyEnum.ExperiencePoints] = xp;
                Properties[PropertyEnum.ExperiencePointsNeeded] = xpNeeded;
            }

            if (isInitializing || levelDelta != 0)
                OnLevelUp(oldLevel, newLevel);

            return levelDelta;
        }

        public static int GetTeamUpLevelCap()
        {
            AdvancementGlobalsPrototype advancementProto = GameDatabase.AdvancementGlobalsPrototype;
            return advancementProto != null ? advancementProto.GetTeamUpLevelCap() : 0;
        }

        protected virtual bool OnLevelUp(int oldLevel, int newLevel, bool restoreHealthAndEndurance = true)
        {
            if (IsTeamUpAgent == false) return Logger.WarnReturn(false, "OnLevelUp(): IsTeamUpAgent == false");
            
            // Restore health if needed
            if (restoreHealthAndEndurance && IsDead == false)
                Properties[PropertyEnum.Health] = Properties[PropertyEnum.HealthMax];

            // Unlock new powers
            if (TeamUpOwner != null)
                UpdatePowerProgressionPowers(false);

            // Update player owner property if reached level cap
            Player owner = GetOwnerOfType<Player>();
            if (owner != null && IsAtLevelCap)
                owner.Properties.AdjustProperty(1, PropertyEnum.TeamUpsAtMaxLevelPersistent);

            // Notify the client
            SendLevelUpMessage();
            return true;
        }

        protected void SendLevelUpMessage()
        {
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            PlayerConnectionManager networkManager = Game.NetworkManager;
            if (networkManager.GetInterestedClients(interestedClientList, this, AOINetworkPolicyValues.AOIChannelOwner | AOINetworkPolicyValues.AOIChannelProximity))
            {
                var levelUpMessage = NetMessageLevelUp.CreateBuilder().SetEntityID(Id).Build();
                networkManager.SendMessageToMultiple(interestedClientList, levelUpMessage);
            }

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }

        protected override void SetCharacterLevel(int characterLevel)
        {
            int oldCharacterLevel = CharacterLevel;
            base.SetCharacterLevel(characterLevel);

            if (characterLevel != oldCharacterLevel && CanBePlayerOwned())
                PowerCollection?.OnOwnerLevelChange();
        }

        protected override void SetCombatLevel(int combatLevel)
        {
            int oldCombatLevel = CombatLevel;
            base.SetCombatLevel(combatLevel);

            if (combatLevel != oldCombatLevel && CanBePlayerOwned())
                PowerCollection?.OnOwnerLevelChange();

            foreach (var summon in new SummonedEntityIterator(this))
                summon.CombatLevel = combatLevel;
        }

        #endregion

        #region Interaction

        public virtual bool UseInteractableObject(ulong entityId, PrototypeId missionProtoRef)
        {
            // NOTE: This appears to be unused by regular agents.
            var interactableObject = Game.EntityManager.GetEntity<WorldEntity>(entityId);
            if (interactableObject == null || interactableObject.IsInWorld == false) return false;
            if (InInteractRange(interactableObject, InteractionMethod.Use) == false) return false;
            interactableObject.OnInteractedWith(this);
            return true;
        }

        public InteractionResult StartInteractionWith(EntityDesc interacteeDesc, InteractionFlags flags, bool inRange, InteractionMethod method)
        {
            if (interacteeDesc.IsValid == false) return InteractionResult.Failure;
            return PreAttemptInteractionWith(interacteeDesc, flags, method);
            // switch result for client only
        }

        private InteractionResult PreAttemptInteractionWith(EntityDesc interacteeDesc, InteractionFlags flags, InteractionMethod method)
        {
            var interactee = interacteeDesc.GetEntity<WorldEntity>(Game);
            if (interactee != null)
            {
                // UpdateServerAvatarState client only
                return interactee.AttemptInteractionBy(new EntityDesc(this), flags, method);
            }
            // IsRemoteValid client only
            return InteractionResult.Failure;
        }

        #endregion

        #region Inventory

        public InventoryResult CanEquip(Item item, out PropertyEnum propertyRestriction)
        {
            propertyRestriction = PropertyEnum.Invalid;

            PrototypeId agentProtoRef = PrototypeDataRef;
            if (agentProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(InventoryResult.Invalid, "CanEquip(): agentProtoRef == PrototypeId.Invalid");

            // Check EquippableBy
            PrototypeId equippableBy = item.ItemSpec.EquippableBy;
            if (equippableBy != PrototypeId.Invalid && equippableBy != agentProtoRef)
                return InventoryResult.InvalidCharacterRestriction;

            // Check binding
            if (item.BindsToCharacterOnEquip && item.IsBoundToCharacter && item.BoundAgentProtoRef != agentProtoRef)
                return InventoryResult.InvalidBound;

            // Check item type
            InventoryResult typeResult = CanEquipItemType(item.PrototypeDataRef);
            if (typeResult != InventoryResult.Success)
                return typeResult;

            // Check requirement properties
            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;
            foreach (var kvp in item.Properties.IteratePropertyRange(PropertyEnum.Requirement))
            {
                float requiredValue = kvp.Value;
                if (requiredValue <= 0f)
                    continue;

                Property.FromParam(kvp.Key, 0, out PrototypeId propertyInfoProtoRef);
                if (propertyInfoProtoRef == PrototypeId.Invalid)
                    continue;

                PropertyEnum property = propertyInfoTable.GetPropertyEnumFromPrototype(propertyInfoProtoRef);
                PropertyInfoPrototype propertyInfoProto = propertyInfoProtoRef.As<PropertyInfoPrototype>();
                if (propertyInfoProto == null)
                {
                    Logger.Warn("CanEquip(): propertyInfoProto == null");
                    continue;
                }

                float value = 0f;
                switch (propertyInfoProto.Type)
                {
                    case PropertyDataType.Boolean:
                        value = Properties[property] ? 1f : 0f;
                        break;

                    case PropertyDataType.Real:
                        value = Properties[property];
                        break;

                    case PropertyDataType.Integer:
                        value = (int)Properties[property];
                        break;

                    default:
                        return Logger.WarnReturn(InventoryResult.Invalid, "CanEquip(): Invalid requirement property");
                }

                if (value < requiredValue)
                {
                    propertyRestriction = property;
                    return InventoryResult.InvalidPropertyRestriction;
                }
            }

            // All good
            return InventoryResult.Success;
        }

        public InventoryResult CanEquipItemType(PrototypeId itemProtoRef)
        {
            if (itemProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(InventoryResult.Invalid, "CanEquipItemType(): itemProtoRef == PrototypeId.Invalid");

            ItemPrototype itemProto = itemProtoRef.As<ItemPrototype>();
            if (itemProto == null) return Logger.WarnReturn(InventoryResult.Invalid, "CanEquipItemType(): itemProto == null");

            AgentPrototype agentProto = AgentPrototype;
            if (agentProto == null) return Logger.WarnReturn(InventoryResult.Invalid, "CanEquipItemType(): agentProto == null");

            if (itemProto.IsUsableByAgent(agentProto) == false)
            {
                if (itemProto is CostumePrototype)
                    return InventoryResult.InvalidCostumeForCharacter;

                return InventoryResult.InvalidItemTypeForCharacter;
            }

            return InventoryResult.Success;
        }

        public bool RevealEquipmentToOwner()
        {
            // Make sure this agent is owned by a player (only avatars and team-ups have equipment that needs to be made visible)
            var player = GetOwnerOfType<Player>();
            if (player == null) return Logger.WarnReturn(false, "RevealEquipmentToOwner(): player == null");

            AreaOfInterest aoi = player.AOI;

            foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
            {
                if (inventory.VisibleToOwner) continue;     // Skip inventories that are already visible
                inventory.VisibleToOwner = true;

                foreach (var entry in inventory)
                {
                    // Validate entity
                    var entity = Game.EntityManager.GetEntity<Entity>(entry.Id);
                    if (entity == null)
                    {
                        Logger.Warn("RevealEquipmentToOwner(): entity == null");
                        continue;
                    }

                    // Update interest for it
                    aoi.ConsiderEntity(entity);
                }
            }

            return true;
        }

        public override void OnOtherEntityAddedToMyInventory(Entity entity, InventoryLocation invLoc, bool unpackedArchivedEntity)
        {
            InventoryPrototype inventoryPrototype = invLoc.InventoryPrototype;
            if (inventoryPrototype == null) { Logger.Warn("OnOtherEntityAddedToMyInventory(): inventoryPrototype == null"); return; }

            if (inventoryPrototype.IsEquipmentInventory)
            {
                // Validate and aggregate equipped item's properties
                if (entity == null) { Logger.Warn("OnOtherEntityAddedToMyInventory(): entity == null"); return; }
                if (entity is not Item) { Logger.Warn("OnOtherEntityAddedToMyInventory(): entity is not Item"); return; }
                if (invLoc.ContainerId != Id) { Logger.Warn("OnOtherEntityAddedToMyInventory(): invLoc.ContainerId != Id"); return; }

                if (UpdateProcEffectPowers(entity.Properties, true) == false)
                    Logger.Warn($"OnOtherEntityAddedToMyInventory(): UpdateProcEffectPowers failed when equipping item=[{entity}] owner=[{this}]");

                Properties.AddChildCollection(entity.Properties);
            }

            base.OnOtherEntityAddedToMyInventory(entity, invLoc, unpackedArchivedEntity);
        }

        public override void OnOtherEntityRemovedFromMyInventory(Entity entity, InventoryLocation invLoc)
        {
            InventoryPrototype inventoryPrototype = invLoc.InventoryPrototype;
            if (inventoryPrototype == null) { Logger.Warn("OnOtherEntityRemovedFromMyInventory(): inventoryPrototype == null"); return; }

            if (inventoryPrototype.IsEquipmentInventory)
            {
                // Validate and remove equipped item's properties
                if (entity == null) { Logger.Warn("OnOtherEntityRemovedFromMyInventory(): entity == null"); return; }
                if (entity is not Item) { Logger.Warn("OnOtherEntityRemovedFromMyInventory(): entity is not Item"); return; }
                if (invLoc.ContainerId != Id) { Logger.Warn("OnOtherEntityRemovedFromMyInventory(): invLoc.ContainerId != Id"); return; }

                entity.Properties.RemoveFromParent(Properties);

                UpdateProcEffectPowers(entity.Properties, false);
            }

            base.OnOtherEntityRemovedFromMyInventory(entity, invLoc);
        }

        protected override bool InitInventories(bool populateInventories)
        {
            bool success = base.InitInventories(populateInventories);

            if (Prototype is AgentTeamUpPrototype teamUpAgentProto && teamUpAgentProto.EquipmentInventories.HasValue())
            {
                foreach (AvatarEquipInventoryAssignmentPrototype equipInvAssignment in teamUpAgentProto.EquipmentInventories)
                {
                    if (AddInventory(equipInvAssignment.Inventory, populateInventories ? equipInvAssignment.LootTable : PrototypeId.Invalid) == false)
                    {
                        success = false;
                        Logger.Warn($"InitInventories(): Failed to add inventory {GameDatabase.GetPrototypeName(equipInvAssignment.Inventory)} to {this}");
                    }
                }
            }

            return success;
        }

        #endregion

        #region AI

        public void ActivateAI()
        {
            if (AIController == null) return;
            BehaviorBlackboard blackboard = AIController.Blackboard;
            if (blackboard.PropertyCollection[PropertyEnum.AIStartsEnabled])
                AIController.SetIsEnabled(true);
            blackboard.SpawnOffset = (SpawnSpec != null) ? SpawnSpec.Transform.Translation : Vector3.Zero;
            if (IsInWorld)
                AIController.OnAIActivated();
        }

        public void Think()
        {
            AIController?.Think();
        }

        private void AllianceChange()
        {
            AIController?.OnAIAllianceChange();
        }

        public void SetDormant(bool dormant)
        {
            if (IsDormant != dormant)
            {
                if (dormant == false)
                {
                    AgentPrototype prototype = AgentPrototype;
                    if (prototype == null) return;
                    if (prototype.WakeRandomStartMS > 0 && IsControlledEntity == false)
                        ScheduleRandomWakeStart(prototype.WakeRandomStartMS);
                    else
                        Properties[PropertyEnum.Dormant] = dormant;
                }
                else
                    Properties[PropertyEnum.Dormant] = dormant;
            }
        }

        public override SimulateResult SetSimulated(bool simulated)
        {
            SimulateResult result = base.SetSimulated(simulated);

            if (result == SimulateResult.Set)
            {
                AIController?.OnAISetSimulated(true);

                if (AgentPrototype.WakeRange <= 0.0f) SetDormant(false);
                if (IsDormant == false) TryAutoActivatePowersInCollection();

                TriggerEntityActionEvent(EntitySelectorActionEventType.OnSimulated);
            }
            else if (result == SimulateResult.Clear)
            {
                AIController?.OnAISetSimulated(false);

                EntityActionComponent?.RestartPendingActions();
                var scheduler = Game?.GameEventScheduler;
                if (scheduler != null)
                {
                    scheduler.CancelEvent(_wakeStartEvent);
                    scheduler.CancelEvent(_wakeEndEvent);
                }
            }

            // Update equipment tickers
            if (result != SimulateResult.None)
            {
                EntityManager entityManager = Game.EntityManager;
                foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
                {
                    foreach (var entry in inventory)
                    {
                        Item item = entityManager.GetEntity<Item>(entry.Id);
                        if (item == null)
                            continue;

                        if (result == SimulateResult.Set)
                            item.StartTicking(this);
                        else
                            item.StopTicking(this);
                    }
                }
            }

            return result;
        }

        public void InitAIOverride(ProceduralAIProfilePrototype profile, PropertyCollection collection)
        {
            if (profile == null || Game == null || collection == null) return;
            collection[PropertyEnum.AIFullOverride] = profile.DataRef;
            AIController = new(Game, this);
            var behaviorProfile = AgentPrototype?.BehaviorProfile;
            if (behaviorProfile == null) return;
            AIController.OnInitAIOverride(behaviorProfile, collection);
        }

        private bool InitAI(EntitySettings settings)
        {
            var agentPrototype = AgentPrototype;
            if (agentPrototype == null || Game == null || this is Avatar) return false;

            var behaviorProfile = agentPrototype.BehaviorProfile;
            if (behaviorProfile != null && behaviorProfile.Brain != PrototypeId.Invalid)
            {
                AIController = new(Game, this);
                using PropertyCollection collection = ObjectPoolManager.Instance.Get<PropertyCollection>();
                collection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] = Properties[PropertyEnum.AIIgnoreNoTgtOverrideProfile];
                SpawnSpec spec = settings?.SpawnSpec ?? new SpawnSpec(Game);
                return AIController.Initialize(behaviorProfile, spec, collection);
            }
            return false;
        }

        public override void OnCollide(WorldEntity whom, Vector3 whoPos)
        {
            // Trigger procs
            TryActivateOnCollideProcs(ProcTriggerType.OnCollide, whom, whoPos);

            if (whom != null)
                TryActivateOnCollideProcs(ProcTriggerType.OnCollideEntity, whom, whoPos);
            else
                TryActivateOnCollideProcs(ProcTriggerType.OnCollideWorldGeo, whom, whoPos);

            // Notify AI
            AIController?.OnAIOnCollide(whom);
        }

        public override void OnOverlapBegin(WorldEntity whom, Vector3 whoPos, Vector3 whomPos)
        {
            base.OnOverlapBegin(whom, whoPos, whomPos);

            // Trigger procs
            TryActivateOnOverlapBeginProcs(whom, whoPos);

            // Notify AI
            AIController?.OnAIOverlapBegin(whom);
        }

        #endregion

        #region Event Handlers

        public override void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            base.OnPropertyChange(id, newValue, oldValue, flags);
            if (flags.HasFlag(SetPropertyFlags.Refresh)) return;

            switch (id.Enum)
            {
                case PropertyEnum.AllianceOverride:
                    AllianceChange();
                    break;

                case PropertyEnum.Confused:
                    SetFlag(EntityFlags.Confused, newValue);
                    AllianceChange();
                    break;

                case PropertyEnum.EnemyBoost:

                    if (IsInWorld)
                    {
                        Property.FromParam(id, 0, out PrototypeId enemyBoost);
                        if (enemyBoost == PrototypeId.Invalid) break;
                        if (newValue) AssignEnemyBoostActivePower(enemyBoost);
                    }

                    break;

                case PropertyEnum.AIMasterAvatarDbGuid:

                    if (newValue == 0ul)
                    {
                        var scheduler = Game?.GameEventScheduler;
                        if (scheduler == null) return;
                        scheduler.CancelEvent(_respawnControlledAgentEvent);
                        if (IsDead) OnRemoveFromWorld(KillFlags.None);
                    }

                    break;

                case PropertyEnum.Knockback:
                case PropertyEnum.Knockdown:
                case PropertyEnum.Knockup:
                case PropertyEnum.Mesmerized:
                case PropertyEnum.Stunned:
                case PropertyEnum.StunnedByHitReact:
                case PropertyEnum.NPCAmbientLock:

                    if (newValue) 
                    {
                        var activePower = ActivePower;
                        bool endPower = false;
                        var endFlags = EndPowerFlags.ExplicitCancel | EndPowerFlags.Interrupting;
                        if (activePower != null)
                            endPower = activePower.EndPower(endFlags);

                        Locomotor?.Stop();

                        var throwablePower = GetThrowablePower();
                        if (throwablePower != null)
                        {
                            if (AIController != null)
                            {
                                if (!endPower || activePower != throwablePower)
                                    AIController.OnAIPowerEnded(throwablePower.PrototypeDataRef, endFlags);
                            }
                            UnassignPower(throwablePower.PrototypeDataRef);
                        }
                    }

                    if (id.Enum == PropertyEnum.Knockdown && newValue == false && oldValue)
                        TryActivateOnKnockdownEndProcs();

                    break;

                case PropertyEnum.Immobilized:
                case PropertyEnum.ImmobilizedByHitReact:
                case PropertyEnum.SystemImmobilized:
                case PropertyEnum.TutorialImmobilized:

                    if (newValue) StopLocomotor();
                    break;

                case PropertyEnum.Dormant:

                    bool dormant = newValue;
                    SetFlag(EntityFlags.Dormant, dormant);
                    if (dormant == false) СheckWakeDelay();
                    RegisterForPendingPhysicsResolve();
                    if (!IsVisibleWhenDormant) Properties[PropertyEnum.Visible] = !dormant;

                    break;

                case PropertyEnum.PowerBoost:
                    Property.FromParam(id, 0, out PrototypeId powerBoostProtoRef);
                    UpdatePowerBoost(powerBoostProtoRef);
                    break;

                case PropertyEnum.PowerGrantRank:
                    Property.FromParam(id, 0, out PrototypeId powerGrantProtoRef);
                    UpdatePowerGrant(powerGrantProtoRef);
                    break;
            }

            AIController?.Brain?.OnPropertyChange(id, newValue, oldValue, flags);
        }

        private void StopLocomotor()
        {
            if (IsInWorld) Locomotor?.Stop();
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);

            // Assign on resurrected power
            PrototypeId onResurrectedPowerRef = AgentPrototype.OnResurrectedPower;
            if (onResurrectedPowerRef != PrototypeId.Invalid)
            {
                PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
                AssignPower(onResurrectedPowerRef, indexProps);
            }

            // TeamUp synergy
            UpdateTeamUpSynergyCondition();

            // AI
            // if (TestAI() == false) return;

            var behaviorProfile = AgentPrototype?.BehaviorProfile;

            if (AIController != null)
            {                
                if (behaviorProfile == null) return;
                AIController.Initialize(behaviorProfile, null, null);
            }
            else InitAI(settings);

            if (AIController != null)
            {
                AIController.OnAIEnteredWorld();
                ActivateAI();
            }

            if (behaviorProfile != null)
                EquipPassivePowers(behaviorProfile.EquippedPassivePowers);

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.EnemyBoost))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId enemyBoost);
                if (enemyBoost == PrototypeId.Invalid) continue;
                AssignEnemyBoostActivePower(enemyBoost);
            }

            if (IsSimulated && Properties.HasProperty(PropertyEnum.AIPowerOnSpawn))
            {
                PrototypeId startPower = Properties[PropertyEnum.AIPowerOnSpawn];
                if (startPower != PrototypeId.Invalid)
                {
                    PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
                    AssignPower(startPower, indexProps);
                    var position = RegionLocation.Position;
                    var powerSettings = new PowerActivationSettings(Id, position, position)
                    { Flags = PowerActivationSettingsFlags.NotifyOwner };
                    ActivatePower(startPower, ref powerSettings);
                }
            }

            if (Properties.HasProperty(PropertyEnum.PlaceableDead))
                Kill();

            TeamUpOwner?.OnEnteredWorldTeamUpAgent();

            if (AIController == null)
                EntityActionComponent?.InitActionBrain();
        }

        private void AssignEnemyBoostActivePower(PrototypeId enemyBoost)
        {
            var boostProto = GameDatabase.GetPrototype<EnemyBoostPrototype>(enemyBoost);
            if (boostProto == null) return;
            var activePower = boostProto.ActivePower;
            if (activePower != PrototypeId.Invalid)
            {
                PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
                AssignPower(activePower, indexProps);
            }
        }

        private void EquipPassivePowers(PrototypeId[] passivePowers)
        {
            if (passivePowers.IsNullOrEmpty()) return;
            foreach (var powerRef in passivePowers)
            {
                var powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerRef);
                if (powerProto == null || powerProto.Activation != PowerActivationType.Passive) continue;
                int rank = Properties[PropertyEnum.PowerRank];
                PowerIndexProperties indexProps = new(rank, CharacterLevel, CombatLevel);
                AssignPower(powerRef, indexProps);
            }
        }

        public override void OnExitedWorld()
        {
            if (Properties[PropertyEnum.IsInCombat])
                ExitCombat();

            base.OnExitedWorld();
            AIController?.OnAIExitedWorld();

            // Cancel events
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.CancelEvent(_respawnControlledAgentEvent);
            scheduler.CancelEvent(_movementStartedEvent);
            scheduler.CancelEvent(_movementStoppedEvent);

            if (this is Avatar || IsTeamUpAgent)
                Properties.RemovePropertyRange(PropertyEnum.PowerRankBase);

            // Remove the team-up synergy condition
            if (IsTeamUpAgent)
            {
                Player player = GetOwnerOfType<Player>();
                if (player != null)
                {
                    ulong teamUpSynergyConditionId = player.TeamUpSynergyConditionId;
                    if (teamUpSynergyConditionId != ConditionCollection.InvalidConditionId)
                    {
                        ConditionCollection.RemoveCondition(teamUpSynergyConditionId);
                        player.TeamUpSynergyConditionId = ConditionCollection.InvalidConditionId;
                    }
                }
            }

            TeamUpOwner?.OnExitedWorldTeamUpAgent(this);
        }

        public override void OnGotHit(WorldEntity attacker)
        {
            AIController?.OnAIOnGotHit(attacker);
            base.OnGotHit(attacker);
        }

        public override void OnDramaticEntranceEnd()
        {
            base.OnDramaticEntranceEnd();
            AIController?.OnAIDramaticEntranceEnd();
        }

        public override void OnKilled(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            AssignSummonPowersToOwnerOnKilled();
            KillSummonedOnOwnerDeath();

            if (IsControlledEntity && killFlags.HasFlag(KillFlags.Release) == false)
                ScheduleRespawnControlledAgent();

            Avatar teamUpOwner = TeamUpOwner;
            if (teamUpOwner != null && this == teamUpOwner.CurrentTeamUpAgent)
                teamUpOwner.ResetTeamUpAgentDuration();

            if (Prototype is OrbPrototype && Properties.HasProperty(PropertyEnum.ItemCurrency) == false)
            {
                var avatar = killer as Avatar;
                var player = avatar?.GetOwnerOfType<Player>();
                player?.OnScoringEvent(new(ScoringEventType.OrbsCollected, Prototype));
            }

            if (AIController != null)
            {
                AIController.OnAIKilled();
                AIController.SetIsEnabled(false);
            }

            EndAllPowers(false);

            Locomotor locomotor = Locomotor;
            if (locomotor != null)
            {
                locomotor.Stop();
                locomotor.SetMethod(LocomotorMethod.Default, 0.0f);
            }

            base.OnKilled(killer, killFlags, directKiller);
        }

        public override void OnDeallocate()
        {
            AIController?.OnAIDeallocate();
            base.OnDeallocate();
        }

        public override void OnLocomotionStateChanged(LocomotionState oldState, LocomotionState newState)
        {
            base.OnLocomotionStateChanged(oldState, newState);

            if (IsInWorld == false || TestStatus(EntityStatus.ExitingWorld))
                return;

            if (IsSimulated)
            {
                if ((oldState.Method == LocomotorMethod.HighFlying) != (newState.Method == LocomotorMethod.HighFlying))
                {
                    Vector3 currentPosition = RegionLocation.Position;
                    Vector3 targetPosition = FloorToCenter(RegionLocation.ProjectToFloor(RegionLocation.Region, RegionLocation.Cell, currentPosition));
                    ChangeRegionPosition(targetPosition, null, ChangePositionFlags.DoNotSendToOwner | ChangePositionFlags.HighFlying);
                }
            }

            // Check movement started/stopped procs if started/stopped locomoting.
            // Use mutually exclusive events scheduled to the end of the current
            // frame to do only one start/stop within a single frame.
            bool isLocomoting = newState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting);
            bool wasLocomoting = oldState.LocomotionFlags.HasFlag(LocomotionFlags.IsLocomoting);
            if (isLocomoting ^ wasLocomoting)
            {
                EventScheduler scheduler = Game.GameEventScheduler;

                if (isLocomoting)
                {
                    scheduler.CancelEvent(_movementStoppedEvent);

                    if (_movementStartedEvent.IsValid == false)
                        ScheduleEntityEvent(_movementStartedEvent, TimeSpan.Zero);
                }
                else
                {
                    scheduler.CancelEvent(_movementStartedEvent);

                    if (_movementStoppedEvent.IsValid == false)
                        ScheduleEntityEvent(_movementStoppedEvent, TimeSpan.Zero);
                }
            }
        }

        public override bool OnPowerAssigned(Power power)
        {
            if (base.OnPowerAssigned(power) == false)
                return false;

            // Make sure non-power progression powers and talents assigned to avatars always have at least rank 1
            bool isPowerProgressionPower = PowerCollection.ContainsPower(power.PrototypeDataRef, true);

            if (this is Avatar &&
                (isPowerProgressionPower == false || power.IsTalentPower()) &&
                power.IsNormalPower() && power.IsEmotePower() == false)
            {
                PropertyId rankBaseProp = new(PropertyEnum.PowerRankBase, power.PrototypeDataRef);
                if (Properties.HasProperty(rankBaseProp) == false)
                    Properties[rankBaseProp] = 1;

                PropertyId rankCurrentBestProp = new(PropertyEnum.PowerRankCurrentBest, power.PrototypeDataRef);
                if (Properties.HasProperty(rankCurrentBestProp) == false)
                    Properties[rankCurrentBestProp] = 1;
            }

            if (IsDormant == false)
                TryAutoActivatePower(power);

            return true;
        }

        public override bool OnPowerUnassigned(Power power)
        {
            Properties.RemoveProperty(new(PropertyEnum.PowerRankBase, power.PrototypeDataRef));
            Properties.RemoveProperty(new(PropertyEnum.PowerRankCurrentBest, power.PrototypeDataRef));

            PowerCategoryType powerCategory = power.GetPowerCategory();
            if (powerCategory == PowerCategoryType.ThrowablePower)
            {
                TryRestoreThrowable();

                Power throwableCancelPower = GetThrowableCancelPower();
                if (throwableCancelPower != null)
                    UnassignPower(throwableCancelPower.PrototypeDataRef);
            }
            else if (powerCategory == PowerCategoryType.ThrowableCancelPower)
            {
                Power throwablePower = GetThrowablePower();
                if (throwablePower != null)
                    UnassignPower(throwablePower.PrototypeDataRef);
            }

            return base.OnPowerUnassigned(power);
        }

        public override void OnPowerEnded(Power power, EndPowerFlags flags)
        {
            base.OnPowerEnded(power, flags);

            PrototypeId powerProtoRef = power.PrototypeDataRef;

            if (powerProtoRef == ActivePowerRef)
            {
                if (power.IsComboEffect())
                {
                    // Restore the triggering power as the active one if its exclusive activation
                    PrototypeId triggeringPowerRef = power.Properties[PropertyEnum.TriggeringPowerRef, powerProtoRef];
                    if (triggeringPowerRef != PrototypeId.Invalid)
                    {
                        Power triggeringPower = GetPower(triggeringPowerRef);
                        if (triggeringPower != null && triggeringPower.IsActive && triggeringPower.IsExclusiveActivation())
                        {
                            ActivePowerRef = triggeringPowerRef;
                            return;
                        }
                    }
                }

                ActivePowerRef = PrototypeId.Invalid;
            }

            AIController?.OnAIPowerEnded(power.PrototypeDataRef, flags);
        }

        public override bool OnNegativeStatusEffectApplied(ulong conditionId)
        {
            base.OnNegativeStatusEffectApplied(conditionId);

            // Apply CCReactCondition (if this agent has one)
            PrototypeId ccReactConditionProtoRef = AgentPrototype.CCReactCondition;
            if (ccReactConditionProtoRef == PrototypeId.Invalid)
                return true;

            Condition negativeStatusCondition = ConditionCollection.GetCondition(conditionId);
            if (negativeStatusCondition == null) return Logger.WarnReturn(false, "OnNegativeStatusEffectApplied(): condition == null");

            // Skip hit react conditions
            if (negativeStatusCondition.IsHitReactCondition())
                return true;

            // Skip self-applied conditions
            if (negativeStatusCondition.ConditionPrototype.Scope == ConditionScopeType.User)
                return true;

            ConditionPrototype ccReactConditionProto = ccReactConditionProtoRef.As<ConditionPrototype>();
            if (ccReactConditionProto == null) return Logger.WarnReturn(false, "OnNegativeStatusEffectApplied(): ccReactConditionProto == null");

            List<PrototypeId> negativeStatusList = ListPool<PrototypeId>.Instance.Get();
            if (negativeStatusCondition.IsANegativeStatusEffect(negativeStatusList))
            {
                // Apply only when this negative status condition has movement / cast speed decreases and no other statuses
                bool hasMovementSpeedDecrease = negativeStatusCondition.Properties.HasProperty(PropertyEnum.MovementSpeedDecrPct);
                bool hasCastSpeedDecrease = negativeStatusCondition.Properties.HasProperty(PropertyEnum.CastSpeedDecrPct);

                if (((hasMovementSpeedDecrease || hasCastSpeedDecrease) && negativeStatusList.Count == 1) ||
                    ((hasMovementSpeedDecrease && hasCastSpeedDecrease) && negativeStatusList.Count == 2))
                {
                    TimeSpan duration = ccReactConditionProto.GetDuration(null, this);

                    Condition ccReactCondition = ConditionCollection.AllocateCondition();
                    ccReactCondition.InitializeFromConditionPrototype(ConditionCollection.NextConditionId, Game, Id, Id, Id, ccReactConditionProto, duration);
                    ConditionCollection.AddCondition(ccReactCondition);
                }
            }
            else
            {
                Logger.Warn("OnNegativeStatusEffectApplied(): condition.IsANegativeStatusEffect(negativeStatusList) == false");
            }

            ListPool<PrototypeId>.Instance.Return(negativeStatusList);
            return true;
        }

        protected override void OnDamaged(PowerResults powerResults)
        {
            // Interrupt active cancel on damage powers (e.g. bodyslide to town, avatar swap cast)

            // Check if this power can cause cancellation (non-power payloads, like DoTs, can always cause cancellation)
            PowerPrototype powerProto = powerResults.PowerPrototype;
            if (powerProto != null && powerProto.CanCauseCancelOnDamage == false)
                return;

            // Check if there is anything to cancel
            Power activePower = ActivePower;
            if (activePower == null)
                return;

            // Check if the active power is cancelled on damage
            if (activePower.IsCancelledOnDamage() == false)
                return;

            // Cancel the power
            activePower.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Interrupting);

            // Apply channel interrupt condition (hit reaction)
            ConditionPrototype conditionProto = GameDatabase.CombatGlobalsPrototype.ChannelInterruptConditionPrototype;

            ulong creatorId = powerResults.PowerOwnerId;
            ulong ultimateCreatorId = powerResults.UltimateOwnerId;
            ulong targetId = powerResults.TargetId;

            TimeSpan duration = conditionProto.GetDuration(null, this);

            Condition condition = ConditionCollection.AllocateCondition();
            condition.InitializeFromConditionPrototype(ConditionCollection.NextConditionId, Game, creatorId, ultimateCreatorId, targetId, conditionProto, duration);
            ConditionCollection.AddCondition(condition);
        }

        #endregion

        #region Team-Ups

        public bool UpdateTeamUpSynergyCondition()
        {
            // Non-team-up agents do not have team-up synergies
            if (IsTeamUpAgent == false)
                return true;
            
            // Need a player owner to get condition synergy data from
            Player player = GetOwnerOfType<Player>();
            if (player == null)
                return true;

            // See if there is a synergy condition to add
            PrototypeId teamUpSynergyConditionRef = GameDatabase.GlobalsPrototype.TeamUpSynergyCondition;
            if (teamUpSynergyConditionRef == PrototypeId.Invalid)
                return true;

            ConditionPrototype teamUpSynergyConditionProto = teamUpSynergyConditionRef.As<ConditionPrototype>();
            if (teamUpSynergyConditionProto == null) return Logger.WarnReturn(false, "UpdateTeamUpSynergyCondition(): teamUpSynergyConditionProto == null");

            // See if there is a synergy condition we don't know about
            ulong teamUpSynergyConditionId = player.TeamUpSynergyConditionId;
            if (teamUpSynergyConditionId == ConditionCollection.InvalidConditionId)
                ConditionCollection.GetConditionIdByRef(teamUpSynergyConditionRef);

            // Remove the existing synergy condition
            if (teamUpSynergyConditionId != ConditionCollection.InvalidConditionId)
            {
                ConditionCollection.RemoveCondition(teamUpSynergyConditionId);
                player.TeamUpSynergyConditionId = ConditionCollection.InvalidConditionId;
            }

            // Add a new synergy condition
            Condition teamUpSynergyCondition = ConditionCollection.AllocateCondition();

            if (teamUpSynergyCondition.InitializeFromConditionPrototype(ConditionCollection.NextConditionId, Game,
                Id, Id, Id, teamUpSynergyConditionProto, TimeSpan.Zero))
            {
                ConditionCollection.AddCondition(teamUpSynergyCondition);
                player.TeamUpSynergyConditionId = teamUpSynergyCondition.Id;
            }
            else
            {
                ConditionCollection.DeleteCondition(teamUpSynergyCondition);
            }

            return true;
        }

        public void AssignTeamUpAgentPowers()
        {
            if (IsTeamUpAgent == false) return;
            AssignTeamUpAgentStylePowers();
            UpdatePowerProgressionPowers(false);
        }

        private void AssignTeamUpAgentStylePowers()
        {
            var teamUpProto = Prototype as AgentTeamUpPrototype;
            var styles = teamUpProto.Styles;
            if (styles.IsNullOrEmpty()) return;

            var teamUpOwner = TeamUpOwner;
            if (teamUpOwner == null) return;

            bool current = this == teamUpOwner.CurrentTeamUpAgent;
            bool isSummoned = IsAliveInWorld && TestStatus(EntityStatus.ExitingWorld);

            int currentStyle = Properties[PropertyEnum.TeamUpStyle];

            for (int styleIndex = 0; styleIndex < styles.Length; styleIndex++)
            {
                var style = styles[styleIndex];
                if (style == null || style.Power == PrototypeId.Invalid) continue;

                var powerRef = style.Power;
                var powerProto = GameDatabase.GetPrototype<PowerPrototype>(powerRef);
                if (powerProto == null || powerProto.Activation != PowerActivationType.Passive) continue;

                bool assignPower = current && currentStyle == styleIndex;

                Agent powerOwner = this;
                if (style.PowerIsOnAvatarWhileAway || style.PowerIsOnAvatarWhileSummoned)
                {
                    powerOwner = teamUpOwner;
                    if (isSummoned) assignPower &= style.PowerIsOnAvatarWhileSummoned;
                    else assignPower &= style.PowerIsOnAvatarWhileAway;
                }
                if (powerOwner.IsInWorld == false || powerOwner.TestStatus(EntityStatus.ExitingWorld)) continue;

                if (assignPower)
                {
                    var collection = powerOwner.PowerCollection;
                    if (collection == null) return;
                    if (collection.ContainsPower(powerRef) == false)
                        powerOwner.AssignPower(powerRef, new(1, CharacterLevel, CombatLevel));
                }
                else powerOwner.UnassignPower(powerRef);
            }
        }

        public void ApplyTeamUpAffixesToAvatar(Avatar avatar)
        {
            EntityManager entityManager = Game.EntityManager;
            foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
            {
                foreach (var entry in inventory)
                {
                    Item item = entityManager.GetEntity<Item>(entry.Id);
                    if (item == null)
                    {
                        Logger.Warn("ApplyTeamUpAffixesToAvatar(): item == null");
                        continue;
                    }

                    item.ApplyTeamUpAffixesToAvatar(avatar);
                }
            }
        }

        public void RemoveTeamUpAffixesFromAvatar(Avatar avatar)
        {
            EntityManager entityManager = Game.EntityManager;
            foreach (Inventory inventory in new InventoryIterator(this, InventoryIterationFlags.Equipment))
            {
                foreach (var entry in inventory)
                {
                    Item item = entityManager.GetEntity<Item>(entry.Id);
                    if (item == null)
                    {
                        Logger.Warn("RemoveTeamUpAffixesFromAvatar(): item == null");
                        continue;
                    }

                    item.RemoveTeamUpAffixesFromAvatar(avatar);
                }
            }
        }

        public void SetTeamUpsAtMaxLevel(Player player)
        {
            if (IsTeamUpAgent == false || player == null) return;

            int maxLevel = player.Properties[PropertyEnum.TeamUpsAtMaxLevelPersistent];
            if (maxLevel > 0) Properties[PropertyEnum.TeamUpsAtMaxLevel] = maxLevel;
        }

        public bool IsPermanentTeamUpStyle()
        {
            if (Prototype is not AgentTeamUpPrototype teamUpProto) return false;
            if (teamUpProto.Styles.IsNullOrEmpty()) return false;

            int styleIndex = Properties[PropertyEnum.TeamUpStyle];
            if (styleIndex < 0 || styleIndex >= teamUpProto.Styles.Length) return false;
            var style = teamUpProto.Styles[styleIndex];
            if (style == null) return false;

            return style.IsPermanent;
        }

        #endregion

        #region PersistentAgents

        public override void OnPreGeneratePath(Vector3 start, Vector3 end, List<WorldEntity> entities)
        {
            if (CanBePlayerOwned() == false) return;

            var manager = Game?.EntityManager;
            if (manager == null) return;

            Agent agent = this;
            while (agent != null)
            {
                if (agent.CanInfluenceNavigationMesh())
                    agent.DisableNavigationInfluence();

                foreach (var summon in new SummonedEntityIterator(agent))
                    if (summon.IsInWorld && summon.CanInfluenceNavigationMesh())
                        agent.DisableNavigationInfluence();

                agent = manager.GetEntity<Agent>(agent.PowerUserOverrideId);
            }
        }

        private void AssignSummonPowersToOwnerOnKilled()
        {
            var summonProto = GetSummonEntityContext();
            if (summonProto == null || summonProto.PowersToAssignToOwnerOnKilled.IsNullOrEmpty()) return;

            var manager = Game?.EntityManager;
            if (manager == null) return;

            var owner = manager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner == null) return;

            foreach (var powerProto in summonProto.PowersToAssignToOwnerOnKilled)
            {
                if (powerProto == null) continue;
                PowerIndexProperties indexProps = new(0, owner.CharacterLevel, owner.CombatLevel);
                AssignPower(powerProto.DataRef, indexProps);
            }
        }

        public bool CanSummonControlledAgent()
        {
            return _respawnControlledAgentEvent.IsValid == false;
        }

        private void ScheduleRespawnControlledAgent()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.CancelEvent(_respawnControlledAgentEvent);

            var aiGLobals = GameDatabase.AIGlobalsPrototype;
            if (aiGLobals == null) return;

            var ressurectTime = TimeSpan.FromMilliseconds(aiGLobals.ControlledAgentResurrectTimerMS);
            Properties[PropertyEnum.ControlledAgentRespawnTime] = Game.CurrentTime + ressurectTime;

            ScheduleEntityEvent(_respawnControlledAgentEvent, ressurectTime);
        }

        private void RespawnControlledAgent()
        {
            var game = Game;
            if (game == null) return;

            TimeSpan currentTime = Game.CurrentTime;
            TimeSpan ressurectTime = Properties[PropertyEnum.ControlledAgentRespawnTime];

            if (currentTime >= ressurectTime)
            {
                ulong masterGuid = Properties[PropertyEnum.AIMasterAvatarDbGuid];
                if (masterGuid == 0) return;
                var avatar = Game.EntityManager.GetEntityByDbGuid<Avatar>(masterGuid);
                if (avatar == null) return;

                if (avatar.HasControlPowerEquipped())
                {
                    SetAsPersistent(avatar, false);
                }
                else
                {
                    KillSummonedOnOwnerDeath();
                    ExitWorld();
                }
            }
            else
            {
                var scheduler = Game.GameEventScheduler;
                if (scheduler == null) return;
                scheduler.CancelEvent(_respawnControlledAgentEvent);
                ScheduleEntityEvent(_respawnControlledAgentEvent, ressurectTime - currentTime);
            }
        }

        public override void SetAsPersistent(Avatar avatar, bool newOnServer)
        {
            if (IsControlledEntity)
            {
                SetState(PrototypeId.Invalid);
                if (IsDead) Resurrect();
                SetControlledProperties(avatar);
            }

            CombatLevel = avatar.CombatLevel;

            base.SetAsPersistent(avatar, newOnServer);

            ActivateAI();

            var controller = AIController;
            if (controller == null) return;

            controller.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = avatar.Id;
            controller.ResetCurrentTargetState();
        }

        public void SetControlledProperties(Avatar avatar)
        {
            if (Properties[PropertyEnum.AIMasterAvatarDbGuid] != avatar.DatabaseUniqueId) return;

            Properties[PropertyEnum.NoLootDrop] = true;
            Properties[PropertyEnum.NoExpOnDeath] = true;
            Properties[PropertyEnum.AIIgnoreNoTgtOverrideProfile] = true;
            Properties[PropertyEnum.DramaticEntrancePlayedOnce] = true;
            Properties[PropertyEnum.PetHealthPctBonus] = avatar.Properties[PropertyEnum.HealthPctBonus];
            Properties[PropertyEnum.PetDamagePctBonus] = avatar.Properties[PropertyEnum.DamagePctBonus];

            // IMPORTANT: Dormant needs to be turned off after setting DramaticEntrancePlayedOnce.
            SetDormant(false);

            AIController?.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFullOverride);
            Properties.RemoveProperty(PropertyEnum.MissionPrototype);
            Properties.RemoveProperty(PropertyEnum.DetachOnContainerDestroyed);

            var region = Region;
            if (region != null)
            {
                var tracker = region.EntityTracker;
                if (tracker == null) return;
                tracker.RemoveFromTracking(this);
            }

            SetSummonedAllianceOverride(avatar.Alliance);

            List<PrototypeId> boostList = ListPool<PrototypeId>.Instance.Get();

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.EnemyBoost))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId boostRef);
                if (boostRef == PrototypeId.Invalid) continue;
                var boostProto = GameDatabase.GetPrototype<EnemyBoostPrototype>(boostRef);
                if (boostProto == null) continue;

                if (boostProto.DisableForControlledAgents)
                    boostList.Add(boostRef);
            }

            foreach (var boostRef in boostList)
                Properties.RemoveProperty(new PropertyId(PropertyEnum.EnemyBoost, boostRef));

            ListPool<PrototypeId>.Instance.Return(boostList);
        }

        public void SetSummonedAllianceOverride(AlliancePrototype alliance)
        {
            if (alliance == null) return;
            var allianceRef = alliance.DataRef;
            Properties[PropertyEnum.AllianceOverride] = allianceRef;
            foreach (var summoned in new SummonedEntityIterator(this))
                summoned.Properties[PropertyEnum.AllianceOverride] = allianceRef;
        }

        public void KillSummonedOnOwnerDeath()
        {
            List<WorldEntity> summons = ListPool<WorldEntity>.Instance.Get();

            foreach (var summoned in new SummonedEntityIterator(this))
            {
                if (summoned.IsDead) continue;
                if (summoned.IsDestroyed || summoned.TestStatus(EntityStatus.PendingDestroy)) continue;

                var contextProto = summoned.GetSummonEntityContext();               
                if (contextProto == null) continue;

                if (contextProto.KillEntityOnOwnerDeath)
                    summons.Add(summoned);
            }

            foreach (var summoned in summons)
                SummonPower.KillSummoned(summoned, this);

            ListPool<WorldEntity>.Instance.Return(summons);
        }

        public override bool IsSummonedPet()
        {
            if (this is Missile) return false;
            if (IsTeamUpAgent) return true;

            PrototypeId powerRef = Properties[PropertyEnum.CreatorPowerPrototype];
            if (powerRef != PrototypeId.Invalid)
            {
                var powerProto = GameDatabase.GetPrototype<SummonPowerPrototype>(powerRef);
                if (powerProto != null)
                    return powerProto.IsPetSummoningPower();
            }

            return false;
        }

        #endregion

        public override bool ProcessEntityAction(EntitySelectorActionPrototype action)
        {
            if (IsControlledEntity || EntityActionComponent == null) return false;

            if (action.SpawnerTrigger != PrototypeId.Invalid)
                TriggerLocalSpawner(action.SpawnerTrigger);

            if (action.AttributeActions.HasValue())
                foreach (var attr in action.AttributeActions)
                    switch (attr)
                    {
                        case EntitySelectorAttributeActions.DisableInteractions:
                            Properties[PropertyEnum.EntSelActInteractOptDisabled] = true; break;
                        case EntitySelectorAttributeActions.EnableInteractions:
                            Properties[PropertyEnum.EntSelActInteractOptDisabled] = false; break;
                    }

            var aiOverride = action.PickAIOverride(Game.Random);
            if (aiOverride != null && aiOverride.SelectorReferencedPowerRemove)
            {
                foreach (var powerRef in EntityActionComponent.PerformPowers)
                    UnassignPower(powerRef);
                EntityActionComponent.PerformPowers.Clear();

                // clear aggro range override
                if (AIController != null)
                {
                    var collection = AIController.Blackboard.PropertyCollection;
                    collection.RemoveProperty(PropertyEnum.AIAggroRangeOverrideAlly);
                    collection.RemoveProperty(PropertyEnum.AIAggroRangeOverrideHostile);
                    collection.RemoveProperty(PropertyEnum.AIProximityRangeOverride);
                }
            }

            if (IsInWorld)
            {
                var overheadText = action.PickOverheadText(Game.Random);
                if (overheadText != null)
                    ShowOverheadText(overheadText.Text, (float)TimeSpan.FromMilliseconds(overheadText.Duration).TotalSeconds);

                if (aiOverride != null)
                {
                    var powerRef = aiOverride.Power;
                    if (powerRef != PrototypeId.Invalid)
                    {
                        if (aiOverride.PowerRemove)
                        {
                            UnassignPower(powerRef);
                            EntityActionComponent.PerformPowers.Remove(powerRef);
                        }
                        else
                        {
                            var result = ActivatePerformPower(powerRef);
                            if (result == PowerUseResult.Success)
                                EntityActionComponent.PerformPowers.Add(powerRef);
                            else
                                Logger.Warn($"ProcessEntityAction ActivatePerformPower [{powerRef}] = {result}");
                            if (result == PowerUseResult.OwnerNotSimulated) return false;
                        }
                    }
                    if (aiOverride.BrainRemove)
                    {
                        AIController?.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFullOverride);
                        Properties.RemoveProperty(PropertyEnum.AllianceOverride);
                    }
                }

                if (action.AllianceOverride != PrototypeId.Invalid)
                    Properties[PropertyEnum.AllianceOverride] = action.AllianceOverride;
            }

            if (aiOverride != null)
            {
                // override AI

                var brainRef = aiOverride.Brain;
                if (brainRef != PrototypeId.Invalid)
                {
                    if (AIController == null)
                    {
                        var brain = GameDatabase.GetPrototype<BrainPrototype>(brainRef);
                        if (brain is not ProceduralAIProfilePrototype profile) return false;
                        using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                        InitAIOverride(profile, properties);
                        if (AIController == null) return false;
                        AIController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIFullOverride);
                    }
                    else
                        AIController.Blackboard.PropertyCollection[PropertyEnum.AIFullOverride] = brainRef;
                }

                var collection = AIController.Blackboard.PropertyCollection;
                if (collection != null) 
                {
                    // set aggro range override
                    if (aiOverride.AIAggroRangeOverrideAlly > 0)
                        collection[PropertyEnum.AIAggroRangeOverrideAlly] = (float)aiOverride.AIAggroRangeOverrideAlly;
                    if (aiOverride.AIAggroRangeOverrideEnemy > 0)
                        collection[PropertyEnum.AIAggroRangeOverrideHostile] = (float)aiOverride.AIAggroRangeOverrideEnemy;
                    if (aiOverride.AIProximityRangeOverride > 0)
                        collection[PropertyEnum.AIProximityRangeOverride] = (float)aiOverride.AIProximityRangeOverride;
                }
                
                if (aiOverride.LifespanEndPower != PrototypeId.Invalid) // not used
                    Properties[PropertyEnum.Proc, (int)ProcTriggerType.OnLifespanExpired, aiOverride.LifespanEndPower] = 1.0f;

                if (aiOverride.LifespanMS > -1) // not used
                {
                    var lifespan = GetRemainingLifespan();
                    var reset = TimeSpan.FromMilliseconds(aiOverride.LifespanMS);
                    if (lifespan == TimeSpan.Zero || reset < lifespan)
                        ResetLifespan(reset);
                }  
            }

            if (action.Rewards.HasValue())
            {
                List<Player> playerList = ListPool<Player>.Instance.Get();
                Power.ComputeNearbyPlayers(Region, RegionLocation.Position, 0, false, playerList);

                List<(PrototypeId, LootActionType)> tables = ListPool<(PrototypeId, LootActionType)>.Instance.Get();
                foreach (var lootTableProtoRef in action.Rewards)
                {
                    if (lootTableProtoRef == PrototypeId.Invalid)
                        continue;

                    tables.Add((lootTableProtoRef, LootActionType.Spawn));
                }

                int recipientId = 1;
                foreach (Player player in playerList)
                {
                    using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                    inputSettings.Initialize(LootContext.Drop, player, this, CharacterLevel);
                    Game.LootManager.AwardLootFromTables(tables, inputSettings, recipientId++);
                }

                ListPool<Player>.Instance.Return(playerList);
                ListPool<(PrototypeId, LootActionType)>.Instance.Return(tables);
            }

            if (action.BroadcastEvent != PrototypeId.Invalid)
            {
                var broadcastEventProto = GameDatabase.GetPrototype<EntityActionEventBroadcastPrototype>(action.BroadcastEvent);
                if (broadcastEventProto != null)
                {
                    var region = Region;
                    if (region == null || broadcastEventProto.BroadcastRange > Vector3.LengthTest(region.Aabb.Extents)) 
                        return false;

                    var volume = new Sphere(RegionLocation.Position, broadcastEventProto.BroadcastRange);
                    foreach (var entity in region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.ActivePartition)))
                        entity.TriggerEntityActionEvent(broadcastEventProto.EventToBroadcast);
                }
            }

            return true;
        }

        public void DrawPath(EntityHelper.TestOrb orbRef)
        {
            if (EntityHelper.DebugOrb == false) return;
            if (Locomotor.HasPath)
                foreach(var node in Locomotor.LocomotionState.PathNodes)
                    EntityHelper.CrateOrb(orbRef, node.Vertex, Region);
        }

        public void StartHitReactionCooldown()
        {
            _hitReactionCooldownEnd = Game.CurrentTime + TimeSpan.FromMilliseconds(AgentPrototype.HitReactCooldownMS);
        }

        public bool IsHitReactionOnCooldown()
        {
            return _hitReactionCooldownEnd > Game.CurrentTime;
        }

        #region Scheduled Events

        private void ScheduleRandomWakeStart(int wakeRandomStartMS)
        {
            if (!_wakeStartEvent.IsValid)
            {
                TimeSpan randomStart = TimeSpan.FromMilliseconds(Game.Random.Next(wakeRandomStartMS));
                ScheduleEntityEvent(_wakeStartEvent, randomStart);
            }
        }

        private void WakeStartCallback()
        {
            Properties[PropertyEnum.Dormant] = false;
        }

        private void СheckWakeDelay()
        {
            var prototype = AgentPrototype;
            if (prototype == null) return;

            if (prototype.WakeDelayMS > 0 
                && prototype.PlayDramaticEntrance != DramaticEntranceType.Never
                && Properties[PropertyEnum.DramaticEntrancePlayedOnce] == false)
            {
                TimeSpan wakeDelay = TimeSpan.FromMilliseconds(prototype.WakeDelayMS);
                if (wakeDelay > TimeSpan.Zero)
                {
                    if (_wakeEndEvent.IsValid)
                    {
                        var scheduler = Game?.GameEventScheduler;
                        if (Game.CurrentTime + wakeDelay < _wakeEndEvent.Get().FireTime)
                            scheduler?.RescheduleEvent(_wakeEndEvent, wakeDelay);
                    }
                    else
                        ScheduleEntityEvent(_wakeEndEvent, wakeDelay);
                }
            }
            else
                TryAutoActivatePowersInCollection();
        }

        private void WakeEndCallback()
        {
            RegisterForPendingPhysicsResolve();
            OnDramaticEntranceEnd();
            var prototype = AgentPrototype;
            if (prototype != null && prototype.PlayDramaticEntrance == DramaticEntranceType.Once)
                Properties[PropertyEnum.DramaticEntrancePlayedOnce] = true;

            Region?.EntityLeaveDormantEvent.Invoke(new(this));
            TryAutoActivatePowersInCollection();
        }

        protected class WakeStartEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Agent)?.WakeStartCallback();
        }

        protected class WakeEndEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t as Agent)?.WakeEndCallback();
        }

        private class ExitCombatEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Agent)t).ExitCombat();
        }

        private class MovementStartedEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((WorldEntity)t).TryActivateOnMovementStartedProcs();
        }

        private class MovementStoppedEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((WorldEntity)t).TryActivateOnMovementStoppedProcs();
        }

        private class RespawnControlledAgentEvent : CallMethodEvent<Entity>
        {
            protected override CallbackDelegate GetCallback() => (t) => ((Agent)t).RespawnControlledAgent();
        }

        #endregion
    }
}
