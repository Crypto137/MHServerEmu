using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Agent : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AIController AIController { get; private set; }
        public AgentPrototype AgentPrototype { get => Prototype as AgentPrototype; }
        public override bool IsTeamUpAgent { get => AgentPrototype is AgentTeamUpPrototype; }

        public override bool IsSummonedPet
        {
            get
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
        }

        public override bool CanRotate
        {
            get
            {
                Player ownerPlayer = GetOwnerOfType<Player>();
                if ( IsInKnockback || IsInKnockdown || IsInKnockup 
                    || IsImmobilized || IsImmobilizedByHitReact || IsSystemImmobilized 
                    || IsStunned || IsMesmerized ||
                    (ownerPlayer != null && (ownerPlayer.IsFullscreenMoviePlaying || ownerPlayer.IsOnLoadingScreen))
                    || NPCAmbientLock)
                    return false;
                return true;
            }
        }

        public override bool CanMove
        {
            get 
            {
                Player ownerPlayer = GetOwnerOfType<Player>();
                if (base.CanMove == false || HasMovementPreventionStatus || IsSystemImmobilized 
                    || (ownerPlayer != null && (ownerPlayer.IsFullscreenMoviePlaying || ownerPlayer.IsOnLoadingScreen)))
                    return false;
                
                Power power = GetThrowablePower();
                if (power != null && power.PrototypeDataRef != ActivePowerRef)
                    return false;

                return true; 
            }
        }

        public override void OnLocomotionStateChanged(LocomotionState oldState, LocomotionState newState)
        {
            base.OnLocomotionStateChanged(oldState, newState);

            if(IsSimulated && IsInWorld && TestStatus(EntityStatus.ExitingWorld) == false)
            {
                if((oldState.Method == LocomotorMethod.HighFlying) != (newState.Method == LocomotorMethod.HighFlying))
                {
                    Vector3 currentPosition = RegionLocation.Position;
                    Vector3 targetPosition = FloorToCenter(RegionLocation.ProjectToFloor(RegionLocation.Region, RegionLocation.Cell, currentPosition));
                    ChangeRegionPosition(targetPosition, null, ChangePositionFlags.NoSendToOwner | ChangePositionFlags.HighFlying);
                }
            }
        }


        public bool HasPowerPreventionStatus
            => IsInKnockback 
            || IsInKnockdown 
            || IsInKnockup 
            || IsStunned 
            || IsMesmerized 
            || NPCAmbientLock 
            || IsInPowerLock;

        public AssetId OriginalWorldAsset { get; private set; }

        public Agent(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            var agentProto = GameDatabase.GetPrototype<AgentPrototype>(settings.EntityRef);
            if (agentProto == null) return false;
            if (agentProto.Locomotion.Immobile == false) Locomotor = new();

            // GetPowerCollectionAllocateIfNull()
            base.Initialize(settings);

            // InitPowersCollection
            InitLocomotor(settings.LocomotorHeightOverride);

            return true;
        }

        private bool InitAI(EntitySettings settings)
        {
            var agentPrototype = AgentPrototype;
            if (agentPrototype == null || Game == null || this is Avatar) return false;

            var behaviorProfile = agentPrototype.BehaviorProfile;
            if (behaviorProfile != null && behaviorProfile.Brain != PrototypeId.Invalid)
            {
                AIController = new(Game, this);
                PropertyCollection collection = new ();
                collection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] = Properties[PropertyEnum.AIIgnoreNoTgtOverrideProfile];
                SpawnSpec spec = settings?.SpawnSpec ?? new SpawnSpec();
                return AIController.Initialize(behaviorProfile, spec, collection);
            }
            return false;
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

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);
            RegionLocation.Cell.EnemySpawn(); // Calc Enemy
            // ActivePowerRef = settings.PowerPrototype

            if (AIController != null) 
            {
                var behaviorProfile = AgentPrototype?.BehaviorProfile;
                if (behaviorProfile == null) return;
                AIController.Initialize(behaviorProfile, null, null);
            }
            else InitAI(settings);

            if (AIController != null)
            {
                AIController.OnAIEnteredWorld();
                ActivateAI();
            }
        }

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

        public override void OnKilled(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            // TODO other events

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

        public void Think()
        {
            AIController?.Think();
        }

        public override void OnExitedWorld()
        {
            base.OnExitedWorld();
            AIController?.OnAIExitedWorld();
        }

        public override void OnDeallocate()
        {
            AIController?.OnAIDeallocate();
            base.OnDeallocate();
        }

        public override void AppendStartAction(PrototypeId actionsTarget) // TODO rewrite this
        {
            bool startAction = false;

            if (EntityActionComponent != null && EntityActionComponent.ActionTable.TryGetValue(EntitySelectorActionEventType.OnSimulated, out var actionSet))
                startAction = AppendSelectorActions(actionSet);
            if (startAction == false && actionsTarget != PrototypeId.Invalid)
                AppendOnStartActions(actionsTarget);
        }

        private bool AppendStartPower(PrototypeId startPowerRef)
        {
            if (startPowerRef == PrototypeId.Invalid) return false;
            //Console.WriteLine($"[{Id}]{GameDatabase.GetPrototypeName(startPowerRef)}");

            Condition condition = new();
            condition.InitializeFromPowerMixinPrototype(1, startPowerRef, 0, TimeSpan.Zero);
            condition.StartTime = Clock.GameTime;
            _conditionCollection.AddCondition(condition);

            AssignPower(startPowerRef, new());
            
            return true;
        }

        public bool AppendOnStartActions(PrototypeId targetRef)
        {
            if (GameDatabase.InteractionManager.GetStartAction(PrototypeDataRef, targetRef, out MissionActionEntityPerformPowerPrototype action))
                return AppendStartPower(action.PowerPrototype);
            return false;
        }

        public bool AppendSelectorActions(HashSet<EntitySelectorActionPrototype> actions)
        {
            var action = actions.First();
            if (action.AIOverrides.HasValue())
            {
                int index = Game.Random.Next(0, action.AIOverrides.Length);
                var actionAIOverrideRef = action.AIOverrides[index];
                if (actionAIOverrideRef == PrototypeId.Invalid) return false;
                var actionAIOverride = actionAIOverrideRef.As<EntityActionAIOverridePrototype>();
                if (actionAIOverride != null) return AppendStartPower(actionAIOverride.Power);
            }
            return false;
        }

        public virtual bool HasPowerInPowerProgression(PrototypeId powerRef)
        {
            if (IsTeamUpAgent)
                return GameDataTables.Instance.PowerOwnerTable.GetTeamUpPowerProgressionEntry(PrototypeDataRef, powerRef) != null;

            return false;
        }

        public virtual bool GetPowerProgressionInfo(PrototypeId powerProtoRef, out PowerProgressionInfo info)
        {
            // Note: this implementation is meant only for team-up agents

            info = new();

            if (powerProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): powerProtoRef == PrototypeId.Invalid");

            var teamUpProto = PrototypeDataRef.As<AgentTeamUpPrototype>();
            if (teamUpProto == null)
                return Logger.WarnReturn(false, "GetPowerProgressionInfo(): teamUpProto == null");

            var powerProgressionEntry = GameDataTables.Instance.PowerOwnerTable.GetTeamUpPowerProgressionEntry(teamUpProto.DataRef, powerProtoRef);
            if (powerProgressionEntry != null)
                info.InitForTeamUp(powerProgressionEntry);
            else
                info.InitNonProgressionPower(powerProtoRef);

            return info.IsValid;
        }

        public int GetPowerRank(PrototypeId powerRef)
        {
            if (powerRef == PrototypeId.Invalid) return 0;
            return Properties[PropertyEnum.PowerRankCurrentBest, powerRef];
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

        private void ScheduleRandomWakeStart(int wakeRandomStartMS)
        {
            throw new NotImplementedException();
        }

        public override void OnDramaticEntranceEnd()
        {
            base.OnDramaticEntranceEnd();
            AIController?.OnAIDramaticEntranceEnd();
        }

        public InventoryResult CanEquip(Item item, ref PropertyEnum propertyRestriction)
        {
            // TODO
            return InventoryResult.Success;     // Bypass property restrictions
        }

        protected override bool InitInventories(bool populateInventories)
        {
            // TODO
            return base.InitInventories(populateInventories);
        }

        public IsInPositionForPowerResult IsInPositionForPower(Power power, WorldEntity target, Vector3 targetPosition)
        {
            var targetingProto = power.TargetingStylePrototype;
            if (targetingProto == null) return IsInPositionForPowerResult.Error;

            if (targetingProto.TargetingShape == TargetingShapeType.Self)
                return IsInPositionForPowerResult.Success;

            if (power.IsOnExtraActivation)
                return IsInPositionForPowerResult.Success;

            if (power.IsOwnerCenteredAOE && (targetingProto.MovesToRangeOfPrimaryTarget == false || target == null))
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

            if (power.RequiresLineOfSight)
            {
                ulong targetId = (target != null ? target.Id : InvalidId);
                if (power.PowerLOSCheck(RegionLocation, position, targetId, out _, power.LOSCheckAlongGround) == false)
                    return IsInPositionForPowerResult.NoPowerLOS;
            }

            if (power.Prototype is SummonPowerPrototype summonPowerProto)
            {
                var summonedProto = summonPowerProto.GetSummonEntity(0, OriginalWorldAsset);
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
                    if (region.IsLocationClear(bounds, pathFlags, PositionCheckFlags.CheckCanBlockedEntity) == false)
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

        private static bool IsInRangeToActivatePower(Power power, WorldEntity target, Vector3 position)
        {
            if (target != null && power.AlwaysTargetsMousePosition)
            {
                if (target.IsInWorld == false) return false;
                return power.IsInRange(target, RangeCheckType.Activation);
            }
            else if (power.IsMelee)
                return true;

            return power.IsInRange(position, RangeCheckType.Activation);
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
                targetId = Id;
            else
                if (IsInWorld == false) return PowerUseResult.RestrictiveCondition;

            var triggerResult = CanTriggerPower(powerProto, power, flags);
            if (triggerResult != PowerUseResult.Success)
                return triggerResult;

            if (power.IsExclusiveActivation)
            {
                if (IsExecutingPower)
                {
                    var activePower = GetPower(ActivePowerRef);
                    if (activePower == null)
                    {
                        Logger.Warn($"Agent has m_activePowerRef set, but is missing the power in its power collection! Power: [{GameDatabase.GetPrototypeName(ActivePowerRef)}] Agent: [{this}]");
                        return PowerUseResult.PowerInProgress;
                    }

                    if (activePower.IsTravelPower)
                    {
                        if (activePower.IsEnding == false)
                            activePower.EndPower(EndFlag.ExplicitCancel | EndFlag.Interrupting);
                    }
                    else
                        return PowerUseResult.PowerInProgress;
                }

                if (Game == null) return PowerUseResult.GenericError;
                var activateTime = Game.CurrentTime - power.LastActivateGameTime;
                var animationTime = power.AnimationTime;
                if (activateTime < animationTime)
                    return PowerUseResult.MinimumReactivateTime;
            }

            WorldEntity target = null;
            if (targetId != InvalidId)
                target = Game.EntityManager.GetEntity<WorldEntity>(targetId);

            if (power.IsItemPower)
            {
                if (itemSourceId == InvalidId)
                {
                    Logger.Warn($"Power is an ItemPower but no itemSourceId specified - {power}");
                    return PowerUseResult.ItemUseRestricted;
                }

                var item = Game.EntityManager.GetEntity<Item>(itemSourceId);
                if (item == null) return PowerUseResult.ItemUseRestricted;

                var powerUse = flags.HasFlag(PowerActivationSettingsFlags.Flag7) == false;
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

        internal bool StartThrowing(ulong entityId)
        {
            throw new NotImplementedException();
        }

        public override PowerUseResult ActivatePower(Power power, in PowerActivationSettings powerSettings)
        {
            var result = base.ActivatePower(power, powerSettings);
            if (result != PowerUseResult.Success && result != PowerUseResult.ExtraActivationFailed)
            {
                Logger.Warn($"Power [{power}] for entity [{this}] failed to properly activate. Result = {result}");
                ActivePowerRef = PrototypeId.Invalid;
            }
            else if (power.IsExclusiveActivation)
            {
                if (IsInWorld)
                    ActivePowerRef = power.PrototypeDataRef;
                else
                    Logger.Warn($"Trying to set the active power for an Agent that is not in the world. " +
                        $"Check to see if there's *anything* that can happen in the course of executing the power that can take them out of the world.\n Agent: {this}");
            }
            return result;
        }
    }

    public enum IsInPositionForPowerResult
    {
        Error,
        Success,
        BadTargetPosition,
        OutOfRange,
        NoPowerLOS
    }
}
