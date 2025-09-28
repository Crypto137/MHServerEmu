﻿using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public partial class Power : IKeyworded
    {
        private const float PowerPositionSweepPadding = Locomotor.MovementSweepPadding;
        private const float PowerPositionSweepPaddingSquared = PowerPositionSweepPadding * PowerPositionSweepPadding;

        private static readonly TimeSpan CheckIfTargetIsKilledInterval = TimeSpan.FromMilliseconds(250);
        private static readonly Logger Logger = LogManager.CreateLogger();

        private SituationalPowerComponent _situationalComponent;
        private KeywordsMask _keywordsMask;

        private PowerActivationPhase _activationPhase = PowerActivationPhase.Inactive;
        private PowerActivationSettings _lastActivationSettings;

        private bool _hasDelayedActivationSettings = false;
        private PowerActivationSettings _delayedActivationSettings;

        private readonly List<TrackedCondition> _trackedConditionList = new();
        private PowerIndexPropertyFlags _trackedConditionIndexPropertyFlags = PowerIndexPropertyFlags.None;

        protected EventGroup _pendingEvents = new();
        private readonly EventGroup _pendingActivationPhaseEvents = new();
        private readonly EventGroup _pendingPowerApplicationEvents = new();

        private readonly EventPointer<DelayedActivationEvent> _delayedActivationEvent = new();
        private readonly EventPointer<StopChargingEvent> _stopChargingEvent = new();
        private readonly EventPointer<StartChannelingEvent> _startChannelingEvent = new();
        private readonly EventPointer<StopChannelingEvent> _stopChannelingEvent = new();
        private readonly EventPointer<EndCooldownEvent> _endCooldownEvent = new();
        private readonly EventPointer<PowerSubsequentActivationTimeoutEvent> _subsequentActivationTimeoutEvent = new();
        private readonly EventPointer<EndPowerEvent> _endPowerEvent = new();
        private readonly EventPointer<PayRecurringCostEvent> _payRecurringCostEvent = new();
        private readonly EventPointer<ReapplyIndexPropertiesEvent> _reapplyIndexPropertiesEvent = new();
        private readonly EventPointer<CancelIfTargetIsKilledEvent> _cancelIfTargetIsKilledEvent = new();

        private List<EventPointer<ScheduledActivateEvent>> _scheduledActivateEventList;     // Initialized on demand

        public Game Game { get; }
        public PrototypeId PrototypeDataRef { get; }
        public PowerPrototype Prototype { get; }
        public TargetingStylePrototype TargetingStylePrototype { get; }

        public WorldEntity Owner { get; private set; }
        public bool IsTeamUpPassivePowerWhileAway { get; private set; }
        public PropertyCollection Properties { get; } = new();
        public KeywordsMask KeywordsMask { get => _keywordsMask; }

        public float AnimSpeedCache { get; private set; } = -1f;
        public bool WasLastActivateInterrupted { get; private set; }
        public TimeSpan LastActivateGameTime { get; private set; }
        public PowerActivationSettings LastActivationSettings { get => _lastActivationSettings; }

        public bool IsSituationalPower { get => _situationalComponent != null; }
        public bool IsControlPower { get => Prototype != null && Prototype.IsControlPower; }

        public int Rank { get => Properties[PropertyEnum.PowerRank]; set => Properties[PropertyEnum.PowerRank] = value; }

        public bool IsInActivation { get => _activationPhase == PowerActivationPhase.Active; }
        public bool IsChanneling { get => _activationPhase == PowerActivationPhase.Channeling || _activationPhase == PowerActivationPhase.LoopEnding; }
        public bool IsEnding { get => _activationPhase == PowerActivationPhase.MinTimeEnding || _activationPhase == PowerActivationPhase.LoopEnding; }
        public bool IsCharging { get => _activationPhase == PowerActivationPhase.Charging; }
        public bool IsActive { get => IsInActivation || IsToggledOn() || IsChanneling || IsCharging || IsEnding || _activationPhase == PowerActivationPhase.ChannelStarting; }

        public Power(Game game, PrototypeId prototypeDataRef)
        {
            Game = game;
            PrototypeDataRef = prototypeDataRef;
            Prototype = prototypeDataRef.As<PowerPrototype>();

            TargetingStylePrototype = Prototype.TargetingStyle.As<TargetingStylePrototype>();
        }

        public override string ToString()
        {
            return $"powerProtoRef={GameDatabase.GetPrototypeName(PrototypeDataRef)}, owner={Owner}";
        }

        public bool Initialize(WorldEntity owner, bool isTeamUpPassivePowerWhileAway, PropertyCollection initializeProperties)
        {
            Owner = owner;
            IsTeamUpPassivePowerWhileAway = isTeamUpPassivePowerWhileAway;

            if (Prototype == null)
                return Logger.WarnReturn(false, $"Initialize(): Prototype == null");

            GeneratePowerProperties(Properties, Prototype, initializeProperties, Owner);
            CreateSituationalComponent();

            return true;
        }

        public bool OnAssign()
        {
            // Initialize situational component
            if (_situationalComponent != null)
            {
                _situationalComponent.Initialize();
                _situationalComponent.OnPowerAssigned();
            }

            // Initialize keywords
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "OnAssign(): powerProto == null");

            _keywordsMask = Prototype.KeywordsMask.Copy<KeywordsMask>();

            // Apply keyword changes from the owner avatar
            WorldEntity owner = Owner;
            if (owner is not Avatar)
            {
                owner = GetUltimateOwner();
                if (owner == null || owner is not Avatar)
                    return true;
            }

            // V48_TODO: Can we remove this section since there are no keyword changes?

            TimeSpan cooldownTimeRemaining = GetCooldownTimeRemaining();
            if (cooldownTimeRemaining > TimeSpan.Zero)
            {
                // Restore saved cooldown that is still in progress.
                StartCooldown(cooldownTimeRemaining);
            }
            else
            {
                // One or more cooldown cycles may have finished while the owner was not in the world
                // (cooldown ran out during transition, when the player swapped to another avatar, etc.).
                // If not handled, this will break the cooldown cycle and prevent charges from replenishing.
                int powerChargesMax = Owner.GetPowerChargesMax(PrototypeDataRef);
                if (powerChargesMax > 0)
                {
                    int powerChargesAvailable = Owner.GetPowerChargesAvailable(PrototypeDataRef);
                    if (powerChargesAvailable < powerChargesMax)
                    {
                        TimeSpan cooldownDuration = default;
                        TimeSpan cooldownTimeElapsed = Owner.GetAbilityCooldownTimeElapsed(Prototype);

                        if (cooldownTimeElapsed != TimeSpan.Zero)
                        {
                            // Replenish charges that would have been replenished if the cooldown continued to cycle normally
                            cooldownDuration = GetCooldownDuration();
                            float numCooldowns = (float)(cooldownTimeElapsed.TotalMilliseconds / cooldownDuration.TotalMilliseconds);
                            numCooldowns = MathF.Min(numCooldowns, powerChargesMax - powerChargesAvailable);
                            Owner.Properties.AdjustProperty((int)numCooldowns, new(PropertyEnum.PowerChargesAvailable, PrototypeDataRef));

                            // Restore the current cooldown from remaining time
                            cooldownDuration *= 1f - (numCooldowns - MathF.Floor(numCooldowns));
                        }

                        StartCooldown(cooldownDuration);
                    }
                }
            }

            return true;
        }

        public void OnUnassign()
        {
            if (Owner != null && Owner.TestStatus(EntityStatus.ExitingWorld) == false)
            {
                // Remove conditions from flagged powers and non-proc power progression powers
                bool removeConditions = Prototype.CancelConditionsOnUnassign;
                if (removeConditions == false && IsProcEffect() == false)
                {
                    removeConditions = Owner.PowerCollection.ContainsPower(PrototypeDataRef, true);

                    // Do not remove conditions from powers triggered by proc effects
                    if (removeConditions && IsComboEffect())
                    {
                        PrototypeId triggeringPowerProtoRef = Properties[PropertyEnum.TriggeringPowerRef, PrototypeDataRef];
                        PowerPrototype triggeringPowerProto = triggeringPowerProtoRef.As<PowerPrototype>();
                        if (triggeringPowerProto != null && IsProcEffect(triggeringPowerProto))
                            removeConditions = false;
                    }
                }

                if (removeConditions)
                    RemoveTrackedConditions(false);
            }

            _situationalComponent?.Shutdown();

            EndPowerFlags endPowerFlags = EndPowerFlags.ExplicitCancel | EndPowerFlags.Unassign;
            if (Owner.TestStatus(EntityStatus.ExitingWorld))
                endPowerFlags |= EndPowerFlags.ExitWorld;

            EndPower(endPowerFlags);

            Owner?.Properties.RemoveProperty(new(PropertyEnum.PowerActivationCount, PrototypeDataRef));
        }

        public void OnEquipped()
        {
            if (IsNormalPower() == false || Owner == null) return;

            if (IsSituationalPower)
                _situationalComponent.OnPowerEquipped();

            if (IsControlPower && Owner is Avatar avatar)
            {
                var controlledAgent = avatar.ControlledAgent;
                if (controlledAgent != null && controlledAgent.CanSummonControlledAgent())
                    avatar.SummonControlledAgentWithDuration();
            }
        }

        public void OnUnequipped()
        {
            if (IsNormalPower() == false || Owner == null) return;

            if (IsControlPower && Owner is Avatar avatar)
            {
                var controlledAgent = avatar.ControlledAgent;
                if (controlledAgent != null && controlledAgent.IsAliveInWorld)
                {
                    controlledAgent.KillSummonedOnOwnerDeath();
                    controlledAgent.ExitWorld();
                }
            }
        }

        public void OnOwnerEnteredWorld()
        {
            _situationalComponent?.Initialize();
        }

        public void OnOwnerExitedWorld()
        {
            if (Prototype.CancelConditionsOnExitWorld)
                RemoveTrackedConditions(false);

            // Immediately finish the chain of subsequent activations
            if (_subsequentActivationTimeoutEvent.IsValid)
            {
                CancelExtraActivationTimeout();
                ExtraActivateTimeoutCallback();
            }

            _situationalComponent?.Shutdown();
        }

        public void OnOwnerCastSpeedChange()
        {
            // Reset animation speed cache when owner cast speed changes
            AnimSpeedCache = -1f;
        }

        public void OnOwnerLevelChange()
        {
            Properties[PropertyEnum.CharacterLevel] = Owner.CharacterLevel;
            Properties[PropertyEnum.CombatLevel] = Owner.CombatLevel;

            // Reapplication needs to be deferred for cases like controlled entities that change level before they become owned by players
            ScheduleIndexPropertiesReapplication(PowerIndexPropertyFlags.CharacterLevel | PowerIndexPropertyFlags.CombatLevel);
        }

        public virtual void OnDeallocate()
        {
            if (_activationPhase != PowerActivationPhase.Inactive)
                Logger.Warn($"The following Power is being destructed while still in an ActivationPhase other than Inactive!\nPower: [{this}]\nOwner: [{Owner}]");

            Game.GameEventScheduler.CancelAllEvents(_pendingEvents);
            Game.GameEventScheduler.CancelAllEvents(_pendingActivationPhaseEvents);
            Game.GameEventScheduler.CancelAllEvents(_pendingPowerApplicationEvents);

            _situationalComponent?.Destroy();
        }

        public static void GeneratePowerProperties(PropertyCollection primaryCollection, PowerPrototype powerProto, PropertyCollection initializeProperties, WorldEntity owner)
        {
            // Start with a clean copy from the prototype
            if (powerProto.Properties != null)
                primaryCollection.FlattenCopyFrom(powerProto.Properties, true);

            // Add properties from the collection passed in the Initialize() method if we have one
            if (initializeProperties != null)
                primaryCollection.FlattenCopyFrom(initializeProperties, false);

            // Set properties for all keywords assigned in the prototype
            if (powerProto.Keywords != null)
            {
                foreach (PrototypeId keywordRef in powerProto.Keywords)
                    primaryCollection[PropertyEnum.HasPowerKeyword, keywordRef] = true;
            }

            // Run evals
            if (powerProto.EvalOnCreate.HasValue())
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = owner.Game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, primaryCollection);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, owner);

                Eval.InitTeamUpEvalContext(evalContext, owner);

                foreach (EvalPrototype evalProto in powerProto.EvalOnCreate)
                {
                    if (Eval.RunBool(evalProto, evalContext) == false)
                        Logger.Warn($"GeneratePowerProperties(): The following EvalOnCreate Eval in a power failed:\nEval: [{evalProto.ExpressionString()}]\nPower: [{powerProto}]");
                }
            }

            if (powerProto.EvalPowerSynergies != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = owner.Game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, primaryCollection);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, owner.Properties);
                evalContext.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, owner?.ConditionCollection);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var2, owner);

                Eval.InitTeamUpEvalContext(evalContext, owner);

                if (Eval.RunBool(powerProto.EvalPowerSynergies, evalContext) == false)
                    Logger.Warn($"GeneratePowerProperties(): The EvalPowerSynergies in a power failed:\nPower: [{powerProto}]");
            }
        }

        public static void CopyPowerIndexProperties(PropertyCollection source, PropertyCollection destination)
        {
            destination.CopyProperty(source, PropertyEnum.PowerRank);
            destination.CopyProperty(source, PropertyEnum.CharacterLevel);
            destination.CopyProperty(source, PropertyEnum.CombatLevel);
            destination.CopyProperty(source, PropertyEnum.ItemLevel);
            destination.CopyProperty(source, PropertyEnum.ItemVariation);
        }

        public PowerIndexProperties GetIndexProperties()
        {
            return new(Properties[PropertyEnum.PowerRank],
                       Properties[PropertyEnum.CharacterLevel],
                       Properties[PropertyEnum.CombatLevel],
                       Properties[PropertyEnum.ItemLevel],
                       Properties[PropertyEnum.ItemVariation]);
        }

        public void RestampIndexProperties(in PowerIndexProperties indexProps)
        {
            Properties[PropertyEnum.PowerRank] = indexProps.PowerRank;
            Properties[PropertyEnum.CharacterLevel] = indexProps.CharacterLevel;
            Properties[PropertyEnum.CombatLevel] = indexProps.CombatLevel;
            Properties[PropertyEnum.ItemLevel] = indexProps.ItemLevel;
            Properties[PropertyEnum.ItemVariation] = indexProps.ItemVariation;
        }

        public void ScheduleIndexPropertiesReapplication(PowerIndexPropertyFlags indexPropertyFlags)
        {
            // If the owner is not simulated there will not be any activation in progress
            if (Owner.IsSimulated)
            {
                if (_reapplyIndexPropertiesEvent.IsValid)
                {
                    _reapplyIndexPropertiesEvent.Get().Flags |= indexPropertyFlags;
                }
                else
                {
                    EventScheduler scheduler = Game.GameEventScheduler;
                    scheduler.ScheduleEvent(_reapplyIndexPropertiesEvent, TimeSpan.Zero, _pendingEvents);
                    _reapplyIndexPropertiesEvent.Get().Initialize(this, indexPropertyFlags);
                }
            }

            // Check triggered powers
            if (Prototype.ActionsTriggeredOnPowerEvent.HasValue())
            {
                foreach (PowerEventActionPrototype actionProto in Prototype.ActionsTriggeredOnPowerEvent)
                {
                    if (actionProto.EventAction != PowerEventActionType.UsePower || actionProto.Power == PrototypeId.Invalid)
                        continue;

                    Power triggeredPower = Owner.GetPower(actionProto.Power);
                    if (triggeredPower == null)
                    {
                        Logger.Warn("ScheduleIndexPropertiesReapplication(): triggeredPower == null");
                        continue;
                    }
                    
                    if (triggeredPower == this)
                    {
                        Logger.Warn($"ScheduleIndexPropertiesReapplication(): Recursion detected for {this}");
                        continue;
                    }

                    triggeredPower.Rank = Rank;
                    triggeredPower.ScheduleIndexPropertiesReapplication(indexPropertyFlags | PowerIndexPropertyFlags.PowerRank);
                }
            }
        }

        private void ReapplyIndexProperties(PowerIndexPropertyFlags indexPropertyFlags)
        {
            //Logger.Debug($"ReapplyIndexProperties(): {this} - {indexPropertyFlags}");

            PowerPrototype powerProto = Prototype;

            // Rerun creation evals
            if (powerProto.EvalOnCreate.HasValue())
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var1, Owner);

                Eval.InitTeamUpEvalContext(evalContext, Owner);

                foreach (EvalPrototype evalProto in Prototype.EvalOnCreate)
                {
                    if (Eval.RunBool(evalProto, evalContext) == false)
                        Logger.Warn($"ReapplyIndexProperties(): The following EvalOnCreate Eval in a power failed:\nEval: [{evalProto.ExpressionString()}]\nPower: [{Prototype}]");
                }
            }

            // Do not notify about index prop changes for orbs (they can change their level when they shrink)
            if (Owner.IsVacuumable == false)
            {
                Player playerOwner = Owner.GetOwnerOfType<Player>();
                if (playerOwner != null)
                {
                    PowerIndexProperties indexProperties = GetIndexProperties();

                    var updatePropsMessage = NetMessageUpdatePowerIndexProps.CreateBuilder()
                        .SetEntityId(Owner.Id)
                        .SetPowerProtoId((ulong)PrototypeDataRef)
                        .SetPowerRank(indexProperties.CombatLevel)
                        .SetCharacterLevel(indexProperties.CharacterLevel)
                        .SetCombatLevel(indexProperties.CombatLevel)
                        .SetItemLevel(indexProperties.ItemLevel)
                        .SetItemVariation(indexProperties.ItemVariation)
                        .Build();

                    playerOwner.SendMessage(updatePropsMessage);
                }
                else
                {
                    Logger.Warn($"ReapplyIndexProperties(): No player owner for power [{this}]");
                }
            }

            // Refresh passive / toggle powers that rely on changed index properties
            bool isToggledOn = IsToggledOn();

            if (((GetActivationType() == PowerActivationType.Passive || isToggledOn) && HasConditionsWithIndexProperties(indexPropertyFlags)) ||
                (GetPowerCategory() == PowerCategoryType.HiddenPassivePower && (IsToggled() == false || isToggledOn || powerProto.HasPowerEvent(PowerEventType.OnPowerApply))))
            {
                EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.Force);

                if (isToggledOn)
                    RemoveTrackedConditions(false);

                if (isToggledOn || CheckCanTriggerEval())
                {
                    ulong ownerId = Owner.Id;
                    Vector3 ownerPosition = Owner.RegionLocation.Position;

                    PowerApplication powerApplication = new()
                    {
                        UserEntityId = ownerId,
                        UserPosition = ownerPosition,
                        TargetEntityId = ownerId,
                        TargetPosition = ownerPosition,
                        IsFree = true
                    };

                    ApplyInternal(powerApplication);
                }
            }
        }

        #region Keywords

        public bool AddKeyword(PrototypeId keywordProtoRef)
        {
            var powerKeywordProto = GameDatabase.GetPrototype<PowerKeywordPrototype>(keywordProtoRef);
            if (powerKeywordProto == null) return Logger.WarnReturn(false, "AddKeyword(): powerKeywordProto == null");

            powerKeywordProto.GetBitMask(ref _keywordsMask);
            return true;
        }

        public bool RemoveKeyword(PrototypeId keywordProtoRef)
        {
            var powerKeywordProto = GameDatabase.GetPrototype<PowerKeywordPrototype>(keywordProtoRef);
            if (powerKeywordProto == null) return Logger.WarnReturn(false, "RemoveKeyword(): powerKeywordProto == null");

            _keywordsMask.Reset(powerKeywordProto.GetBitIndex());
            return true;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            // We may not have a _keywordsMask here because this can be called from Owner.OnPowerAssigned() before Power.OnAssign().
            // See PowerCollection.FinishAssignPower() for context.
            return _keywordsMask != null && keywordProto != null && KeywordPrototype.TestKeywordBit(_keywordsMask, keywordProto);
        }

        public static void AccumulateKeywordProperties(ref float value, PowerPrototype powerProto, PropertyCollection properties1,
            PropertyCollection properties2, PropertyEnum propertyEnum)
        {
            foreach (var kvp in properties1.IteratePropertyRange(propertyEnum))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                if (keywordProtoRef == PrototypeId.Invalid) continue;

                if (powerProto.HasKeyword(keywordProtoRef.As<KeywordPrototype>()))
                    value += kvp.Value;
            }
        }

        public static void AccumulateKeywordProperties(ref float value, PowerPrototype powerProto, PropertyCollection properties1,
            PropertyCollection properties2, PropertyEnum propertyEnum, int param0)
        {
            foreach (var kvp in properties1.IteratePropertyRange(propertyEnum, param0))
            {
                // Keyword param index is shifted by 1 because we have another param
                Property.FromParam(kvp.Key, 1, out PrototypeId keywordProtoRef);
                if (keywordProtoRef == PrototypeId.Invalid) continue;

                if (powerProto.HasKeyword(keywordProtoRef.As<KeywordPrototype>()))
                    value += kvp.Value;
            }
        }

        public static void AccumulateKeywordProperties(ref long value, PowerPrototype powerProto, PropertyCollection properties1,
            PropertyCollection properties2, PropertyEnum propertyEnum)
        {
            foreach (var kvp in properties1.IteratePropertyRange(propertyEnum))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                if (keywordProtoRef == PrototypeId.Invalid) continue;

                if (powerProto.HasKeyword(keywordProtoRef.As<KeywordPrototype>()))
                    value += kvp.Value;
            }
        }

        #endregion

        public WorldEntity GetUltimateOwner()
        {
            if (Owner == null) return Logger.WarnReturn<WorldEntity>(null, "GetUltimateOwner(): Owner == null");

            if (Owner.HasPowerUserOverride == false)
                return Owner;

            ulong powerUserOverrideId = Owner.Properties[PropertyEnum.PowerUserOverrideID];
            if (powerUserOverrideId == Entity.InvalidId)
                return Owner;

            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(powerUserOverrideId);
            if (ultimateOwner == null || ultimateOwner.IsInWorld == false)
                return null;

            return ultimateOwner;
        }

        public virtual PowerUseResult Activate(ref PowerActivationSettings settings)
        {
            //Logger.Trace($"Activate(): {Prototype}");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerUseResult.GenericError, "Activate(): powerProto == null");

            // Assign a random seed to all powers that are activated on the server (if there isn't one already for whatever reason)
            if (settings.PowerRandomSeed == 0)
                settings.PowerRandomSeed = Game.Random.Next(1, 10000);

            if (IsOnExtraActivation())
                return RunExtraActivation(ref settings);

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);

            // Hold and release (variable activation time) powers
            if (powerProto.ExtraActivation != null)
            {
                if (powerProto.ExtraActivation is SecondaryActivateOnReleasePrototype secondaryActivation)
                {
                    // If this is not a release yet, just prepare for it
                    if (settings.VariableActivationRelease == false)
                    {
                        HandleTriggerPowerEventOnHoldBegin();
                        return StartDelayedActivation(ref settings, secondaryActivation);
                    }

                    if (settings.VariableActivationTime == TimeSpan.Zero)
                        settings.VariableActivationTime = Game.CurrentTime - settings.CreationTime;

                    if (secondaryActivation.MaxReleaseTimeMS > 0)
                    {
                        int variableActivationTimeMS = Math.Min((int)settings.VariableActivationTime.TotalMilliseconds, secondaryActivation.MaxReleaseTimeMS);
                        float variableActivationTimePct = variableActivationTimeMS / (float)Math.Max(secondaryActivation.MinReleaseTimeMS, secondaryActivation.MaxReleaseTimeMS);

                        Properties[PropertyEnum.VariableActivationTimeMS] = TimeSpan.FromMilliseconds(variableActivationTimeMS);
                        Properties[PropertyEnum.VariableActivationTimePct] = variableActivationTimePct;
                    }
                }
            }
            else if (powerProto.PowerCategory == PowerCategoryType.ComboEffect && settings.VariableActivationTime > TimeSpan.Zero
                && settings.TriggeringPowerRef != PrototypeId.Invalid && Owner != null)
            {
                Power triggeringPower = Owner.GetPower(settings.TriggeringPowerRef);
                if (triggeringPower != null)
                {
                    Properties.CopyProperty(triggeringPower.Properties, PropertyEnum.VariableActivationTimeMS);
                    Properties.CopyProperty(triggeringPower.Properties, PropertyEnum.VariableActivationTimePct);
                }
            }

            // Run all defined activation evals
            if (powerProto.EvalOnActivate.HasValue())
            {
                // Initialize context data
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
                evalContext.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, Owner.ConditionCollection);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var2, Owner);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var3, target);

                if (Owner is Agent agent)
                {
                    AIController aiController = agent.AIController;
                    if (aiController != null)
                        evalContext.SetVar_PropertyCollectionPtr(EvalContext.EntityBehaviorBlackboard, aiController.Blackboard.PropertyCollection);
                }

                Eval.InitTeamUpEvalContext(evalContext, Owner);

                bool evalsSucceeded;

                if (target == null)
                {
                    using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, properties);
                    evalsSucceeded = RunActivateEval(evalContext);
                }
                else
                {
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
                    evalsSucceeded = RunActivateEval(evalContext);
                }

                if (evalsSucceeded == false)
                    return Logger.WarnReturn(PowerUseResult.GenericError, $"Activate(): EvalOnActivate failed for Power: {this}.");
            }

            // Run power synergy eval if defined
            if (powerProto.EvalPowerSynergies != null)
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, target?.Properties);
                evalContext.SetReadOnlyVar_ConditionCollectionPtr(EvalContext.Var1, Owner.ConditionCollection);
                evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Var2, Owner);
                Eval.InitTeamUpEvalContext(evalContext, Owner);

                if (Eval.RunBool(powerProto.EvalPowerSynergies, evalContext) == false)
                    return Logger.WarnReturn(PowerUseResult.GenericError, $"Activate(): The EvalPowerSynergies Eval in a power failed:\nPower: [{this}]");
            }

            if (StopsMovementOnActivation())
            {
                if (Owner is Agent agent)
                {
                    Locomotor locomotor = agent.Locomotor;
                    if (locomotor != null && (Owner.IsMovementAuthoritative || locomotor.IsSyncMoving))
                        locomotor.Stop();
                }
            }

            if (OrientsTowardsTargetWhileActive())
            {
                if (Owner is Agent agent)
                    agent.OrientForPower(this, settings.TargetPosition, settings.UserPosition);
            }

            if (GetTargetingShape() == TargetingShapeType.Self)
            {
                settings.TargetEntityId = Owner.Id;
            }
            else if (GetTargetingShape() == TargetingShapeType.TeamUp)
            {
                if (Owner is Avatar avatar)
                {
                    Agent currentTeamUpAgent = avatar.CurrentTeamUpAgent;
                    if (currentTeamUpAgent != null)
                    {
                        settings.TargetEntityId = currentTeamUpAgent.Id;
                        settings.TargetPosition = currentTeamUpAgent.RegionLocation.Position;
                    }
                }
            }

            settings.OriginalTargetPosition = settings.TargetPosition;
            // EntityHelper.CrateOrb(EntityHelper.TestOrb.Red, settings.TargetPosition, Owner.Region);
            GenerateActualTargetPosition(settings.TargetEntityId, settings.OriginalTargetPosition, out settings.TargetPosition, ref settings);
            // EntityHelper.CrateOrb(EntityHelper.TestOrb.BigRed, settings.TargetPosition, Owner.Region);
            MovementPowerPrototype movementPowerProto = FindPowerPrototype<MovementPowerPrototype>(powerProto);
            if (movementPowerProto == null || movementPowerProto.TeleportMethod != TeleportMethodType.Teleport)
                ComputePowerMovementSettings(movementPowerProto, ref settings);

            if (Properties.HasProperty(PropertyEnum.PowerPeriodicActivation))
            {
                int activateAtCount = Properties[PropertyEnum.PowerPeriodicActivation];
                int activationCount = Properties[PropertyEnum.PowerPeriodicActivationCount];

                if (activateAtCount <= 0)
                {
                    return Logger.WarnReturn(PowerUseResult.GenericError,
                        $"Activate(): Tried to activate Periodic Activation Power [{this}] but it has no periodic activation value specified!");
                }

                activationCount++;
                if (activationCount < activateAtCount)
                {
                    Properties[PropertyEnum.PowerPeriodicActivationCount] = activateAtCount;
                    return PowerUseResult.Success;
                }
                else
                {
                    Properties[PropertyEnum.PowerPeriodicActivationCount] = 0;
                }
            }

            PowerUseResult result = ActivateInternal(ref settings);
            if (result != PowerUseResult.Success)
                return result;

            WasLastActivateInterrupted = false;

            if (Owner != null && (Owner.IsDestroyed || Owner.IsInWorld == false))
                return result;

            if (Game == null) return Logger.WarnReturn(PowerUseResult.GenericError, "Activate(): Game == null");
            LastActivateGameTime = Game.CurrentTime;

            // We need to get target here again because activation settings may have changed since the beginning of this method
            target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
            if (target != null && Owner.IsHostileTo(target))
                Owner.Properties[PropertyEnum.LastHostileTargetID] = settings.TargetEntityId;

            _activationPhase = PowerActivationPhase.Active;

            if (GetChargingTime() > TimeSpan.Zero)
                StartCharging();

            if (GetTotalChannelingTime() > TimeSpan.Zero)
                ScheduleChannelStart();

            if (GetActivationType() != PowerActivationType.Passive && powerProto.IsRecurring == false)
                SchedulePowerEnd(ref settings);

            _situationalComponent?.OnPowerActivated(target);

            return result;
        }

        private bool ScheduledActivateCallback(PrototypeId triggeredPowerProtoRef, PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (Game == null) return Logger.WarnReturn(false, "ScheduledActivateCallback(): Game == null");
            if (_scheduledActivateEventList == null) return Logger.WarnReturn(false, "ScheduledActivateCallback(): _scheduledActivateEventList == null");
            if (triggeredPowerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "ScheduledActivateCallback(): triggeredPowerProtoRef == PrototypeId.Invalid");
            if (triggeredPowerEvent == null) return Logger.WarnReturn(false, "ScheduledActivateCallback(): triggeredPowerEvent == null");
            // null check for settings doesn't make sense for us
            if (Owner == null) return Logger.WarnReturn(false, "ScheduledActivateCallback(): Owner == null");

            if (Owner.IsInWorld == false)
                return false;

            if (Owner.IsDead)
                return false;

            PowerCollection powerCollection = Owner.PowerCollection;
            if (powerCollection == null) return Logger.WarnReturn(false, "ScheduledActivateCallback(): powerCollection == null");

            Power triggeredPower = powerCollection.GetPower(triggeredPowerProtoRef);
            if (triggeredPower == null) return Logger.WarnReturn(false,
                $"ScheduledActivateCallback(): Couldn't find the power to activate for a scheduled activation. Owner: {Owner}\nPower ref hash ID: {triggeredPowerProtoRef}");

            return DoActivateComboPower(triggeredPower, triggeredPowerEvent, ref settings);
        }

        public virtual bool ApplyPower(PowerApplication powerApplication)
        {
            if (Owner.IsInWorld == false)
                return Logger.WarnReturn(false, $"ApplyPower(): Power is applying when its owner is not in the world.\n{this}");

            //Logger.Trace($"ApplyPower(): {Prototype}");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "ApplyPower(): powerProto == null");

            if (Game == null) return Logger.WarnReturn(false, "ApplyPower(): Game == null");

            // Toggle
            if (IsToggled())
            {
                if (IsToggledOn())  // Toggle off
                {
                    SetToggleState(false, false);
                    return true;
                }
                else                // Toggle on
                {
                    CancelTogglePowersInSameGroup();
                    SetToggleState(true, false);
                }
            }

            // Check target
            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(powerApplication.TargetEntityId);
            if (NeedsTarget() && (target == null || target.IsInWorld == false))
                return FinishApplyPower(powerApplication, powerProto, false);

            // Update target position if needed
            if (powerProto.ResetTargetPositionAtContactTime && powerProto is not MovementPowerPrototype)
            {
                if (target?.IsInWorld == true)
                    powerApplication.TargetPosition = TargetsAOE() ? target.RegionLocation.ProjectToFloor() : target.RegionLocation.Position;
            }

            // Update user position if needed
            if (powerProto.ResetUserPositionAtContactTime)
                powerApplication.UserPosition = TargetsAOE() ? Owner.RegionLocation.ProjectToFloor() : Owner.RegionLocation.Position;

            // Update recurring application
            if (powerProto.IsRecurring)
                Owner.UpdateRecurringPowerApplication(powerApplication, PrototypeDataRef);

            if (ApplyInternal(powerApplication) == false)
                return FinishApplyPower(powerApplication, powerProto, false);

            if (IsToggled() == false)
                StartCooldown();

            HandleTriggerPowerEventOnContactTime();
            return FinishApplyPower(powerApplication, powerProto, true);
        }

        private bool FinishApplyPower(PowerApplication powerApplication, PowerPrototype powerProto, bool success)
        {
            // Reschedule application or end recurring power
            if (powerProto.IsRecurring)
            {
                EndPowerFlags flags = EndPowerFlags.None;

                if (success && Owner.ShouldContinueRecurringPower(this, ref flags))
                {
                    // Schedule a new application for the next loop
                    PowerApplication newApplication = new(powerApplication);
                    SchedulePowerApplication(newApplication, GetChannelLoopTime());
                }
                else
                {
                    // End power
                    SchedulePowerEnd(TimeSpan.Zero, flags, true);
                }
            }

            if (success)
            {
                if (IsChannelingPower() && NeedsTarget() && IsCancelledOnTargetKilled())
                {
                    if (powerApplication.TargetEntityId == Entity.InvalidId) return Logger.WarnReturn(false, "FinishApplyPower(): powerApplication.TargetEntityId == Entity.InvalidId");

                    EventScheduler scheduler = Game.GameEventScheduler;
                    if (_cancelIfTargetIsKilledEvent.IsValid)
                    {
                        Logger.Warn("FinishApplyPower(): _cancelIfTargetIsKilledEvent.IsValid");
                        scheduler.CancelEvent(_cancelIfTargetIsKilledEvent);
                    }

                    scheduler.ScheduleEvent(_cancelIfTargetIsKilledEvent, CheckIfTargetIsKilledInterval, _pendingEvents);
                    _cancelIfTargetIsKilledEvent.Get().Initialize(this, powerApplication.TargetEntityId);
                }

                if (IsProcEffect() == false && IsComboEffect() == false)
                    Owner.ConditionCollection.RemoveCancelOnPowerUseConditions(powerProto);

                if (IsThrowablePower())
                {
                    ulong throwableEntityId = Owner.Properties[PropertyEnum.ThrowableOriginatorEntity];
                    if (throwableEntityId != 0)
                    {
                        var throwableEntity = Game.EntityManager.GetEntity<WorldEntity>(throwableEntityId);
                        if (throwableEntity != null)
                        {
                            // Trigger EntityDead Event
                            var avatar = Owner?.GetMostResponsiblePowerUser<Avatar>();
                            var player = avatar?.GetOwnerOfType<Player>();
                            Owner.Region.EntityDeadEvent.Invoke(new(throwableEntity, Owner, player));

                            // Destroy throwable
                            throwableEntity.Destroy();
                        }
                    }

                    Owner.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorEntity);
                    Owner.Properties.RemoveProperty(PropertyEnum.ThrowableOriginatorAssetRef);
                }
            }

            return success;
        }

        private static bool DeliverPayload(PowerPayload payload)
        {
            payload.OnDeliverPayload();

            // Find targets for this power application
            List<WorldEntity> targetList = ListPool<WorldEntity>.Instance.Get();
            List<PowerResults> targetResultsList = ListPool<PowerResults>.Instance.Get();

            GetTargets(targetList, payload);
            payload.Properties[PropertyEnum.TargetsHit] = targetList.Count;

            EntityManager entityManager = payload.Game.EntityManager;

            PowerPrototype powerProto = payload.PowerPrototype;
            Game game = payload.Game;
            WorldEntity powerOwner = entityManager.GetEntity<WorldEntity>(payload.PowerOwnerId);
            WorldEntity ultimateOwner = entityManager.GetEntity<WorldEntity>(payload.UltimateOwnerId);
            Avatar avatar = ultimateOwner?.GetMostResponsiblePowerUser<Avatar>();
            Player player = avatar?.GetOwnerOfType<Player>();

            PowerActivationSettings activationSettings = payload.ActivationSettings;

            bool isProc = powerProto.PowerCategory == PowerCategoryType.ProcEffect;

            // Accumulate results for the owner in a separate PowerResults instance
            PowerResults ownerResults = new();
            ulong ownerId = payload.PowerOwnerId;
            ownerResults.Init(ownerId, ownerId, ownerId, payload.PowerOwnerPosition, powerProto, payload.PowerAssetRefOverride, false);
            ownerResults.SetKeywordsMask(payload.KeywordsMask);
            ownerResults.SetFlag(PowerResultFlags.Proc, isProc);
            ownerResults.ActivationSettings = activationSettings;

            // Calculate and apply results for each target
            int payloadCombatLevel = payload.CombatLevel;

            for (int i = 0; i < targetList.Count; i++)
            {
                WorldEntity target = targetList[i];
                int targetCombatLevel = target.GetDynamicCombatLevel(target.CombatLevel);

                // Recalculate initial damage for each enemy -> player result
                if (payloadCombatLevel != targetCombatLevel && payload.IsPlayerPayload == false && target.CanBePlayerOwned())
                {
                    payload.RecalculateInitialDamageForCombatLevel(targetCombatLevel);
                    payloadCombatLevel = targetCombatLevel;
                }

                payload.IncrementHitCount(target.Id);

                PowerResults targetResults = new();
                payload.InitPowerResultsForTarget(targetResults, target);
                targetResults.SetKeywordsMask(payload.KeywordsMask);
                targetResults.SetFlag(PowerResultFlags.Proc, isProc);
                targetResults.ActivationSettings = activationSettings;
                payload.CalculatePowerResults(targetResults, ownerResults, target, true);
                
                if (player != null && powerProto.CanCauseTag)
                {
                    if (avatar.IsInWorld && avatar.IsHostileTo(target))
                        target.SetTaggedBy(player, powerProto);
                }

                if (player != null && powerProto.CanCauseTag)
                {
                    // NOTE: We don't need to null-check the avatar here because we get the player from it
                    if (avatar.IsInWorld && avatar.IsHostileTo(target))
                        target.SetTaggedBy(player, powerProto);
                }

                targetResultsList.Add(targetResults);
            }

            // Calculate owner results
            bool hasMeaningfulOwnerResults = false;

            if (powerOwner != null && powerOwner.IsInWorld && powerOwner.TestStatus(EntityStatus.Destroyed) == false)
            {
                payload.CalculatePowerResults(ownerResults, null, powerOwner, false);
                hasMeaningfulOwnerResults = ownerResults.HasMeaningfulResults();

                Power power = powerOwner.GetPower(powerProto.DataRef);
                if (power != null)
                {
                    power.HandleTriggerPowerEventOnPowerApply(ref activationSettings);

                    foreach (PowerResults results in targetResultsList)
                    {
                        if (results.IsDodged)
                            continue;

                        power.HandleTriggerPowerEventOnPowerHit(results, ref activationSettings);
                    }
                }
            }

            if (hasMeaningfulOwnerResults == false)
                ownerResults.Clear();   // Clear results to prevent them from being sent

            // Apply results - this is delayed to account for proc effects that may kill our targets
            bool isHostile = false;
            foreach (PowerResults results in targetResultsList)
            {
                bool applied = false;

                WorldEntity target = entityManager.GetEntity<WorldEntity>(results.TargetId);
                if (target != null && target.IsInWorld)
                {
                    applied = target.ScheduleApplyPowerResultsEvent(results);
                    isHostile |= results.TestFlag(PowerResultFlags.Hostile);
                }

                if (applied == false)
                    results.Clear();    // leak prevention
            }

            if (powerOwner != null && powerOwner.IsInWorld && powerOwner.TestStatus(EntityStatus.Destroyed) == false)
            {
                bool applied = false;
                powerOwner.ConditionCollection.RemoveCancelOnPowerUsePostConditions(powerProto);

                // Cancelling conditions can potentially kill the target, so check again
                if (powerOwner.IsInWorld && powerOwner.TestStatus(EntityStatus.Destroyed) == false && ownerResults.HasMeaningfulResults())
                {
                    applied = powerOwner.ScheduleApplyPowerResultsEvent(ownerResults);
                }

                if (applied == false)
                    ownerResults.Clear(); // leak prevention
            }

            ListPool<WorldEntity>.Instance.Return(targetList);
            ListPool<PowerResults>.Instance.Return(targetResultsList);

            // Break stealth if needed
            TryBreakStealth(powerOwner, ultimateOwner, powerProto, isHostile, false);

            // Schedule beam sweep reapplication if needed
            CheckBeamSweepTick(payload);

            // Check if this payload needs bouncing and end projectile powers once all bouncing is over
            if (TryBouncePayload(payload) == false)
                TryEndNonMissilePowerActiveUntilProjExpire(payload);

            return true;
        }

        public bool EndPower(EndPowerFlags flags)
        {
            //Logger.Trace($"EndPower(): {Prototype} (flags={flags})");

            // Validate client cancel requests
            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) && flags.HasFlag(EndPowerFlags.ClientRequest)
                && flags.HasFlag(EndPowerFlags.ExitWorld) == false && flags.HasFlag(EndPowerFlags.Unassign) == false
                && flags.HasFlag(EndPowerFlags.Interrupting) == false && flags.HasFlag(EndPowerFlags.Force) == false)
            {
                _lastActivationSettings.Flags |= PowerActivationSettingsFlags.Cancel;
                if (CanBeUserCanceledNow() == false)
                    return false;
            }

            if (CanEndPower(flags) == false)
                return false;

            if (OnEndPowerCheckTooEarly(flags))
            {
                _activationPhase = PowerActivationPhase.MinTimeEnding;
                return false;
            }

            if (OnEndPowerRemoveApplications(flags))
                return false;

            OnEndPowerCancelEvents(flags);
            OnEndPowerCancelConditions();
            OnEndPowerSendCancel(flags);

            if (OnEndPowerCheckLoopEnd(flags))
            {
                HandleTriggerPowerEventOnPowerLoopEnd();
                _activationPhase = PowerActivationPhase.LoopEnding;
                return false;
            }

            EndPowerInternal(flags);

            if (flags.HasFlag(EndPowerFlags.Interrupting))
                WasLastActivateInterrupted = true;

            bool wasActive = _activationPhase != PowerActivationPhase.Inactive;
            _activationPhase = PowerActivationPhase.Inactive;

            if (IsToggledOn() && Owner?.IsInWorld == true)
            {
                bool exitWorld = flags.HasFlag(EndPowerFlags.ExitWorld);
                bool unassign = flags.HasFlag(EndPowerFlags.Unassign);
                bool notEnoughEndurance = flags.HasFlag(EndPowerFlags.NotEnoughEndurance);

                if ((exitWorld == false && unassign) || (exitWorld && HasEnduranceCostRecurring()) || notEnoughEndurance)
                    SetToggleState(false, unassign);
            }

            if (Owner == null) return Logger.WarnReturn(false, "EndPower(): Owner == null");
            Owner.OnPowerEnded(this, flags);

            if (wasActive)
                HandleTriggerPowerEventOnPowerStopped(flags);

            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) == false && flags.HasFlag(EndPowerFlags.ExitWorld) == false
                && flags.HasFlag(EndPowerFlags.Unassign) == false)
            {
                HandleTriggerPowerEventOnPowerEnd();
            }

            _lastActivationSettings = new();

            Owner.ActivatePostPowerAction(this, flags);

            OnEndPowerConditionalRemove(flags);     // Remove one-offs, like throwables

            return true;
        }

        public static bool TryBreakStealth(WorldEntity powerOwner, WorldEntity ultimateOwner, PowerPrototype powerProto, bool isHostile, bool isOverTime)
        {
            // Non-power payloads don't break stealth
            if (powerProto == null)
                return false;

            // Check if this power can potentially break stealth at all
            if (powerProto.BreaksStealth == false || powerProto.Activation == PowerActivationType.Passive)
                return false;

            WorldEntity stealthedEntity = null;

            if (isOverTime == false)
            {
                // For direct damage use the ultimate owner for hotspots / missiles
                if (powerOwner == null || powerOwner is Missile || powerOwner is Hotspot)
                    stealthedEntity = ultimateOwner;
                else
                    stealthedEntity = powerOwner;
            }
            else
            {
                // For DoTs only hotspots attached to their owners break stealth
                if (isHostile && powerOwner != null && powerOwner is Hotspot && ultimateOwner != null &&
                    powerOwner.IsAttachedToEntity && powerOwner.Properties[PropertyEnum.AttachedToEntityId] == ultimateOwner.Id)
                {
                    stealthedEntity = ultimateOwner;
                }
            }

            // Validate our stealthed entity
            if (stealthedEntity == null || stealthedEntity.IsInWorld == false || stealthedEntity.TestStatus(EntityStatus.Destroyed))
                return false;

            // Check if our potentially stealthed entity is actually stealthed
            KeywordPrototype stealthPowerKeyword = GameDatabase.KeywordGlobalsPrototype.StealthPowerKeywordPrototype;
            if (stealthedEntity.HasConditionWithKeyword(stealthPowerKeyword) == false)
                return false;

            // Remove stealth via power results
            PowerResults results = new();
            results.Init(stealthedEntity.Id, stealthedEntity.Id, stealthedEntity.Id, stealthedEntity.RegionLocation.Position, powerProto, AssetId.Invalid, false);

            Power power = stealthedEntity == powerOwner ? stealthedEntity.GetPower(powerProto.DataRef) : null;
            results.SetKeywordsMask(power != null ? power.KeywordsMask : powerProto.KeywordsMask);

            foreach (Condition condition in stealthedEntity.ConditionCollection)
            {
                if (condition.HasKeyword(stealthPowerKeyword) == false)
                    continue;

                results.AddConditionToRemove(condition.Id);
            }

            if (results.HasMeaningfulResults())
                stealthedEntity.ScheduleApplyPowerResultsEvent(results);

            return true;
        }

        protected virtual PowerUseResult RunExtraActivation(ref PowerActivationSettings settings)
        {
            if (Prototype?.ExtraActivation == null) return Logger.WarnReturn(PowerUseResult.ExtraActivationFailed, "RunExtraActivation(): Prototype?.ExtraActivation == null");

            CancelExtraActivationTimeout();

            Owner.Properties.AdjustProperty(1, new(PropertyEnum.PowerActivationCount, PrototypeDataRef));

            StartCooldown();
            EndPower(EndPowerFlags.None);

            return PowerUseResult.Success;
        }

        private void CancelIfTargetIsKilled(ulong targetId)
        {
            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(targetId);

            if (target == null || target.IsDead)
            {
                // End the power if the target is lost
                EndPower(EndPowerFlags.ExplicitCancel);
            }
            else
            {
                // Schedule another check
                Game.GameEventScheduler.ScheduleEvent(_cancelIfTargetIsKilledEvent, CheckIfTargetIsKilledInterval, _pendingEvents);
                _cancelIfTargetIsKilledEvent.Get().Initialize(this, targetId);
            }
        }

        #region Delayed / Variable Time Activation

        public void ReleaseVariableActivation(ref PowerActivationSettings settings)
        {
            if (Prototype.ExtraActivation is not SecondaryActivateOnReleasePrototype secondaryActivationProto)
                return;

            // If we are receiving a release message and there are not delayed activation settings server-side, it means we have a desync that needs to be handled
            if (_hasDelayedActivationSettings == false)
            {
                if (Owner is Avatar avatar)
                {
                    // If the server doesn't have delayed activation settings because of a desync, init new settings
                    if (avatar.CanActivatePower(this, settings.TargetEntityId, settings.TargetPosition) == PowerUseResult.Success)
                    {
                        avatar.CancelPendingAction();
                        settings.VariableActivationRelease = true;
                        settings.VariableActivationTime = TimeSpan.FromMilliseconds(secondaryActivationProto.MinReleaseTimeMS);
                        Activate(ref settings);
                    }
                    else if (avatar.PendingPowerDataRef == PrototypeDataRef)
                    {
                        avatar.CancelPendingAction();
                    }
                }

                return;
            }

            _delayedActivationSettings.TargetEntityId = settings.TargetEntityId;
            _delayedActivationSettings.TargetPosition = settings.TargetPosition;
            _delayedActivationSettings.VariableActivationTime = Game.CurrentTime - _delayedActivationSettings.CreationTime;

            TimeSpan delay = TimeSpan.FromMilliseconds(secondaryActivationProto.MinReleaseTimeMS) - _delayedActivationSettings.VariableActivationTime;

            if (delay > TimeSpan.Zero)
                ScheduleDelayedActivation(delay);
            else
                DelayedActivateCallback();
        }

        private PowerUseResult StartDelayedActivation(ref PowerActivationSettings settings, SecondaryActivateOnReleasePrototype secondaryActivationProto)
        {
            // Send pre-activation message to nearby clients
            var preActivatePower = NetMessagePreActivatePower.CreateBuilder()
                .SetIdUserEntity(Owner.Id)
                .SetPowerPrototypeId((ulong)PrototypeDataRef)
                .SetIdTargetEntity(settings.TargetEntityId)
                .SetTargetPosition(settings.TargetPosition.ToNetStructPoint3())
                .Build();

            Game.NetworkManager.SendMessageToInterested(preActivatePower, Owner, AOINetworkPolicyValues.AOIChannelProximity, true);

            // Initialize settings
            if (_hasDelayedActivationSettings)
                Logger.Warn($"StartDelayedActivation(): Overwriting existing delayed activation settings for {this}");

            if (_delayedActivationEvent.IsValid)
            {
                Logger.Warn($"StartDelayedActivation(): Cancelling previously scheduled delayed activation for {this}");
                Game.GameEventScheduler?.CancelEvent(_delayedActivationEvent);
            }

            _hasDelayedActivationSettings = true;
            _delayedActivationSettings = settings;
            _delayedActivationSettings.VariableActivationRelease = true;

            // TODO: Implement EnduranceCostIncreasePerSecond if needed for older versions of the game
            // (1.52 uses EnduranceCostRank0NoCost for all hold and release powers).

            return PowerUseResult.Success;
        }

        private bool DelayedActivateCallback()
        {
            if (_hasDelayedActivationSettings == false) return Logger.WarnReturn(false, "DelayedActivateCallback(): _hasDelayedActivationSettings == false");

            if (Owner.IsInWorld && Owner.IsDead == false)
            {
                _delayedActivationSettings.UserPosition = Owner.RegionLocation.Position;
                Activate(ref _delayedActivationSettings);
            }

            CancelDelayedActivation();
            return true;
        }

        #endregion

        private bool StartCharging()
        {
            if (Owner == null) return Logger.WarnReturn(false, "StartCharging(): Owner == null");
            if (Game == null) return Logger.WarnReturn(false, "StartCharging(): Game == null");

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "StartCharging(): scheduler == null");

            if (_stopChargingEvent.IsValid)
                scheduler.CancelEvent(_stopChargingEvent);

            TimeSpan chargeTime = GetChargingTime();
            if (chargeTime <= TimeSpan.Zero) return Logger.WarnReturn(false, "StartCharging(): chargeTime <= TimeSpan.Zero");

            _activationPhase = PowerActivationPhase.Charging;

            scheduler.ScheduleEvent(_stopChargingEvent, chargeTime, _pendingActivationPhaseEvents);
            _stopChargingEvent.Get().Initialize(this);

            return true;
        }

        private bool StopCharging()
        {
            if (Owner == null) return Logger.WarnReturn(false, "StopCharging(): Owner == null");

            _activationPhase = PowerActivationPhase.Active;

            // Cancel scheduled charge event if stopping early
            if (_stopChargingEvent.IsValid)
            {
                if (Game == null) return Logger.WarnReturn(false, "StopCharging(): Game == null");

                EventScheduler scheduler = Game.GameEventScheduler;
                if (scheduler == null) return Logger.WarnReturn(false, "StopCharging(): scheduler == null");

                scheduler.CancelEvent(_stopChargingEvent);
            }

            return true;
        }

        private bool StartChanneling()
        {
            if (Owner == null) return Logger.WarnReturn(false, "StartChanneling(): Owner == null");
            if (Game == null) return Logger.WarnReturn(false, "StartChanneling(): Game == null");

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "StartChanneling(): scheduler == null");

            TimeSpan channelTime = GetChannelLoopTime();
            if (channelTime <= TimeSpan.Zero) return Logger.WarnReturn(false, "StartChanneling(): channelTime <= TimeSpan.Zero");

            if (IsEnding)
                return true;

            // Start channeling
            _activationPhase = PowerActivationPhase.Channeling;

            if (Prototype?.MovementPreventChannelLoop == true)
                Owner.Locomotor?.Stop();

            // Update cancellation event
            if (_stopChannelingEvent.IsValid)
            {
                scheduler.RescheduleEvent(_stopChannelingEvent, channelTime);
                _stopChannelingEvent.Get().Initialize(this);
            }
            else
            {
                scheduler.ScheduleEvent(_stopChannelingEvent, channelTime, _pendingActivationPhaseEvents);
                _stopChannelingEvent.Get().Initialize(this);
            }

            return true;
        }

        private bool StopChanneling()
        {
            if (Owner == null) return Logger.WarnReturn(false, "StopChanneling(): Owner == null");

            if (_activationPhase != PowerActivationPhase.ChannelStarting && _activationPhase != PowerActivationPhase.Channeling
                && _activationPhase != PowerActivationPhase.MinTimeEnding && _activationPhase != PowerActivationPhase.LoopEnding)
            {
                return Logger.WarnReturn(false,
                    $"StopChanneling(): Tried to stop channeling power {this} that isn't channeling. Activation phase: {_activationPhase}. Power owner: [{Owner}] IsDead: {Owner.IsDead}");
            }

            if (Game == null) return Logger.WarnReturn(false, "StopChanneling(): Game == null");
            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "StopChanneling(): scheduler == null");

            if (_startChannelingEvent.IsValid)
                scheduler.CancelEvent(_startChannelingEvent);

            _activationPhase = PowerActivationPhase.Active;

            OnEndChannelingPhase();
            return true;
        }

        #region Cooldowns

        public bool StartCooldown(TimeSpan cooldownDuration = default)
        {
            if (Owner == null) return Logger.WarnReturn(false, "StartCooldown(): Owner == null");

            if (CanStartCooldowns() == false)
                return false;

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "StartCooldown(): powerProto == null");

            if (GetPowerCategory() == PowerCategoryType.NormalPower)
            {
                // AI cooldowns for normal powers have their own thing going on
                if (Owner is Agent agentOwner && agentOwner.AIController != null)
                {
                    TimeSpan cooldownTime = agentOwner.Game.CurrentTime + cooldownDuration;
                    PropertyCollection blackboardProperties = agentOwner.AIController.Blackboard.PropertyCollection;
                    blackboardProperties[PropertyEnum.AIProceduralPowerSpecificCDTime, PrototypeDataRef] = (long)cooldownTime.TotalMilliseconds;
                    return true;
                }
            }

            if (cooldownDuration == TimeSpan.Zero)
            {
                if (powerProto.ExtraActivation is ExtraActivateOnSubsequentPrototype extraActivateOnSubsequent)
                {
                    Owner.Properties.AdjustProperty(1, new(PropertyEnum.PowerActivationCount, PrototypeDataRef));

                    ScheduleExtraActivationTimeout(extraActivateOnSubsequent);

                    int numActivatesBeforeCooldown = extraActivateOnSubsequent.GetNumActivatesBeforeCooldown(Properties[PropertyEnum.PowerRank]);
                    if (numActivatesBeforeCooldown > 1)
                    {
                        int powerActivationCount = Owner.Properties[PropertyEnum.PowerActivationCount, PrototypeDataRef];
                        if (powerActivationCount < numActivatesBeforeCooldown)
                            return true;

                        // Cancel timeout and start cooldown after reaching the number of activations before cooldown
                        Owner.Properties[PropertyEnum.PowerActivationCount, PrototypeDataRef] = 0;
                        CancelExtraActivationTimeout();
                        HandleTriggerPowerEventOnExtraActivationCooldown();
                    }
                }

                if (Owner.GetPowerChargesMax(PrototypeDataRef) > 0)
                {
                    // Fix for BUE 2
                    if (IsOnCooldown() || (powerProto is MovementPowerPrototype && Game.CustomGameOptions.DisableMovementPowerChargeCost))
                        return true;
                }
            }

            PropertyCollection properties = Owner.Properties;
            if (powerProto.CooldownOnPlayer)
            {
                Player player = Owner.GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, "StartCooldown(): player == null");
                properties = player.Properties;
            }

            cooldownDuration = CalcCooldownDuration(powerProto, Owner, Properties, cooldownDuration);

            if (cooldownDuration > TimeSpan.Zero)
            {
                properties[PropertyEnum.PowerCooldownStartTime, powerProto.DataRef] = Game.Current.CurrentTime;
                properties[PropertyEnum.PowerCooldownDuration, powerProto.DataRef] = cooldownDuration;

                // Schedule cooldown end event that's going to replenish charges
                EventScheduler scheduler = Game?.GameEventScheduler;
                if (scheduler == null) return Logger.WarnReturn(false, "StartCooldown(): scheduler == null");

                if (_endCooldownEvent.IsValid)
                {
                    scheduler.RescheduleEvent(_endCooldownEvent, cooldownDuration);
                }
                else
                {
                    scheduler.ScheduleEvent(_endCooldownEvent, cooldownDuration, _pendingEvents);
                    _endCooldownEvent.Get().Initialize(this);
                }

                //Logger.Debug($"StartCooldown(): {Prototype} - {cooldownDuration.TotalMilliseconds} ms");
            }

            return true;
        }

        private bool EndCooldown()
        {
            if (Owner == null) return Logger.WarnReturn(false, $"EndCooldown(): Owner == null");

            if (CanEndCooldowns() == false)
                return false;

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, $"EndCooldown(): powerProto == null");

            PropertyCollection properties = Owner.Properties;
            if (powerProto.CooldownOnPlayer)
            {
                Player player = Owner.GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, $"EndCooldown(): player == null");
                properties = player.Properties;
            }

            properties.RemoveProperty(new(PropertyEnum.PowerCooldownStartTime, PrototypeDataRef));
            properties.RemoveProperty(new(PropertyEnum.PowerCooldownDuration, PrototypeDataRef));

            if (_endCooldownEvent.IsValid)
            {
                EventScheduler scheduler = Game.GameEventScheduler;
                if (scheduler == null) return Logger.WarnReturn(false, "EndCooldown(): scheduler == null");
                scheduler.CancelEvent(_endCooldownEvent);
            }

            OnCooldownEndCallback();
            return true;
        }

        private bool ModifyCooldown(TimeSpan offset)
        {
            if (Owner == null) return Logger.WarnReturn(false, "ModifyCooldown(): Owner == null");

            if (CanModifyCooldowns() == false)
                return false;

            if (IsOnCooldown() == false)
                return false;

            if (Owner is Agent agent && agent.AIController != null)
            {
                PropertyCollection blackboardProperties = agent.AIController.Blackboard.PropertyCollection;
                blackboardProperties.AdjustProperty((int)offset.TotalMilliseconds, new(PropertyEnum.AIProceduralPowerSpecificCDTime, PrototypeDataRef));
                return true;
            }

            PropertyCollection properties = Owner.Properties;

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "ModifyCooldown(): powerProto == null");

            if (powerProto.CooldownOnPlayer)
            {
                Player player = Owner.GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(false, $"ModifyCooldown(): player == null");

                properties = player.Properties;
            }

            properties.AdjustProperty((int)offset.TotalMilliseconds, new(PropertyEnum.PowerCooldownDuration, PrototypeDataRef));

            // Reschedule cooldown end event (since we are modifying an existing cooldown, there should be one)
            if (_endCooldownEvent.IsValid == false) return Logger.WarnReturn(false, "ModifyCooldown(): _endCooldownEvent.IsValid == false");

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, $"ModifyCooldown(): scheduler == null");

            TimeSpan delay = _endCooldownEvent.Get().FireTime - Game.CurrentTime + offset;
            delay = Clock.Max(delay, TimeSpan.Zero);
            scheduler.RescheduleEvent(_endCooldownEvent, delay);

            return true;
        }

        private bool ModifyCooldownByPercentage(float value)
        {
            if (Owner == null) return Logger.WarnReturn(false, "ModifyCooldownByPercentage(): Owner == null");

            if (CanModifyCooldowns() == false)
                return false;

            if (IsOnCooldown() == false)
                return false;

            value = MathF.Max(value, -1f);

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, $"ModifyCooldownBYPercentage(): powerProto == null");

            TimeSpan cooldownTimeRemaining = Owner.GetAbilityCooldownTimeRemaining(powerProto);
            return ModifyCooldown(cooldownTimeRemaining * value);
        }

        private void OnCooldownEndCallback()
        {
            // This callback is only for replenishing power charges
            if (ShouldReplenishCharges() == false)
                return;

            // Replenish a charge
            PrototypeId powerProtoRef = PrototypeDataRef;
            Owner.Properties.AdjustProperty(1, new(PropertyEnum.PowerChargesAvailable, powerProtoRef));

            if (Owner.GetPowerChargesAvailable(powerProtoRef) < Owner.GetPowerChargesMax(powerProtoRef))
            {
                // Restart the cooldown to continue replenishing charges if we are still below cap
                StartCooldown();
            }
            else
            {
                // Remove the cooldown if we are done replenishing charges
                Owner.Properties.RemoveProperty(new(PropertyEnum.PowerCooldownStartTime, powerProtoRef));
                Owner.Properties.RemoveProperty(new(PropertyEnum.PowerCooldownDuration, powerProtoRef));
            }
        }

        private bool ExtraActivateTimeoutCallback()
        {
            if (Owner == null) return Logger.WarnReturn(false, "ExtraActivateTimeoutCallback(): Owner == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "ExtraActivateTimeoutCallback(): powerProto == null");

            if (powerProto.ExtraActivation != null &&
                powerProto.ExtraActivation is ExtraActivateOnSubsequentPrototype extraActivateOnSubsequent)
            {
                // Fast forward activation count to the end of the counter
                int numActivatesBeforeCooldown = extraActivateOnSubsequent.GetNumActivatesBeforeCooldown(Properties[PropertyEnum.PowerRank]);
                if (numActivatesBeforeCooldown > 1)
                    Owner.Properties[PropertyEnum.PowerActivationCount, PrototypeDataRef] = numActivatesBeforeCooldown;
            }

            StartCooldown();
            return true;
        }

        private bool ShouldReplenishCharges()
        {
            PrototypeId powerProtoRef = PrototypeDataRef;
            int maxCharges = Owner.GetPowerChargesMax(powerProtoRef);

            if (maxCharges <= 0)
                return false;

            int chargeCount = Owner.GetPowerChargesAvailable(powerProtoRef);

            if (Owner.TestStatus(EntityStatus.EnteringWorld) == false &&
                Owner.TestStatus(EntityStatus.ExitingWorld) == false &&
                (chargeCount < 0 || chargeCount > maxCharges))
            {
                return Logger.WarnReturn(false, "ShouldReplenishCharges(): chargeCount < 0 || chargetCount > maxCharges");
            }

            return chargeCount < maxCharges;
        }

        #endregion

        #region Costs

        private void PayCost(PowerApplication powerApplication)
        {
            // it's free real estate
            if (powerApplication.IsFree)
                return;

            // Item cost
            if (powerApplication.ItemSourceId != Entity.InvalidId && IsItemPower())
            {
                Item item = Game.EntityManager.GetEntity<Item>(powerApplication.ItemSourceId);
                if (item != null)
                    item.OnUsePowerActivated();
                else
                    Logger.Warn("PayCost(): item == null");
            }

            // Charges
            if (Owner.GetPowerChargesMax(PrototypeDataRef) > 0)
            {
                // Doctors hate him! BUE fixed with one simple trick
                if (Prototype is not MovementPowerPrototype || Game.CustomGameOptions.DisableMovementPowerChargeCost == false)
                    Owner.Properties.AdjustProperty(-1, new(PropertyEnum.PowerChargesAvailable, PrototypeDataRef));
            }

            // Endurance (spirit / other primary resources)
            if (Owner is Avatar avatar)
            {
                bool hasRecurringCost = false;

                foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in avatar.GetPrimaryResourceManaBehaviors())
                {
                    float endurance = Owner.Properties[PropertyEnum.Endurance, primaryManaBehaviorProto.ManaType];
                    float cost = GetEnduranceCost(primaryManaBehaviorProto.ManaType, true);

                    endurance = MathF.Max(endurance - cost, 0f);
                    Owner.Properties[PropertyEnum.Endurance, primaryManaBehaviorProto.ManaType] = endurance;

                    hasRecurringCost |= GetEnduranceCostRecurring(primaryManaBehaviorProto.ManaType, false, false) > 0f;
                }

                if (hasRecurringCost)
                    ScheduleRecurringCostEvent();
            }

            // Check costs for the ultimate owner
            WorldEntity ultimateOwner = GetUltimateOwner();
            if (ultimateOwner != null)
            {
                // Secondary resource cost
                float secondaryResource = ultimateOwner.Properties[PropertyEnum.SecondaryResource];
                float cost = GetSecondaryResourceCost();

                // Secondary resource costs are paid only if we have enough of them
                if (cost <= secondaryResource)
                {
                    secondaryResource = MathF.Max(secondaryResource - cost, 0f);
                    ultimateOwner.Properties[PropertyEnum.SecondaryResource] = secondaryResource;
                }

                // Health cost
                float powerHealthCost = Properties[PropertyEnum.PowerHealthCost];
                if (powerHealthCost > 0f)
                {
                    long health = ultimateOwner.Properties[PropertyEnum.Health];
                    health -= MathHelper.RoundToInt64(powerHealthCost);
                    health = Math.Max(health, 1);   // Should not be able to kill self with a health cost
                    ultimateOwner.Properties[PropertyEnum.Health] = health;
                }

                // Weapon cost for bounce powers
                // NOTE: Weapon costs for non-bounce powers are handled by missiles themselves.
                if (Properties[PropertyEnum.PowerUsesReturningWeapon] && Properties[PropertyEnum.BounceCount] > 0)
                    ultimateOwner.Properties[PropertyEnum.WeaponMissing] = true;
            }
        }

        private bool PayRecurringCost()
        {
            if (Owner is not Avatar avatar) return Logger.WarnReturn(false, "PayRecurringCost(): Owner is not Avatar");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "PayRecurringCost(): powerProto == null");

            // NOTE: There shouldn't be more than one recurring endurance cost for a single power.
            foreach (PrimaryResourceManaBehaviorPrototype primaryManaBehaviorProto in avatar.GetPrimaryResourceManaBehaviors())
            {
                float enduranceCostRecurring = GetEnduranceCostRecurring(primaryManaBehaviorProto.ManaType, true, true);

                if (enduranceCostRecurring <= 0f)
                {
                    // If this recurring cost payment was reduced to zero by modifiers, don't pay and schedule the next tick straight away.
                    if (GetEnduranceCostRecurring(primaryManaBehaviorProto.ManaType, false, false) > 0f)
                    {
                        ScheduleRecurringCostEvent();
                        return true;
                    }

                    // Continue to the next mana type (if any)
                    continue;
                }

                // EnduranceCostRecurring is cost per second, so we need to multiply it by the number of seconds in our interval
                enduranceCostRecurring *= (float)powerProto.GetRecurringCostInterval().TotalSeconds;

                // Recurring endurance cost may be optional for this power (by default it is required)
                bool enduranceCostRequired = Properties[PropertyEnum.EnduranceCostRequired];

                // Check if we have endurance to continue
                float endurance = avatar.Properties[PropertyEnum.Endurance, primaryManaBehaviorProto.ManaType];
                if (endurance <= 0f || (enduranceCostRequired && enduranceCostRecurring > endurance))
                {
                    EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.NotEnoughEndurance);
                    return true;
                }

                // Schedule the next tick and pay the cost.
                // Schedule before paying because changing the endurance property may end the power.
                ScheduleRecurringCostEvent();
                avatar.Properties.AdjustProperty(-enduranceCostRecurring, new(PropertyEnum.Endurance, primaryManaBehaviorProto.ManaType));
                return true;
            }

            return true;
        }

        #endregion

        #region Condition Management

        public void TrackCondition(ulong entityId, Condition condition)
        {
            // NOTE: We include power index property flags in tracked conditions because conditions applied by this power
            // may need to be refreshed when one of the index properties of this power changes (e.g. owner levels up).
            TrackedCondition trackedCondition = new(entityId, condition.Id, condition.PowerIndexPropertyFlags);
            _trackedConditionList.Add(trackedCondition);
            _trackedConditionIndexPropertyFlags |= trackedCondition.PowerIndexPropertyFlags;
        }

        public void UntrackCondition(ulong entityId, Condition condition)
        {
            TrackedCondition trackedCondition = new(entityId, condition.Id, condition.PowerIndexPropertyFlags);

            if (_trackedConditionList.Remove(trackedCondition))
                RefreshConditionIndexProperties();
        }

        public bool IsTrackingCondition(ulong entityId, Condition condition)
        {
            TrackedCondition trackedCondition = new(entityId, condition.Id, condition.PowerIndexPropertyFlags);
            return _trackedConditionList.Contains(trackedCondition);
        }

        public void RemoveOrUnpauseTrackedConditionsForTarget(ulong targetId)
        {
            // Check if there is anything to remove before doing anything else
            if (_trackedConditionList.Count == 0)
                return;

            // The entity the tracked condition was applied to may no longer exist
            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(targetId);
            ConditionCollection conditionCollection = target?.ConditionCollection;
            if (conditionCollection == null)
                return;

            List<TrackedCondition> unpausedConditionList = ListPool<TrackedCondition>.Instance.Get();

            for (int i = 0; i < _trackedConditionList.Count; i++)
            {
                TrackedCondition trackedCondition = _trackedConditionList[i];
                if (trackedCondition.EntityId != targetId)
                    continue;

                _trackedConditionList.RemoveAt(i);
                if (conditionCollection.RemoveOrUnpauseCondition(trackedCondition.ConditionId) == false)
                    unpausedConditionList.Add(trackedCondition);

                // RemoveOrUnpauseCondition can potentially change _trackedConditionList, so we need to restart iteration
                i = -1;
            }

            // Readd conditions that were unpaused
            foreach (TrackedCondition unpausedCondition in unpausedConditionList)
                _trackedConditionList.Add(unpausedCondition);

            ListPool<TrackedCondition>.Instance.Return(unpausedConditionList);
        }

        private void RemoveTrackedConditions(bool allowUnpause)
        {
            List<TrackedCondition> unpausedConditionList = ListPool<TrackedCondition>.Instance.Get();

            EntityManager entityManager = Game.EntityManager;

            while (_trackedConditionList.Count > 0)
            {
                int index = _trackedConditionList.Count - 1;
                TrackedCondition trackedCondition = _trackedConditionList[index];
                _trackedConditionList.RemoveAt(index);

                // The entity the tracked condition was applied to may no longer exist
                WorldEntity entity = entityManager.GetEntity<WorldEntity>(trackedCondition.EntityId);
                ConditionCollection conditionCollection = entity?.ConditionCollection;
                if (conditionCollection == null)
                    continue;

                if (allowUnpause)
                {
                    if (conditionCollection.RemoveOrUnpauseCondition(trackedCondition.ConditionId) == false)
                        unpausedConditionList.Add(trackedCondition);
                }
                else
                {
                    conditionCollection.RemoveCondition(trackedCondition.ConditionId);
                }
            }

            if (allowUnpause)
            {
                // Readd conditions that were unpaused
                foreach (TrackedCondition unpausedCondition in unpausedConditionList)
                    _trackedConditionList.Add(unpausedCondition);
            }

            RefreshConditionIndexProperties();
            ListPool<TrackedCondition>.Instance.Return(unpausedConditionList);
        }

        private void RefreshConditionIndexProperties()
        {
            _trackedConditionIndexPropertyFlags = PowerIndexPropertyFlags.None;

            foreach (TrackedCondition trackedCondition in _trackedConditionList)
                _trackedConditionIndexPropertyFlags |= trackedCondition.PowerIndexPropertyFlags;
        }

        private bool HasConditionsWithIndexProperties(PowerIndexPropertyFlags indexPropertyFlags)
        {
            return (_trackedConditionIndexPropertyFlags & indexPropertyFlags) != PowerIndexPropertyFlags.None;
        }

        #endregion

        public bool GetTargets(List<WorldEntity> targetList, WorldEntity target, in Vector3 targetPosition, int randomSeed = 0, int beamSweepSlice = -1)
        {
            if (Owner == null) return Logger.WarnReturn(false, "GetTargets(): Owner == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "GetTargets(): powerProto == null");

            if (Owner.IsInWorld == false)
                Logger.WarnReturn(false, $"GetTargets(): Entity {Owner} getting targets for power {this} is not in the world.");

            return GetTargets(targetList, Game, powerProto, Owner.Properties, target, targetPosition, Owner.RegionLocation.Position,
                GetApplicationRange(), Owner.Region.Id, Owner.Id, Owner.Id, Owner.Alliance, beamSweepSlice, GetFullExecutionTime(), randomSeed);
        }

        public static bool GetTargets(List<WorldEntity> targetList, PowerPayload payload)
        {
            WorldEntity primaryTarget = payload.Game.EntityManager.GetEntity<WorldEntity>(payload.TargetId);

            return GetTargets(targetList, payload.Game, payload.PowerPrototype, payload.Properties, primaryTarget, payload.TargetPosition, payload.PowerOwnerPosition,
                payload.Range, payload.RegionId, payload.PowerOwnerId, payload.UltimateOwnerId, payload.OwnerAlliance, payload.AOESweepTick,
                payload.ExecutionTime, payload.PowerRandomSeed);
        }

        public static bool GetTargets(List<WorldEntity> targetList, Game game, PowerPrototype powerProto, PropertyCollection properties,
            WorldEntity target, in Vector3 targetPosition, in Vector3 userPosition, float range, ulong regionId, ulong ownerId,
            ulong ultimateOwnerId, AlliancePrototype userAllianceProto, int beamSweepSlice, TimeSpan executionTime, int randomSeed)
        {
            // Some more validation
            if (game == null) return Logger.WarnReturn(false, "GetTargets(): game == null");

            WorldEntity owner = game.EntityManager.GetEntity<WorldEntity>(ownerId);

            TargetingStylePrototype targetingStyle = powerProto.GetTargetingStyle();
            if (targetingStyle == null) return Logger.WarnReturn(false, "GetTargets(): targetingStyle == null");

            TargetingReachPrototype targetingReach = powerProto.GetTargetingReach();
            if (targetingReach == null) return Logger.WarnReturn(false, "GetTargets(): targetingReach == null");
            
            // Add targets based on targeting style / reach
            if (targetingStyle.TargetingShape == TargetingShapeType.Self)
            {
                if (owner != null)
                    targetList.Add(owner);
            }
            else if (targetingStyle.TargetingShape == TargetingShapeType.SingleTargetOwner)
            {
                WorldEntity ultimateOwner = ultimateOwnerId != ownerId ? game.EntityManager.GetEntity<WorldEntity>(ultimateOwnerId) : owner;
                if (ultimateOwner != null)
                {
                    WorldEntity mostResponsiblePowerUser = ultimateOwner.GetMostResponsiblePowerUser<Agent>();
                    if (mostResponsiblePowerUser != null)
                        targetList.Add(mostResponsiblePowerUser);
                }
            }
            else if (targetingStyle.TargetingShape == TargetingShapeType.TeamUp)
            {
                if (owner is Avatar avatar)
                {
                    Agent teamUpAgent = avatar.CurrentTeamUpAgent;
                    if (teamUpAgent != null)
                        targetList.Add(teamUpAgent);
                }
            }
            else if (targetingReach.TargetsEntitiesInInventory != InventoryConvenienceLabel.None)
            {
                WorldEntity ultimateOwner = ultimateOwnerId != ownerId ? game.EntityManager.GetEntity<WorldEntity>(ultimateOwnerId) : owner;
                if (ultimateOwner != null)
                {
                    WorldEntity mostResponsiblePowerUser = ultimateOwner.GetMostResponsiblePowerUser<Agent>();
                    if (mostResponsiblePowerUser != null)
                        GetTargetsFromInventory(targetList, game, mostResponsiblePowerUser, target, powerProto, userAllianceProto, targetingReach.TargetsEntitiesInInventory);
                }
            }
            else if (targetingStyle.TargetingShape == TargetingShapeType.SingleTarget ||
                targetingStyle.TargetingShape == TargetingShapeType.SingleTargetRandom)
            {
                if (target != null && IsValidTarget(powerProto, owner, userAllianceProto, target)
                    && (properties[PropertyEnum.PayloadSkipRangeCheck] || IsInApplicationRange(target, userPosition, ownerId, range, powerProto)))
                {
                    targetList.Add(target);
                }
                else if (targetingStyle.NeedsTarget == false && targetingReach.Melee)
                {
                    GetValidMeleeTarget(targetList, powerProto, userAllianceProto, owner, targetPosition);
                }
            }
            else if (TargetsAOE(powerProto))
            {
                GetAOETargets(targetList, game, powerProto, range, properties, target, owner, in targetPosition,
                    in userPosition, regionId, ownerId, userAllianceProto, beamSweepSlice, executionTime, randomSeed);
            }

            return true;
        }

        public static bool IsTargetInAOE(WorldEntity target, WorldEntity owner, Vector3 ownerPosition, Vector3 targetPosition, float radius,
            int beamSlice, TimeSpan totalSweepTime, PowerPrototype powerProto, PropertyCollection properties)
        {
            var styleProto = powerProto.GetTargetingStyle();
            if (styleProto == null) return Logger.WarnReturn(false, $"IsTargetInAOE(): Unable to get the prototype for power. Prototype:{powerProto} ");
            Vector3 position = targetPosition;
            if (styleProto.AOESelfCentered && styleProto.RandomPositionRadius == 0)
                position = ownerPosition + styleProto.GetOwnerOrientedPositionOffset(owner);

            return styleProto.TargetingShape switch
            {
                TargetingShapeType.ArcArea      => IsTargetInArc(target, owner, radius, position, targetPosition, powerProto, styleProto, properties),
                TargetingShapeType.BeamSweep    => IsTargetInBeamSlice(target, owner, radius, position, targetPosition, beamSlice, totalSweepTime, powerProto, styleProto),
                TargetingShapeType.CapsuleArea  => IsTargetInCapsule(target, owner, position, targetPosition, powerProto, styleProto, properties),
                TargetingShapeType.CircleArea   => IsTargetInCircle(target, radius, position),
                TargetingShapeType.RingArea     => IsTargetInRing(target, radius, position, powerProto, properties),
                TargetingShapeType.WedgeArea    => IsTargetInWedge(target, owner, radius, position, targetPosition, powerProto, styleProto),
                _ => Logger.WarnReturn(false, $"IsTargetInAOE(): Targeting shape ({styleProto.TargetingShape}) for this power hasn't been implemented! Prototype: {powerProto}"),
            };
        }

        public static int ComputeNearbyPlayers(Region region, Vector3 position, int numPlayersMin = 0, bool combatActiveOnly = false, HashSet<ulong> nearbyPlayerIds = null)
        {
            return ComputeNearbyPlayersInternal(region, position, numPlayersMin, combatActiveOnly, nearbyPlayerIds, null);
        }

        public static int ComputeNearbyPlayers(Region region, Vector3 position, int numPlayersMin, bool combatActiveOnly, List<Player> nearbyPlayers)
        {
            return ComputeNearbyPlayersInternal(region, position, numPlayersMin, combatActiveOnly, null, nearbyPlayers);
        }

        private static int ComputeNearbyPlayersInternal(Region region, Vector3 position, int numPlayersMin, bool combatActiveOnly, HashSet<ulong> nearbyPlayerIds, List<Player> nearbyPlayers)
        {
            if (region == null) return Logger.WarnReturn(numPlayersMin, "ComputeNearbyPlayersInternal(): region == null");

            DifficultyPrototype difficultyProto = region.TuningTable?.Prototype;
            if (difficultyProto == null) return Logger.WarnReturn(numPlayersMin, "ComputeNearbyPlayersInternal(): difficultyProto == null");

            // "Nearby" depends on the region: in private regions like terminals this covers the entire region (100000),
            // while in public regions like Midtown Patrol it's about the size of two screens (1200).
            float playerNearbyRange = difficultyProto.PlayerNearbyRange;
            if (playerNearbyRange <= 0f) return Logger.WarnReturn(numPlayersMin, "ComputeNearbyPlayersInternal(): playerNearbyRange <= 0f");

            int numPlayers = 0;

            Sphere sphere = new(position, playerNearbyRange);
            foreach (Avatar avatar in region.IterateAvatarsInVolume(sphere))
            {
                // Skip AFK avatars if needed (e.g. for loot rewards)
                if (combatActiveOnly && avatar.IsCombatActive() == false)
                    continue;

                if (nearbyPlayerIds != null)
                    nearbyPlayerIds.Add(avatar.OwnerId);

                if (nearbyPlayers != null)
                {
                    Player player = avatar.GetOwnerOfType<Player>();
                    if (player == null)
                    {
                        Logger.Warn("ComputeNearbyPlayersInternal(): player == null");
                        continue;
                    }

                    // NOTE: We are using List instead of Set like the client does here, change this if it causes issues
                    nearbyPlayers.Add(player);
                }

                numPlayers++;
            }

            return Math.Max(numPlayersMin, numPlayers);
        }

        public WorldEntity GetRandomTarget()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn<WorldEntity>(null, "GetRandomTarget(): powerProto == null");

            Region region = Owner?.Region;
            if (region == null) return Logger.WarnReturn<WorldEntity>(null, "GetRandomTarget(): region == null");

            // Populate potential target picker
            Picker<WorldEntity> picker = new(Game.Random);
            bool requiresLineOfSight = RequiresLineOfSight(powerProto);
            Sphere bounds = new(Owner.RegionLocation.Position, powerProto.Radius);

            foreach (WorldEntity entity in region.IterateEntitiesInVolume(bounds, new(EntityRegionSPContextFlags.ActivePartition)))
            {
                if (IsValidTarget(entity) == false)
                    continue;

                if (entity.IsTargetable(Owner) == false)
                    continue;

                if (requiresLineOfSight)
                {
                    Vector3? resultPosition = null;
                    if (PowerLOSCheck(Owner.RegionLocation, entity.RegionLocation.Position, entity.Id, ref resultPosition, LOSCheckAlongGround()) == false)
                        continue;
                }

                picker.Add(entity);
            }

            return picker.Empty() == false ? picker.Pick() : null;
        }

        #region State Accessors

        public bool PreventsNewMovementWhileActive()
        {
            if (Prototype == null) return false;

            if (Prototype.MovementPreventWhileActive)
                return true;

            return _activationPhase switch
            {
                PowerActivationPhase.ChannelStarting => Prototype.MovementPreventChannelStart,
                PowerActivationPhase.Channeling => Prototype.MovementPreventChannelLoop,
                PowerActivationPhase.LoopEnding => Prototype.MovementPreventChannelEnd,
                _ => false,
            };
        }

        public bool StopsMovementOnActivation()
        {
            if (Owner is Avatar avatar && IsGamepadMeleeMoveIntoRangePower() && avatar.PendingActionState == PendingActionState.MovingToRange)
                return false;

            return Prototype != null && Prototype.MovementStopOnActivate;
        }

        public bool IsToggledOn()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsToggledOn(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "IsToggledOn(): Owner == null");
            return IsToggledOn(powerProto, Owner);
        }

        public static bool IsToggledOn(PowerPrototype powerProto, WorldEntity owner)
        {
            return owner.Properties[PropertyEnum.PowerToggleOn, powerProto.DataRef];
        }

        public bool IsToggleInPrevRegion()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsToggleInPrevRegion(): powerProto == null");
            if (Owner == null) return Logger.WarnReturn(false, "IsToggleInPrevRegion(): Owner == null");
            return IsToggleInPrevRegion(powerProto, Owner);
        }

        public static bool IsToggleInPrevRegion(PowerPrototype powerProto, WorldEntity owner)
        {
            return owner.Properties[PropertyEnum.PowerToggleInPrevRegion, powerProto.DataRef];
        }

        public float GetEnduranceCost(ManaType manaType, bool useSecondaryResource)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetEnduranceCost(): powerProto == null");
            return GetEnduranceCost(Properties, manaType, powerProto, Owner, useSecondaryResource);
        }

        public static float GetEnduranceCost(PropertyCollection powerProperties, ManaType manaType, PowerPrototype powerProto, 
            WorldEntity powerOwner, bool canSkipCost)
        {
            if (canSkipCost && CanUseSecondaryResourceEffects(powerProperties, powerOwner.Properties) && powerProperties[PropertyEnum.SecondaryResourceNoEndurance])
                return 0f;

            float cost = powerProperties[PropertyEnum.EnduranceCost, manaType];

            return ModifyEnduranceCost(cost, manaType, powerProto, powerProperties, powerOwner, canSkipCost);
        }

        private float ModifyEnduranceCost(float cost, ManaType manaType, bool canSkipCost)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "ModifyEnduranceCost(): powerProto == null");
            return ModifyEnduranceCost(cost, manaType, powerProto, Properties, Owner, canSkipCost);
        }

        private static float ModifyEnduranceCost(float cost, ManaType manaType, PowerPrototype powerProto, PropertyCollection powerProperties,
            WorldEntity powerOwner, bool canSkipCost)
        {
            if (powerOwner == null) return Logger.WarnReturn(cost, "ModifyEnduranceCost(): owner == null");

            cost *= powerOwner.GetEnduranceCostMultiplier(manaType, powerProto, canSkipCost);
            cost *= LiveTuningManager.GetLivePowerTuningVar(powerProto, PowerTuningVar.ePTV_PowerCost);

            float minCost = 0f;

            if (powerProperties[PropertyEnum.EnduranceCostAllRemaining, manaType] || powerProperties[PropertyEnum.EnduranceCostAllRemaining, ManaType.TypeAll])
                minCost = powerOwner.Properties[PropertyEnum.Endurance];

            return MathF.Max(cost, minCost);
        }
        
        public float GetSecondaryResourceCost()
        {
            WorldEntity ultimateOwner = GetUltimateOwner();
            if (ultimateOwner == null)
                return 0f;

            return GetSecondaryResourceCost(Properties, ultimateOwner.Properties);
        }

        public static float GetSecondaryResourceCost(PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            float secondaryResourceCost = powerProperties[PropertyEnum.SecondaryResourceCost];

            int secondaryResourceCostPips = powerProperties[PropertyEnum.SecondaryResourceCostPips];
            float secondaryResourceCostPerPip = ownerProperties[PropertyEnum.SecondaryResourceCostPerPip];
            secondaryResourceCost += secondaryResourceCostPips * secondaryResourceCostPerPip;

            return secondaryResourceCost;
        }

        /// <summary>
        /// Returns the amount of endurance per second this <see cref="Power"/> costs as long as it is active.
        /// </summary>
        public float GetEnduranceCostRecurring(ManaType manaType, bool applyModifiers, bool canSkipCost)
        {
            float powerRecurringCost = Properties[PropertyEnum.PowerRecurringCost, manaType];

            if (applyModifiers == false)
                return powerRecurringCost;

            return ModifyEnduranceCost(powerRecurringCost, manaType, canSkipCost);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="Power"/> has a recurring cost for any <see cref="ManaType"/>.
        /// </summary>
        public bool HasEnduranceCostRecurring()
        {
            return GetEnduranceCostRecurring(ManaType.Type1, false, false) > 0f || GetEnduranceCostRecurring(ManaType.Type2, false, false) > 0f;
        }

        public bool IsOnCooldown()
        {
            if (Owner == null) return Logger.WarnReturn(false, "IsOnCooldown(): Owner == null");
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsOnCooldown(): powerProto == null");
            return Owner.IsPowerOnCooldown(powerProto);
        }

        public TimeSpan GetCooldownTimeRemaining()
        {
            if (Owner == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownTimeRemaining(): Owner == null");
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownTimeRemaining(): powerProto == null");
            return GetCooldownTimeRemaining(powerProto, Owner);
        }

        public static TimeSpan GetCooldownTimeRemaining(PowerPrototype powerProto, WorldEntity owner)
        {
            return owner.GetAbilityCooldownTimeRemaining(powerProto);
        }

        #endregion

        #region Data Accessors

        // NOTE: We have to use methods instead of properties here because we can't have static methods and properties share the same name.
        // NOTE: Do we actually need all these prototype null checks in instance methods?

        public PowerCategoryType GetPowerCategory()
        {
            return Prototype != null ? Prototype.PowerCategory : PowerCategoryType.None;
        }

        public static PowerCategoryType GetPowerCategory(PowerPrototype powerProto)
        {
            return powerProto.PowerCategory;
        }

        public bool IsNormalPower()
        {
            return GetPowerCategory() == PowerCategoryType.NormalPower;
        }

        public bool IsGameFunctionPower()
        {
            return GetPowerCategory() == PowerCategoryType.GameFunctionPower;
        }

        public bool IsEmotePower()
        {
            return GetPowerCategory() == PowerCategoryType.EmotePower;
        }

        public bool IsThrowablePower()
        {
            return GetPowerCategory() == PowerCategoryType.ThrowablePower;
        }

        public bool IsMissileEffect()
        {
            return GetPowerCategory() == PowerCategoryType.MissileEffect;
        }

        public static bool IsMissileEffect(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.MissileEffect;
        }

        public bool IsProcEffect()
        {
            return GetPowerCategory() == PowerCategoryType.ProcEffect;
        }

        public static bool IsProcEffect(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.ProcEffect;
        }

        public bool IsItemPower()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsItemPower(): powerProto == null");
            return IsItemPower(powerProto);
        }

        public static bool IsItemPower(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.ItemPower;
        }

        public bool IsRecurring()
        {
            return Prototype != null && Prototype.IsRecurring;
        }

        public PowerActivationType GetActivationType()
        {
            return Prototype != null ? Prototype.Activation : PowerActivationType.None;
        }

        public static PowerActivationType GetActivationType(PowerPrototype powerProto)
        {
            return powerProto.Activation;
        }

        public bool IsComboEffect()
        {
            return GetPowerCategory() == PowerCategoryType.ComboEffect;
        }

        public static bool IsComboEffect(PowerPrototype powerProto)
        {
            return GetPowerCategory(powerProto) == PowerCategoryType.ComboEffect;
        }

        public static bool IsUltimatePower(PowerPrototype powerProto)
        {
            return powerProto.IsUltimate;
        }

        public bool IsTalentPower()
        {
            return Prototype is SpecializationPowerPrototype;
        }

        public static bool IsTalentPower(PowerPrototype powerProto)
        {
            return powerProto is SpecializationPowerPrototype;
        }

        public bool IsTravelPower()
        {
            return Prototype != null && Prototype.IsTravelPower;
        }

        public static bool IsTravelPower(PowerPrototype powerProto)
        {
            return powerProto.IsTravelPower;
        }

        public bool IsMovementPower()
        {
            return Prototype is MovementPowerPrototype;
        }

        public static bool IsMovementPower(PowerPrototype powerProto)
        {
            return powerProto is MovementPowerPrototype;
        }

        public bool IsPartOfAMovementPower()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsPartOfAMovementPower(): powerProto == null");
            return IsPartOfAMovementPower(powerProto);
        }

        public static bool IsPartOfAMovementPower(PowerPrototype powerProto)
        {
            return powerProto is MovementPowerPrototype;
        }

        public bool IsHighFlyingPower()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsHighFlyingPower(): powerProto == null");
            return powerProto.IsHighFlyingPower;
        }

        public bool IsChannelingPower()
        {
            return GetTotalChannelingTime() != TimeSpan.Zero && IsRecurring() == false;
        }

        public bool NeedsTarget()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "NeedsTarget(): powerProto == null");
            return NeedsTarget(powerProto);
        }

        public static bool NeedsTarget(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "NeedsTarget(): stylePrototype == null");
            return stylePrototype.NeedsTarget;
        }

        public bool AlwaysTargetsMousePosition()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "AlwaysTargetsMousePosition(): powerProto == null");
            return AlwaysTargetsMousePosition(powerProto);
        }

        public static bool AlwaysTargetsMousePosition(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "AlwaysTargetsMousePosition(): stylePrototype == null");
            return stylePrototype.AlwaysTargetMousePos;
        }

        public bool ShouldOrientToTarget()
        {
            TargetingStylePrototype stylePrototype = TargetingStylePrototype;
            if (stylePrototype == null) return Logger.WarnReturn(false, "ShouldOrientToTarget(): stylePrototype == null");
            return stylePrototype.TurnsToFaceTarget;
        }

        public static bool ShouldOrientToTarget(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "ShouldOrientToTarget(): stylePrototype == null");
            return stylePrototype.TurnsToFaceTarget;
        }

        public bool DisableOrientationWhileActive()
        {
            TargetingStylePrototype stylePrototype = TargetingStylePrototype;
            if (stylePrototype == null) return Logger.WarnReturn(false, "DisableOrientationWhileActive(): stylePrototype == null");
            return stylePrototype.DisableOrientationDuringPower;
        }

        public static bool DisableOrientationWhileActive(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "DisableOrientationWhileActive(): stylePrototype == null");
            return stylePrototype.DisableOrientationDuringPower;
        }

        public bool OrientsTowardsTargetWhileActive()
        {
            return Prototype != null && Prototype.MovementOrientToTargetOnActivate;
        }

        public bool TargetsAOE()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TargetsAOE(): powerProto == null");
            return TargetsAOE(powerProto);
        }

        public static bool TargetsAOE(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "TargetsAOE(): stylePrototype == null");
            return stylePrototype.TargetsAOE();
        }

        public bool IsOwnerCenteredAOE()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsOwnerCenteredAOE(): powerProto == null");
            return IsOwnerCenteredAOE(powerProto);
        }

        public static bool IsOwnerCenteredAOE(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(false, "IsOwnerCenteredAOE(): stylePrototype == null");
            return stylePrototype.AOESelfCentered;
        }

        public float GetAOERadius()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetAOERadius(): Owner == null");
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetAOERadius(): powerProto == null");
            return GetAOERadius(powerProto, Owner.Properties);
        }

        public static float GetAOERadius(PowerPrototype powerProto, PropertyCollection ownerProperties = null)
        {
            float radius = powerProto.Radius;
            radius *= GetAOESizePctModifier(powerProto, ownerProperties);
            return radius;
        }

        public static float GetAOESizePctModifier(PowerPrototype powerProto, PropertyCollection ownerProperties)
        {
            float aoeSizePctModifier = 1f;

            if (ownerProperties != null)
            {
                aoeSizePctModifier += ownerProperties[PropertyEnum.AOESizePctModifier];
                AccumulateKeywordProperties(ref aoeSizePctModifier, powerProto, ownerProperties, ownerProperties, PropertyEnum.AOESizePctModifierKeyword);
            }

            return MathF.Max(aoeSizePctModifier, 0.1f);  // 0.1f - smallest possible AoE size modifier
        }

        public float GetAOEAngle()
        {
            var powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetAOEAngle(): powerProto == null");
            return GetAOEAngle(powerProto);
        }

        public static float GetAOEAngle(PowerPrototype powerProto)
        {
            var styleProto = powerProto.GetTargetingStyle();
            if (styleProto == null) return Logger.WarnReturn(0f, "GetAOEAngle(): styleProto == null");

            if (styleProto.TargetingShape == TargetingShapeType.CircleArea)
                return 360.0f;

            return styleProto.AOEAngle switch
            {
                AOEAngleType._0     => 0.0f,
                AOEAngleType._1     => 1.0f,
                AOEAngleType._10    => 10.0f,
                AOEAngleType._30    => 30.0f,
                AOEAngleType._45    => 45.0f,
                AOEAngleType._60    => 60.0f,
                AOEAngleType._90    => 90.0f,
                AOEAngleType._120   => 120.0f,
                AOEAngleType._180   => 180.0f,
                AOEAngleType._240   => 240.0f,
                AOEAngleType._300   => 300.0f,
                AOEAngleType._360   => 360.0f,
                _                   => 0.0f
            };
        }

        public float GetTargetingWidth()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetTargetingWidth(): Owner == null");
            var powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetTargetingWidth(): powerProto == null");
            return GetTargetingWidth(powerProto, Owner.Properties);
        }

        public static float GetTargetingWidth(PowerPrototype powerProto, PropertyCollection ownerProperties)
        {
            var styleProto = powerProto.GetTargetingStyle();
            if (styleProto == null) return Logger.WarnReturn(0f, "GetTargetingWidth(): styleProto == null");
            return styleProto.Width * GetAOESizePctModifier(powerProto, ownerProperties);
        }

        public TargetingShapeType GetTargetingShape()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TargetingShapeType.None, "GetTargetingShape(): powerProto == null");
            return GetTargetingShape(powerProto);
        }

        public static TargetingShapeType GetTargetingShape(PowerPrototype powerProto)
        {
            TargetingStylePrototype stylePrototype = powerProto.GetTargetingStyle();
            if (stylePrototype == null) return Logger.WarnReturn(TargetingShapeType.None, "GetTargetingShape(): stylePrototype == null");
            return stylePrototype.TargetingShape;
        }

        public bool IsMelee()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsMelee(): powerProto == null");
            return IsMelee(powerProto);
        }

        public static bool IsMelee(PowerPrototype powerProto)
        {
            TargetingReachPrototype reachProto = powerProto.GetTargetingReach();
            if (reachProto == null) return Logger.WarnReturn(false, "IsMelee(): reachProto == null");
            return reachProto.Melee;
        }

        public static bool IsSummoned(PowerPrototype powerProto)
        {
            TargetingReachPrototype reachProto = powerProto.GetTargetingReach();
            if (reachProto == null) return Logger.WarnReturn(false, "IsSummoned(): reachProto == null");
            return reachProto.TargetsEntitiesInInventory == InventoryConvenienceLabel.Summoned;
        }

        public bool IsGamepadMeleeMoveIntoRangePower()
        {
            // V48_TODO: Remove this?
            return false;
        }

        public float GetRange()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetRange(): Owner == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetRange(): powerProto == null");

            float range;

            if (Owner is Avatar avatar && avatar.IsUsingGamepadInput && GetGamepadRange() > 0f)
                range = GetGamepadRange();
            else
                range = GetRange(powerProto, Properties, Owner.Properties);

            if (powerProto.PowerCategory == PowerCategoryType.MissileEffect)
                range = Math.Max(range, Owner.EntityCollideBounds.Radius);

            return range;
        }

        public static float GetRange(PowerPrototype powerProto, PropertyCollection powerProperties, PropertyCollection ownerProperties)
        {
            float range = IsOwnerCenteredAOE(powerProto) ? GetAOERadius(powerProto) : powerProto.GetRange(powerProperties, ownerProperties);

            if (ownerProperties != null && range > 0f && IsMelee(powerProto) == false && IsOwnerCenteredAOE(powerProto) == false)
            {
                range += ownerProperties[PropertyEnum.RangeModifier];

                // Calculate and apply range multiplier
                float rangeMult = 1f;
                AccumulateKeywordProperties(ref rangeMult, powerProto, ownerProperties, ownerProperties, PropertyEnum.RangeModifierPctKeyword);
                range *= rangeMult;
            }

            return range;
        }

        public float GetGamepadRange()
        {
            // V48_TODO: Remove this?
            return 0f;
        }

        public float GetApplicationRange()
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetApplicationRange(): Owner == null");
            return TargetsAOE() ? GetAOERadius() : GetRange();
        }

        public float GetKnockbackDistance(WorldEntity target)
        {
            if (Owner == null) return Logger.WarnReturn(0f, "GetKnockbackDistance(): Owner == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetKnockbackDistance(): powerProto == null");

            return GetKnockbackDistance(target, Owner.Id, powerProto, Properties);
        }

        public static float GetKnockbackDistance(WorldEntity target, ulong userId, PowerPrototype powerProto,
            PropertyCollection powerProperties, Vector3 secondaryTargetPosition = default)
        {
            if (userId == Entity.InvalidId) return Logger.WarnReturn(0f, "GetKnockbackDistance(): userId == Entity.InvalidId");

            Game game = target.Game;
            if (game == null) return Logger.WarnReturn(0f, "GetKnockbackDistance(): game == null");

            // Calculate knockback distance
            float knockbackDistance = 0f;

            if (powerProperties.HasProperty(PropertyEnum.KnockbackToSource))
            {
                WorldEntity user = game.EntityManager.GetEntity<WorldEntity>(userId);
                if (user != null)
                {
                    float distanceToTarget = Vector3.Distance2D(target.RegionLocation.Position, user.RegionLocation.Position);
                    float combinedRadiuses = target.Bounds.Radius + user.Bounds.Radius;
                    knockbackDistance = -distanceToTarget + combinedRadiuses + powerProperties[PropertyEnum.KnockbackDistance];
                }
            }
            else if (powerProto is MovementPowerPrototype movementPowerProto && movementPowerProto.MoveToSecondaryTarget)
            {
                float distanceToSecondaryTarget = Vector3.Distance2D(target.RegionLocation.Position, secondaryTargetPosition);
                knockbackDistance = distanceToSecondaryTarget + powerProperties[PropertyEnum.KnockbackDistance];
            }
            else
            {
                knockbackDistance = powerProperties[PropertyEnum.KnockbackDistance];
            }

            // Apply knockback resist
            if (target.Id != userId)
                knockbackDistance *= Math.Clamp(1f - target.Properties[PropertyEnum.KnockbackResist], 0f, 1f);

            return knockbackDistance;
        }

        public float GetProjectileSpeed(float distance)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetProjectileSpeed(): powerProto == null");
            return GetProjectileSpeed(powerProto, Properties, Owner.Properties, distance);
        }

        public float GetProjectileSpeed(Vector3 userPosition, Vector3 targetPosition)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetProjectileSpeed(): powerProto == null");
            return GetProjectileSpeed(powerProto, Properties, Owner.Properties, userPosition, targetPosition);
        }

        public static float GetProjectileSpeed(PowerPrototype powerProto, PropertyCollection powerProperties, PropertyCollection ownerProperties,
            Vector3 userPosition, Vector3 targetPosition)
        {
            float distance = 0f;

            if (powerProto.ProjectileTimeToImpactOverride > 0f)
                distance = Vector3.Distance(userPosition, targetPosition);

            return GetProjectileSpeed(powerProto, powerProperties, ownerProperties, distance);
        }

        public static float GetProjectileSpeed(PowerPrototype powerProto, PropertyCollection powerProperties, PropertyCollection ownerProperties, float distance)
        {
            float speed;

            if (powerProto.ProjectileTimeToImpactOverride > 0f)
                speed = distance / powerProto.ProjectileTimeToImpactOverride;
            else
                speed = powerProto.GetProjectileSpeed(powerProperties, ownerProperties);

            if (ownerProperties != null)
                speed *= 1f + powerProperties[PropertyEnum.MissileSpeedBonus];

            return speed;
        }

        public bool RequiresLineOfSight()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "RequiresLineOfSight(): powerProto == null");
            return RequiresLineOfSight(powerProto);
        }

        public static bool RequiresLineOfSight(PowerPrototype powerProto)
        {
            TargetingReachPrototype targetingReachProto = powerProto.GetTargetingReach();
            if (targetingReachProto == null) return Logger.WarnReturn(false, "RequiresLineOfSight(): targetingReachProto == null");
            return targetingReachProto.RequiresLineOfSight;
        }

        public bool LOSCheckAlongGround()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "LOSCheckAlongGround(): powerProto == null");

            TargetingReachPrototype targetingReachProto = powerProto.GetTargetingReach();
            if (targetingReachProto == null) return Logger.WarnReturn(false, "LostCheckAlongGround(): targetingReachProto == null");

            return targetingReachProto.LOSCheckAlongGround;
        }

        public float GetAnimSpeed()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(1f, "GetAnimSpeed(): powerProto == null");
            return GetAnimSpeed(powerProto, Owner, this);
        }

        public static float GetAnimSpeed(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            // -1 is invalid speed cache
            if (power != null && power.AnimSpeedCache >= 0f)
                return power.AnimSpeedCache;

            float animSpeed = 1f;

            // No owner to get speed bonuses from
            if (owner == null)
                return Logger.WarnReturn(animSpeed, "GetAnimSpeed(): powerOwner == null");

            // Movement power animations don't scale with cast speed
            if (IsMovementPower(powerProto) == false)
                animSpeed = owner.GetCastSpeedPct(powerProto);

            // Update cache
            if (power != null)
                power.AnimSpeedCache = animSpeed;

            return animSpeed;
        }

        public TimeSpan GetAnimationTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetAnimationTime(): powerProto == null");
            return GetAnimationTime(powerProto, Owner, this);
        }

        public static TimeSpan GetAnimationTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            if (owner == null) return Logger.WarnReturn(TimeSpan.Zero, "GetAnimationTime(): owner == null");

            TimeSpan baseTime = powerProto.GetAnimationTime(owner.GetOriginalWorldAsset(), owner.GetEntityWorldAsset());
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            TimeSpan result = animSpeed > 0f ? baseTime / animSpeed : TimeSpan.Zero;

            // What exactly are these Bad Things? o_o
            if (baseTime != TimeSpan.Zero && result <= TimeSpan.Zero)
            {
                Logger.Warn($"GetAnimationTime(): The following power has a non-zero animation time, but bonuses on the character are such" +
                    $" that the time is being reduced to 0, which will cause Bad Things to happen...\n[{powerProto}]");
            }

            return result;
        }

        public float GetAnimContactTimePercent()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(0f, "GetAnimContactTimePercent(): powerProto == null");
            return GetAnimContactTimePercent(powerProto, Owner);
        }

        public static float GetAnimContactTimePercent(PowerPrototype powerProto, WorldEntity owner)
        {
            if (owner == null) return Logger.WarnReturn(0f, "GetAnimContactTimePercent(): powerProto == null");

            float powerContactPctWhenMoving = powerProto.Properties != null ? powerProto.Properties[PropertyEnum.PowerContactPctWhenMoving] : -1f;

            if (powerContactPctWhenMoving >= 0f)
            {
                if (owner.Locomotor != null && owner.Locomotor.IsLocomoting)
                    return powerContactPctWhenMoving;
            }

            return powerProto.GetContactTimePercent(owner.GetOriginalWorldAsset(), owner.GetEntityWorldAsset());
        }

        public TimeSpan GetChannelStartTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelStartTime(): powerProto == null");
            return GetChannelStartTime(powerProto, Owner, this);
        }

        public static TimeSpan GetChannelStartTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            return powerProto.ChannelStartTime * timeMult;
        }

        public TimeSpan GetChannelLoopTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelLoopTime(): powerProto == null");
            return GetChannelLoopTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetChannelLoopTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            if (owner == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelLoopTime(): owner == null");

            float timeMult = 1f;

            if (powerProto.IsRecurring)
            {
                float animSpeed = GetAnimSpeed(powerProto, owner, power);
                timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            }

            if (powerProto.OmniDurationBonusExclude == false)
            {
                timeMult += owner.Properties[PropertyEnum.OmniDurationBonusPct];
                timeMult = MathF.Max(timeMult, 0.5f);
            }

            return powerProto.GetChannelLoopTime(powerProperties, owner.Properties) * timeMult;
        }

        public TimeSpan GetChannelEndTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelEndTime(): powerProto == null");
            return GetChannelEndTime(powerProto, Owner, this);
        }

        public static TimeSpan GetChannelEndTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            return powerProto.ChannelEndTime * timeMult;
        }

        public TimeSpan GetChannelMinTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChannelMinTime(): powerProto == null");
            return GetChannelMinTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetChannelMinTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            if (powerProto.IsRecurring)
            {
                TimeSpan channelStartTime = GetChannelStartTime(powerProto, owner, power);
                TimeSpan channelLoopTime = GetChannelLoopTime(powerProto, owner, powerProperties, power);
                TimeSpan channelMinTime = powerProto.ChannelMinTime;
                return Clock.Max(channelStartTime + channelLoopTime, channelMinTime);
            }

            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            return powerProto.ChannelMinTime * timeMult;
        }

        public TimeSpan GetTotalChannelingTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetTotalChannelingTime(): powerProto == null");
            return GetTotalChannelingTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetTotalChannelingTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            TimeSpan channelStartTime = GetChannelStartTime(powerProto, owner, power);
            TimeSpan channelLoopTime = GetChannelLoopTime(powerProto, owner, powerProperties, power);
            TimeSpan channelEndTime = GetChannelEndTime(powerProto, owner, power);
            return channelStartTime + channelLoopTime + channelEndTime;
        }

        public TimeSpan GetChargingTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetChargingTime(): powerProto == null");
            return GetChargingTime(powerProto, Owner, this);
        }

        public static TimeSpan GetChargingTime(PowerPrototype powerProto, WorldEntity owner, Power power)
        {
            float animSpeed = GetAnimSpeed(powerProto, owner, power);
            return animSpeed > 0f ? powerProto.ChargeTime / animSpeed : TimeSpan.Zero;  // Avoid division by 0 / negative
        }

        public TimeSpan GetActivationTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetActivationTime(): powerProto == null");

            // Channeled powers
            if (GetChannelLoopTime() > TimeSpan.Zero)
            {
                if (powerProto.IsRecurring == false)
                    return GetChannelStartTime() * GetAnimContactTimePercent(powerProto, Owner);

                return GetChannelStartTime() + (GetChannelLoopTime() * GetAnimContactTimePercent(powerProto, Owner));
            }

            // Non-channeled powers
            float animSpeed = GetAnimSpeed();
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;     // Avoid division by 0 / negative
            TimeSpan oneOffAnimContactTime = powerProto.GetOneOffAnimContactTime(Owner.GetOriginalWorldAsset(), Owner.GetEntityWorldAsset());
            return GetChargingTime() + (oneOffAnimContactTime * timeMult);
        }

        public TimeSpan GetFullExecutionTime()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetFullExecutionTime(): powerProto == null");
            return GetFullExecutionTime(powerProto, Owner, Properties, this);
        }

        public static TimeSpan GetFullExecutionTime(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            TimeSpan chargingTime = GetChargingTime(powerProto, owner, power);
            TimeSpan animationTime = GetAnimationTime(powerProto, owner, power);
            TimeSpan standardExecutionTime = chargingTime + animationTime;

            TimeSpan totalChannelingTime = GetTotalChannelingTime(powerProto, owner, powerProperties, power);
            if (totalChannelingTime > TimeSpan.Zero)
            {
                if (standardExecutionTime > TimeSpan.Zero)
                {
                    Logger.Warn($"GetFullExecutionTime(): The following power has non-zero charging/standard-anim time AND non-zero channel time," +
                        $" which are incompatible! Using the channel time only.\nPower: [{powerProto}]");
                }

                return totalChannelingTime;
            }

            return standardExecutionTime;
        }

        public TimeSpan GetPayloadDeliveryDelay(PowerPayload payload)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetPayloadDeliveryTime(): powerProto == null");

            TimeSpan delay = TimeSpan.FromMilliseconds(powerProto.PostContactDelayMS);

            if (powerProto is not MissilePowerPrototype)
            {
                Vector3 userPosition = payload.PowerOwnerPosition;
                Vector3 targetPosition = payload.TargetPosition;

                float projectileSpeed = GetProjectileSpeed(userPosition, targetPosition);
                if (projectileSpeed > 0f)
                {
                    float distance = Vector3.Length(targetPosition - userPosition);
                    if (distance > 0f)
                        delay += TimeSpan.FromSeconds(distance / projectileSpeed);
                }
            }

            return delay;
        }

        public TimeSpan GetCooldownDuration()
        {
            if (Owner == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownDuration(): Owner == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(TimeSpan.Zero, "GetCooldownDuration(): powerProto == null");

            return GetCooldownDuration(powerProto, Owner, Properties);
        }

        public static TimeSpan GetCooldownDuration(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties)
        {
            // First check if the power is already on cooldown and return that if it is
            TimeSpan cooldownTimeElapsed = owner.GetAbilityCooldownTimeElapsed(powerProto);
            TimeSpan cooldownDurationForLastActivation = owner.GetAbilityCooldownDurationUsedForLastActivation(powerProto);

            if (cooldownTimeElapsed <= cooldownDurationForLastActivation)
                return cooldownDurationForLastActivation;

            // Calculate new cooldown duration
            return CalcCooldownDuration(powerProto, owner, powerProperties);
        }

        public static TimeSpan CalcCooldownDuration(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, TimeSpan baseCooldown = default)
        {
            if (baseCooldown == default)
                baseCooldown = powerProto.GetCooldownDuration(powerProperties, owner.Properties);

            // Calculate cooldown modifier percentage
            float cooldownModifierPct = owner.Properties[PropertyEnum.CooldownModifierPctGlobal];
            cooldownModifierPct += owner.Properties[PropertyEnum.CooldownModifierPctForPower, powerProto.DataRef];
            AccumulateKeywordProperties(ref cooldownModifierPct, powerProto, owner.Properties, owner.Properties, PropertyEnum.CooldownModifierPctForKeyword);

            // Calculate flat cooldown modifier
            long flatCooldownModifierMS = owner.Properties[PropertyEnum.CooldownModifierMSForPower, powerProto.DataRef];
            AccumulateKeywordProperties(ref flatCooldownModifierMS, powerProto, owner.Properties, owner.Properties, PropertyEnum.CooldownModifierMSForKeyword);
            TimeSpan flatCooldownModifier = TimeSpan.FromMilliseconds(flatCooldownModifierMS);

            // Calculate cooldown
            TimeSpan cooldown = baseCooldown;
            cooldown += flatCooldownModifier;               // Apply flat modifier to base
            cooldown += cooldown * cooldownModifierPct;     // Apply percentage modifier

            // Get interrupt cooldown and use it to override the value we calculated if its longer
            TimeSpan interruptCooldown = owner.GetPowerInterruptCooldown(powerProto);
            cooldown = Clock.Max(cooldown, interruptCooldown);

            // Make we don't get a negative cooldown
            return Clock.Max(cooldown, TimeSpan.Zero);
        }

        public static bool IsCooldownOnPlayer(PowerPrototype powerProto)
        {
            return powerProto.CooldownOnPlayer;
        }

        public static bool IsCooldownPersistent(PowerPrototype powerProto)
        {
            return powerProto.CooldownIsPersistentToDatabase || powerProto.IsUltimate;
        }

        public bool TriggersComboPowerOnEvent(PowerEventType eventType)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TriggersComboPowerOnEvent(): powerProto == null");

            return powerProto.TriggersComboPowerOnEvent(eventType, Properties, Owner);
        }

        public bool IsOnExtraActivation()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsOnExtraActivation(): powerProto == null");
            return IsOnExtraActivation(powerProto, Owner);
        }

        public static bool IsOnExtraActivation(PowerPrototype powerProto, WorldEntity owner)
        {
            if (owner == null) return Logger.WarnReturn(false, "IsOnExtraActivation(): owner == null");
            if (powerProto.DataRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "IsOnExtraActivation(): powerProto.DataRef == PrototypeId.Invalid");

            if (powerProto.ExtraActivation == null || powerProto.ExtraActivation is not ExtraActivateOnSubsequentPrototype extraActivate)
                return false;

            if (extraActivate.ExtraActivateEffect == SubsequentActivateType.RepeatActivation)
                return false;

            int powerActivationCount = owner.Properties[PropertyEnum.PowerActivationCount, powerProto.DataRef];
            return extraActivate.ExtraActivateEffect == SubsequentActivateType.DestroySummonedEntity && powerActivationCount % 2 == 1;
        }

        public bool IsToggled()
        {
            return Prototype != null && Prototype.IsToggled;
        }

        public bool IsCancelledOnDamage()
        {
            return Prototype != null && Prototype.CancelledOnDamage;
        }

        public bool IsCancelledOnMove()
        {
            return Prototype != null && Prototype.CancelledOnMove;
        }

        public bool IsCancelledOnRelease()
        {
            return Prototype != null && Prototype.CancelledOnButtonRelease;
        }

        public bool IsCancelledOnTargetKilled()
        {
            return Prototype != null && Prototype.CancelledOnTargetKilled;
        }

        public bool IsNonCancellableChannelPower()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsNonCancellableChannelPower(): powerProto == null");

            return powerProto.CanBeInterrupted == false && IsCancelledOnRelease() == false && IsCancelledOnMove() == false && IsChannelingPower();
        }

        public bool IsExclusiveActivation()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsExclusiveActivation(): powerProto == null");
            return IsExclusiveActivation(powerProto, Owner, Properties, this);
        }

        public static bool IsExclusiveActivation(PowerPrototype powerProto, WorldEntity owner, PropertyCollection powerProperties, Power power)
        {
            if (owner == null) return Logger.WarnReturn(false, "IsExclusiveActivation(): owner == null");

            if (GetActivationType(powerProto) == PowerActivationType.Passive)
                return false;

            if (powerProto.ForceNonExclusive)
                return false;

            if (IsProcEffect(powerProto) || IsComboEffect(powerProto) || IsMissileEffect(powerProto) || powerProto.IsToggled || IsItemPower(powerProto))
            {
                if (GetFullExecutionTime(powerProto, owner, powerProperties, power) == TimeSpan.Zero)
                    return powerProto is MovementPowerPrototype movementPowerProto && movementPowerProto.ConstantMoveTime == false;
            }

            return IsOnExtraActivation(powerProto, owner) == false;
        }

        public bool IsSecondActivateOnRelease()
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsSecondActivateOnRelease(): powerProto == null");

            if (powerProto.ExtraActivation == null)
                return false;

            return Prototype.ExtraActivation is SecondaryActivateOnReleasePrototype;
        }

        public bool IsContinuous()
        {
            if (IsToggled())
                return false;

            // <= 50 ms is too fast to be a continuous power - is this related to game fixed time update time?
            if (GetFullExecutionTime().TotalMilliseconds <= 50)
                return false;

            if (GetCooldownDuration() > TimeSpan.Zero)
                return false;

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "IsContinuous(): powerProto == null");

            if (powerProto.DisableContinuous)
                return false;

            if (powerProto.PowerCategory != PowerCategoryType.NormalPower)
                return false;

            if (powerProto.ExtraActivation != null)
                return false;

            if (powerProto.Activation == PowerActivationType.Passive || powerProto.Activation == PowerActivationType.TwoStageTargeted)
                return false;

            if (IsCancelledOnRelease())
                return false;

            if (IsSecondActivateOnRelease())
                return false;

            // After facing many challenges, we have reached the end and earned our right to be a continuous power
            return true;
        }

        public bool IsUseableWhileDead()
        {
            return Prototype != null && Prototype.IsUseableWhileDead;
        }

        public bool CanBeUsedInRegion(Region region)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CanBeUsedInRegino(): powerProto == null");
            return CanBeUsedInRegion(powerProto, Properties, region);
        }

        public static bool CanBeUsedInRegion(PowerPrototype powerProto, PropertyCollection powerProperties, Region region)
        {
            if (region == null) return false;
            RegionPrototype regionPrototype = region.Prototype;
            if (regionPrototype == null) return Logger.WarnReturn(false, "CanBeUsedInRegion(): regionPrototype == null");

            PropertyCollection properties = powerProperties ?? powerProto.Properties;

            // Check power properties
            if (powerProto.Activation != PowerActivationType.Passive && properties != null)
            {
                // Check if we can use the power in the current region type (town / public / private / etc)
                if (properties[PropertyEnum.PowerUsePreventIn, (int)regionPrototype.Behavior])
                    return false;

                // Check keywords that prevent powers from being used in regions
                foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.PowerUsePreventInRegionKwd))
                {
                    if (kvp.Value == false)
                        continue;

                    Property.FromParam(kvp.Key, 0, out PrototypeId regionKeywordRef);
                    if (regionKeywordRef == PrototypeId.Invalid)
                        Logger.Warn($"CanBeUsedInRegion(): Power has invalid PowerUsePreventInRegionKwd!\n Power Prototype: {powerProto}");

                    if (regionPrototype.HasKeyword(regionKeywordRef))
                        return false;
                }

                // Check keywords that are required for a power to be used in a region
                foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.PowerUseRequiresRegionKwd))
                {
                    if (kvp.Value == false)
                        continue;

                    Property.FromParam(kvp.Key, 0, out PrototypeId regionKeywordRef);
                    if (regionKeywordRef == PrototypeId.Invalid)
                        Logger.Warn($"CanBeUsedInRegion(): Power has invalid PowerUseRequiresRegionKwd!\n Power Prototype: {powerProto}");

                    if (regionPrototype.HasKeyword(regionKeywordRef) == false)
                        return false;
                }
            }

            // Check region keyword blacklist
            if (regionPrototype.PowerKeywordBlacklist.HasValue() && powerProto.Keywords.HasValue())
            {
                foreach (PrototypeId powerKeywordRef in regionPrototype.PowerKeywordBlacklist)
                {
                    if (powerProto.HasKeyword(powerKeywordRef.As<KeywordPrototype>()))
                        return false;
                }
            }

            return true;
        }

        public static bool CanCauseHitReact(PowerPrototype powerProto, Agent target)
        {
            if (powerProto.CanCauseHitReact == false)
                return false;

            AgentPrototype agentProto = target.AgentPrototype;
            if (agentProto == null) return Logger.WarnReturn(false, "CanCauseHitReact(): agentProto == null");

            if (agentProto.HitReactCondition == PrototypeId.Invalid)
                return false;

            if (target.IsHitReactionOnCooldown())
                return false;

            return true;
        }

        public static T FindPowerPrototype<T>(PowerPrototype powerProto) where T: PowerPrototype
        {
            if (powerProto == null) return Logger.WarnReturn<T>(null, "FindPowerPrototype(): powerProto == null");

            if (powerProto is T typedPowerProto)
                return typedPowerProto;

            if (powerProto.ActionsTriggeredOnPowerEvent.HasValue())
            {
                foreach (PowerEventActionPrototype triggeredPowerEventProto in powerProto.ActionsTriggeredOnPowerEvent)
                {
                    if (triggeredPowerEventProto.EventAction != PowerEventActionType.UsePower)
                        continue;

                    if (triggeredPowerEventProto.Power == PrototypeId.Invalid)
                        return Logger.WarnReturn<T>(null, $"FindPowerPrototype(): Infinite loop detected in {powerProto}!");

                    typedPowerProto = FindPowerPrototype<T>(triggeredPowerEventProto.Power.As<PowerPrototype>());
                    if (typedPowerProto != null)
                        return typedPowerProto;
                }
            }

            return null;
        }

        #endregion

        #region Stat Calculations

        public static float GetDamageRatingMult(float damageRating, PropertyCollection userProperties, WorldEntity target)
        {
            CombatGlobalsPrototype combatGlobals = GameDatabase.CombatGlobalsPrototype;
            if (combatGlobals == null) return Logger.WarnReturn(0f, "GetDamageRatingMult(): combatGlobals == null");

            EvalPrototype damageRatingEval = combatGlobals.EvalDamageRatingFormula;
            if (damageRatingEval == null) return Logger.WarnReturn(0f, "GetDamageRatingMult(): damageRatingEval == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, userProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
            evalContext.SetVar_Float(EvalContext.Var1, damageRating);

            return Eval.RunFloat(damageRatingEval, evalContext);
        }

        public static float GetCritChance(PowerPrototype powerProto, PropertyCollection userProperties, WorldEntity target,
            ulong userEntityId, PrototypeId keywordProtoRef = PrototypeId.Invalid, int targetLevelOverride = -1)
        {
            CombatGlobalsPrototype combatGlobals = GameDatabase.CombatGlobalsPrototype;
            if (combatGlobals == null) return Logger.WarnReturn(0f, "GetCritChance(): combatGlobals == null");

            EvalPrototype critEval = combatGlobals.EvalCritChanceFormula;
            if (critEval == null) return Logger.WarnReturn(0f, "GetCritChance(): critEval == null");

            // Start calculating crit rating for the user
            float critRatingAdd = userProperties[PropertyEnum.CritRatingBonusAdd];
            float critRatingMult = 1f + userProperties[PropertyEnum.CritRatingBonusMult];
            float critChancePctAdd = userProperties[PropertyEnum.CritChancePctAdd];

            // Apply power bonuses
            critRatingAdd += userProperties[PropertyEnum.CritRatingPowerBonusAdd];
            critRatingMult += userProperties[PropertyEnum.CritRatingPowerBonusMult];

            // Apply targeted crit bonus
            ulong targetedCritBonusId = target.Properties[PropertyEnum.TargetedCritBonusId];
            if (userEntityId != Entity.InvalidId && targetedCritBonusId == userEntityId)
                critRatingAdd += target.Properties[PropertyEnum.TargetedCritBonus];

            // Apply keyword bonuses
            if (powerProto != null)
            {
                AccumulateKeywordProperties(ref critRatingAdd, powerProto, userProperties, userProperties, PropertyEnum.CritRatingBonusAddPowerKeyword);
                AccumulateKeywordProperties(ref critRatingMult, powerProto, userProperties, userProperties, PropertyEnum.CritRatingBonusMultPowerKeyword);
                AccumulateKeywordProperties(ref critChancePctAdd, powerProto, userProperties, userProperties, PropertyEnum.CritChancePctAddPowerKeyword);
            }
            else if (keywordProtoRef != PrototypeId.Invalid)
            {
                critRatingAdd += userProperties[PropertyEnum.CritRatingBonusAddPowerKeyword, keywordProtoRef];
                critRatingMult += userProperties[PropertyEnum.CritRatingBonusMultPowerKeyword, keywordProtoRef];
                critChancePctAdd += userProperties[PropertyEnum.CritChancePctAddPowerKeyword, keywordProtoRef];
            }

            // Apply target keyword crit bonus
            target.AccumulateKeywordProperties(PropertyEnum.CritRatingBonusVsTargetKeyword, userProperties, ref critRatingAdd);            

            // Prepare int arguments for context data
            int critRating = (int)(critRatingAdd * MathF.Max(critRatingMult, 0f));
            int critChancePctAddInt = MathHelper.RoundToInt(critChancePctAdd * 100f);
            int userLevel = Math.Max(1, userProperties[PropertyEnum.CombatLevel]);
            int targetLevel = targetLevelOverride >= 0 ? targetLevelOverride : target.CombatLevel;

            // Run eval
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, userProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
            evalContext.SetVar_Int(EvalContext.Var1, critRating);
            evalContext.SetVar_Int(EvalContext.Var2, critChancePctAddInt);
            evalContext.SetVar_Int(EvalContext.Var3, userLevel);
            evalContext.SetVar_Int(EvalContext.Var4, targetLevel);

            return Eval.RunFloat(critEval, evalContext);
        }

        public static float GetSuperCritChance(PowerPrototype powerProto, PropertyCollection userProperties, WorldEntity target, int targetLevelOverride = -1)
        {
            CombatGlobalsPrototype combatGlobals = GameDatabase.CombatGlobalsPrototype;
            if (combatGlobals == null) return Logger.WarnReturn(0f, "GetSuperCritChance(): combatGlobals == null");

            EvalPrototype superCritEval = combatGlobals.EvalSuperCritChanceFormula;
            if (superCritEval == null) return Logger.WarnReturn(0f, "GetSuperCritChance(): superCritEval == null");

            // Start calculating super crit rating for the user
            float superCritRatingAdd = userProperties[PropertyEnum.SuperCritRatingBonusAdd];
            float superCritRatingMult = 1f + userProperties[PropertyEnum.SuperCritRatingBonusMult];
            float superCritChancePctAdd = userProperties[PropertyEnum.SuperCritChancePctAdd];

            // Apply power bonuses
            superCritRatingAdd += userProperties[PropertyEnum.SuperCritRatingPowerBonusAdd];
            superCritRatingMult += userProperties[PropertyEnum.SuperCritRatingPowerBonusMult];

            // Apply power keyword bonuses
            if (powerProto != null)
            {
                AccumulateKeywordProperties(ref superCritRatingAdd, powerProto, userProperties, userProperties, PropertyEnum.SuperCritRatingBonusAddPowerKeyword);
                AccumulateKeywordProperties(ref superCritRatingMult, powerProto, userProperties, userProperties, PropertyEnum.SuperCritRatingBonusMultPowerKeyword);
                AccumulateKeywordProperties(ref superCritChancePctAdd, powerProto, userProperties, userProperties, PropertyEnum.SuperCritChancePctAddPowerKwd);
            }

            // Prepare arguments for context data
            float superCritRating = superCritRatingAdd * MathF.Max(superCritRatingMult, 0f);
            int superCritChancePctAddInt = MathHelper.RoundToInt(superCritChancePctAdd * 100f);
            int userLevel = userProperties[PropertyEnum.CombatLevel];
            int targetLevel = targetLevelOverride >= 0 ? targetLevelOverride : target.CombatLevel;

            // Run eval
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, userProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
            evalContext.SetVar_Float(EvalContext.Var1, superCritRating);
            evalContext.SetVar_Int(EvalContext.Var2, superCritChancePctAddInt);
            evalContext.SetVar_Int(EvalContext.Var3, userLevel);
            evalContext.SetVar_Int(EvalContext.Var4, targetLevel);

            return Eval.RunFloat(superCritEval, evalContext);
        }

        public static float GetCritDamageMult(PropertyCollection userProperties, WorldEntity target, bool isSuperCrit)
        {
            CombatGlobalsPrototype combatGlobals = GameDatabase.CombatGlobalsPrototype;
            if (combatGlobals == null) return Logger.WarnReturn(0f, "GetCritDamageMult(): combatGlobals == null");

            EvalPrototype ratingEval = combatGlobals.EvalCritDamageRatingFormula;
            if (ratingEval == null) return Logger.WarnReturn(0f, "GetCritDamageMult(): ratingEval == null");

            // Start calculating crit damage mult
            float critDamageMult = userProperties[PropertyEnum.CritDamageMult];
            critDamageMult += userProperties[PropertyEnum.CritDamagePowerMultBonus];

            if (isSuperCrit)
            {
                critDamageMult += userProperties[PropertyEnum.SuperCritDamageMult];
                critDamageMult += userProperties[PropertyEnum.SuperCritDamagePowerMultBonus];
            }

            // Calculate crit damage rating
            float critDamageRating = userProperties[PropertyEnum.CritDamageRating];

            if (isSuperCrit)
                critDamageRating += userProperties[PropertyEnum.SuperCritDamageRating];

            // Run crit damage rating eval
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, userProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
            evalContext.SetVar_Float(EvalContext.Var1, critDamageRating);

            float critDamageRatingBonus = Eval.RunFloat(ratingEval, evalContext);

            if (target.IsInPvPMatch)
                critDamageRatingBonus *= GameDatabase.DifficultyGlobalsPrototype.PvPCritDamageMultiplier;

            return critDamageMult + critDamageRatingBonus;
        }

        public static float GetBlockChance(PowerPrototype powerProto, PropertyCollection attackerProperties, PropertyCollection targetProperties, ulong attackerId)
        {
            float blockRatingAdd = targetProperties[PropertyEnum.BlockRatingBonusAdd];

            // Add keyword bonuses for power (may be null)
            if (powerProto != null)
                AccumulateKeywordProperties(ref blockRatingAdd, powerProto, targetProperties, attackerProperties, PropertyEnum.BlockRatingBonusVsPowerKwd);
            
            // Add keyword bonuses for attacker (may be null)
            WorldEntity attacker = Game.Current.EntityManager.GetEntity<WorldEntity>(attackerId);
            attacker?.AccumulateKeywordProperties(PropertyEnum.BlockRatingBonusVsAttackerKwd, targetProperties, ref blockRatingAdd);

            // Apply multiplier
            float blockRatingMult = Math.Max(1f + targetProperties[PropertyEnum.BlockRatingBonusMult], 0f);
            float blockRating = Math.Max(blockRatingAdd * blockRatingMult, 0f);

            // Run eval
            EvalPrototype blockEvalProto = GameDatabase.CombatGlobalsPrototype.EvalBlockChanceFormula;
            if (blockEvalProto == null) return Logger.WarnReturn(0f, "GetBlockChance(): blockEvalProto == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, targetProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, attackerProperties);
            evalContext.SetVar_Float(EvalContext.Var1, blockRating);

            if (Eval.Run(blockEvalProto, evalContext, out float blockChance) == false)
                Logger.Warn($"GetBlockChance(): Failed to calculate block chance for power!\n Power: {powerProto}\n Caster: {attacker}");

            return blockChance;
        }

        public static float GetDodgeChance(PowerPrototype powerProto, PropertyCollection attackerProperties, PropertyCollection targetProperties, ulong attackerId)
        {
            // Calculate raw dodge percentage
            float dodgeChancePct = targetProperties[PropertyEnum.DodgeChancePct];

            // Add keyword bonuses for power (may be null)
            if (powerProto != null)
                AccumulateKeywordProperties(ref dodgeChancePct, powerProto, targetProperties, attackerProperties, PropertyEnum.DodgeChancePctVsPowerKeyword);

            // Calculate dodge rating
            float dodgeRatingAdd = targetProperties[PropertyEnum.DodgeRatingBonusAdd];
            float dodgeRatingMult = Math.Max(1f + targetProperties[PropertyEnum.DodgeRatingBonusMult], 0f);
            float dodgeRating = Math.Max(dodgeRatingAdd * dodgeRatingMult, 0f);

            // Run eval
            EvalPrototype dodgeEvalProto = GameDatabase.CombatGlobalsPrototype.EvalDodgeChanceFormula;
            if (dodgeEvalProto == null) return Logger.WarnReturn(0f, "GetDodgeChance(): dodgeEvalProto == null");

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, targetProperties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, attackerProperties);

            // The eval truncates the value, so we need to multiply the raw dodge chance by 100 to save the decimal part
            evalContext.SetVar_Int(EvalContext.Var1, MathHelper.RoundToInt(dodgeChancePct * 100f));
            evalContext.SetVar_Float(EvalContext.Var2, dodgeRating);

            if (Eval.Run(dodgeEvalProto, evalContext, out float dodgeChance) == false)
            {
                WorldEntity attacker = Game.Current.EntityManager.GetEntity<WorldEntity>(attackerId);
                Logger.Warn($"GetDodgeChance(): Failed to calculate dodge chance for power!\n Power: {powerProto}\n Caster: {attacker}");
            }

            return dodgeChance;
        }

        #endregion

        #region Payload
        
        // Payload Serialization is the term the game uses for the snapshotting of properties that happens when a power is applied

        public WorldEntity GetPayloadPropertySourceEntity(WorldEntity ultimateOwner)
        {
            if (IsTeamUpPassivePowerWhileAway)
            {
                Avatar avatarOwner = ultimateOwner != null ? ultimateOwner.GetMostResponsiblePowerUser<Avatar>() : Owner.GetMostResponsiblePowerUser<Avatar>();
                if (avatarOwner != null)
                {
                    Agent teamUpAgent = avatarOwner.CurrentTeamUpAgent;
                    if (teamUpAgent != null)
                        return teamUpAgent;
                }
            }

            return Owner;
        }

        public static void SerializePropertiesForSummonEntity(PropertyCollection sourceProperties, PropertyCollection destinationProperties)
        {
            SerializePropertiesForPowerPayload(sourceProperties, destinationProperties, PowerSerializeType.Entity);
            SerializePropertiesForPowerPayload(sourceProperties, destinationProperties, PowerSerializeType.Power);
        }

        public static void SerializeEntityPropertiesForPowerPayload(WorldEntity worldEntity, PropertyCollection destinationProperties)
        {
            SerializePropertiesForPowerPayload(worldEntity.Properties, destinationProperties, PowerSerializeType.Entity);
        }

        public static void SerializePowerPropertiesForPowerPayload(Power power, PropertyCollection destinationProperties)
        {
            SerializePropertiesForPowerPayload(power.Properties, destinationProperties, PowerSerializeType.Power);
        }

        private static void SerializePropertiesForPowerPayload(PropertyCollection sourceProperties, PropertyCollection destinationProperties, PowerSerializeType serializeType)
        {
            if (serializeType == PowerSerializeType.Entity)
            {
                foreach (var kvp in sourceProperties.IteratePropertyRange(PropertyEnumFilter.SerializeEntityToPowerPayloadFunc))
                    destinationProperties[kvp.Key] = kvp.Value;
            }
            else if (serializeType == PowerSerializeType.Power)
            {
                foreach (var kvp in sourceProperties.IteratePropertyRange(PropertyEnumFilter.SerializePowerToPowerPayloadFunc))
                    destinationProperties[kvp.Key] = kvp.Value;
            }
        }

        #endregion

        protected virtual PowerUseResult ActivateInternal(ref PowerActivationSettings settings)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(PowerUseResult.GenericError, "ActivateInternal(): powerProto == null");

            bool isComboEffect = IsComboEffect();
            bool isProcEffect = IsProcEffect();

            // Set triggering power for combos (this is used to avoid infinite loops via recursion)
            if (isComboEffect)
                Properties[PropertyEnum.TriggeringPowerRef, PrototypeDataRef] = settings.TriggeringPowerRef;

            // All procs are activated by the server and need to be replicated to clients
            if (isProcEffect)
                settings.Flags |= PowerActivationSettingsFlags.ServerCombo;

            // Send non-combo activations and combos / procs triggered by the server
            if (isComboEffect == false || settings.Flags.HasFlag(PowerActivationSettingsFlags.ServerCombo))
            {
                // Send message if there are any interested clients in proximity
                PlayerConnectionManager networkManager = Owner.Game.NetworkManager;

                // Owner is excluded from power activation messages unless explicitly flagged or this is a combo power triggered by the server (therefore the client is not aware of it)
                bool skipOwner = settings.Flags.HasFlag(PowerActivationSettingsFlags.NotifyOwner) == false && settings.Flags.HasFlag(PowerActivationSettingsFlags.ServerCombo) == false;

                List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
                if (networkManager.GetInterestedClients(interestedClientList, Owner, AOINetworkPolicyValues.AOIChannelProximity, skipOwner))
                {
                    NetMessageActivatePower activatePowerMessage = ArchiveMessageBuilder.BuildActivatePowerMessage(this, ref settings);
                    networkManager.SendMessageToMultiple(interestedClientList, activatePowerMessage);
                }

                ListPool<PlayerConnection>.Instance.Return(interestedClientList);
            }

            // ScoringEvent AvatarUsedPower
            if (Owner is Avatar avatar)
            {
                Player player = avatar.GetOwnerOfType<Player>();
                if (player == null) return Logger.WarnReturn(PowerUseResult.GenericError, "ActivateInternal(): player == null");

                WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);
                player.OnScoringEvent(new(ScoringEventType.AvatarUsedPower, powerProto, target?.Prototype));
            }

            // Make a copy of activation settings
            _lastActivationSettings = settings;

            // Trigger events (this relies on the copy of setting we just made)
            HandleTriggerPowerEventOnPowerStart();

            // Get activation time to determine how much to wait before applying the power
            TimeSpan activationTime = GetActivationTime();

            // Copy settings data to a power application instance
            PowerApplication powerApplication = new()
            {
                UserEntityId = Owner.Id,
                UserPosition = settings.UserPosition,
                MovementSpeed = settings.MovementSpeed,
                MovementTime = settings.MovementTime,
                VariableActivationTime = settings.VariableActivationTime,
                PowerRandomSeed = settings.PowerRandomSeed,
                FXRandomSeed = settings.FXRandomSeed,
                ItemSourceId = settings.ItemSourceId,
                SkipRangeCheck = settings.Flags.HasFlag(PowerActivationSettingsFlags.SkipRangeCheck)
            };

            if (GetTargetingShape() == TargetingShapeType.BeamSweep)
                powerApplication.BeamSweepTick = 0;

            if (isProcEffect == false)
            {
                if (Owner == null) return Logger.WarnReturn(PowerUseResult.GenericError, "ActivateInternal(): Owner == null");
                powerApplication.TargetEntityId = settings.TargetEntityId;
                powerApplication.TargetPosition = settings.TargetPosition;
            }
            else
            {
                if (activationTime != TimeSpan.Zero)
                {
                    return Logger.WarnReturn(PowerUseResult.GenericError,
                        $"ActivateInternal(): Power {this} is a proc effect, but it has an application delay of {activationTime.TotalMilliseconds} ms.");
                }

                WorldEntity target = null;
                if (settings.TargetEntityId != Entity.InvalidId)
                    target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);

                if (FillOutProcEffectPowerApplication(target, ref settings, powerApplication) == false)
                    return PowerUseResult.GenericError;
            }

            powerApplication.UnknownTimeSpan = settings.UnknownTimeSpan;

            // Schedule the application or apply it straight away
            if (activationTime > TimeSpan.Zero)
                SchedulePowerApplication(powerApplication, activationTime);
            else
                ApplyPower(powerApplication);

            return PowerUseResult.Success;
        }

        protected virtual bool ApplyInternal(PowerApplication powerApplication)
        {
            // NOTE: This is where powers actually do stuff
            if (Prototype is MovementPowerPrototype movementPowerProto)
            {
                Locomotor locomotor = Owner.Locomotor;
                if (locomotor == null) return Logger.WarnReturn(false, "ApplyInternal(): locomotor == null");

                if (movementPowerProto.TeleportMethod == TeleportMethodType.Teleport)
                {
                    // Instantly teleport to the destination
                    Vector3 teleportPosition = Owner.FloorToCenter(RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, powerApplication.TargetPosition));

                    if (ExecuteTeleport(teleportPosition) == false)
                        return false;
                }
                else if (Owner.IsMovementAuthoritative)
                {
                    // Locomote to the destination
                    // Avatars are not movement authoritative for the server, so this is for NPCs only
                    if (movementPowerProto.IsHighFlyingPower)
                    {
                        locomotor.SetMethod(LocomotorMethod.HighFlying, powerApplication.MovementSpeed);
                    }
                    else
                    {
                        LocomotionOptions locomotionOptions = new()
                        {
                            Flags = LocomotionFlags.IsMovementPower | LocomotionFlags.SkipCurrentSpeedRate,
                            BaseMoveSpeed = powerApplication.MovementSpeed,
                            MoveHeight = movementPowerProto.MovementHeightBonus
                        };

                        if (movementPowerProto.UserNoEntityCollide || movementPowerProto.TeleportMethod == TeleportMethodType.Phase)
                            locomotionOptions.Flags |= LocomotionFlags.LocomotionNoEntityCollide;

                        if (movementPowerProto.TeleportMethod == TeleportMethodType.Phase)
                            locomotionOptions.Flags |= LocomotionFlags.IgnoresWorldCollision;

                        if (movementPowerProto.AllowOrientationChange == false)
                            locomotionOptions.Flags |= LocomotionFlags.DisableOrientation;

                        // NOTE: locomotor.FollowPath is client-only, so we just use 
                        locomotor.MoveTo(powerApplication.TargetPosition, locomotionOptions);
                    }
                }
            }

            // Avatar may exit world as a result of the application of this power
            if (Owner.IsInWorld == false) return true;

            // Create a payload
            PowerPayload payload = new();
            payload.Init(this, powerApplication);     // Payload stores a snapshot of the state of this power and its owner at the moment of application
            payload.CalculateInitialProperties(this);

            // Run pre-apply eval
            if (Prototype.EvalOnPreApply.HasValue())
            {
                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Default, payload.Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Var1, Properties);
                
                Eval.InitTeamUpEvalContext(evalContext, Owner);

                WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(payload.TargetId);
                if (target == null)
                {
                    using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, properties);
                    RunPreApplyEval(evalContext);
                }
                else
                {
                    evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
                    RunPreApplyEval(evalContext);
                }
            }

            // Pay costs
            PayCost(powerApplication);

            // Deliver payload now or schedule it for later
            TimeSpan deliveryDelay = GetPayloadDeliveryDelay(payload);
            if (Prototype.ApplyResultsImmediately && deliveryDelay == TimeSpan.Zero)
                DeliverPayload(payload);
            else
                SchedulePayloadDelivery(payload, deliveryDelay);

            return true;
        }

        protected virtual bool EndPowerInternal(EndPowerFlags flags)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "EndPowerInternal(): powerProto == null");
            powerProto.OnEndPower(this, Owner);
            return true;
        }

        protected virtual bool OnEndPowerCheckTooEarly(EndPowerFlags flags)
        {
            // NOTE: The return value in this method is reversed (i.e. end power proceeds when this returns false)

            // Check flags
            if (flags.HasFlag(EndPowerFlags.ExitWorld) ||
                flags.HasFlag(EndPowerFlags.Unassign) ||
                flags.HasFlag(EndPowerFlags.Interrupting) ||
                flags.HasFlag(EndPowerFlags.WaitForMinTime) ||
                flags.HasFlag(EndPowerFlags.Force))
            {
                return false;
            }

            if (Game == null) return Logger.WarnReturn(true, "OnEndPowerCheckTooEarly(): Game == null");

            TimeSpan timeSinceLastActivation = Game.CurrentTime - LastActivateGameTime;
            TimeSpan channelMinTime = GetChannelMinTime();

            if (channelMinTime > timeSinceLastActivation &&
                (flags.HasFlag(EndPowerFlags.ExplicitCancel) || flags.HasFlag(EndPowerFlags.NotEnoughEndurance) || flags.HasFlag(EndPowerFlags.ClientRequest)))
            {
                _lastActivationSettings.Flags |= PowerActivationSettingsFlags.Cancel;
                SchedulePowerEnd(channelMinTime - timeSinceLastActivation, flags | EndPowerFlags.WaitForMinTime);
                return true;
            }

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(true, "OnEndPowerCheckTooEarly(): powerProto == null");

            TimeSpan activationTime = GetActivationTime();
            float animSpeed = GetAnimSpeed();
            float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;  // Avoid division by 0 / negative

            // It appears the client here is doing imprecise millisecond-based checks, so we have to match it on the server.
            // TODO: See if we things break if we get rid of this and just compare TimeSpans directly.
            TimeSpan halfMillisecond = TimeSpan.FromTicks(5000);
            TimeSpan preWindowTime = activationTime - (powerProto.NoInterruptPreWindowTime * timeMult) - halfMillisecond;
            TimeSpan postWindowTime = activationTime + (powerProto.NoInterruptPostWindowTime * timeMult) + halfMillisecond;

            long timeSinceLastActivationMS = (long)timeSinceLastActivation.TotalMilliseconds;
            long preWindowTimeMS = (long)preWindowTime.TotalMilliseconds;
            long postWindowTimeMS = (long)postWindowTime.TotalMilliseconds;

            // Delay this power's end if it's within the time window of not being interruptable
            if (timeSinceLastActivationMS >= preWindowTimeMS && timeSinceLastActivationMS < postWindowTimeMS)
            {
                _lastActivationSettings.Flags |= PowerActivationSettingsFlags.Cancel;
                TimeSpan endDelay = postWindowTime - timeSinceLastActivation;
                SchedulePowerEnd(endDelay, flags | EndPowerFlags.WaitForMinTime);

                return true;
            }

            return false;
        }

        protected virtual bool OnEndPowerRemoveApplications(EndPowerFlags flags)
        {
            // NOTE: The return value in this method is reversed (i.e. end power proceeds when this returns false)
            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) || flags.HasFlag(EndPowerFlags.Force)
                || IsRecurring() || Properties[PropertyEnum.PowerActiveUntilProjExpire])
            {
                EventScheduler gameEventScheduler = Game?.GameEventScheduler;

                if (gameEventScheduler != null)
                    gameEventScheduler.CancelAllEvents(_pendingPowerApplicationEvents);
                else
                    Logger.Warn("OnEndPowerRemoveApplications(): gameEventScheduler == null");
            }
            else if (_pendingPowerApplicationEvents.IsEmpty == false)
            {
                Logger.Warn($"OnEndPowerRemoveApplications(): _pendingPowerApplicationEvents is not empty! endPowerFlags=[{flags}] power=[{this}]");
            }

            if (IsToggled() && flags.HasFlag(EndPowerFlags.ExplicitCancel) == false && flags.HasFlag(EndPowerFlags.ExitWorld))
            {
                RemoveTrackedConditions(false);
                return true;
            }

            return false;
        }

        protected virtual bool OnEndPowerCancelEvents(EndPowerFlags flags)
        {
            if (IsCharging)
                StopCharging();

            if (Game == null) return Logger.WarnReturn(false, "OnEndPowerCancelEvents(): Game == null");
            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "OnEndPowerCancelEvents(): scheduler == null");

            scheduler.CancelAllEvents(_pendingActivationPhaseEvents);
            CancelAllScheduledActivations();

            scheduler.CancelEvent(_payRecurringCostEvent);
            scheduler.CancelEvent(_reapplyIndexPropertiesEvent);
            scheduler.CancelEvent(_cancelIfTargetIsKilledEvent);
            CancelDelayedActivation();

            return true;
        }

        protected virtual void OnEndPowerCancelConditions()
        {
            if (IsToggled())
                return;

            if (Prototype.CancelConditionsOnEnd || Prototype.Activation == PowerActivationType.Passive)
                RemoveTrackedConditions(true);
        }

        protected virtual void OnEndPowerSendCancel(EndPowerFlags flags)
        {
            // Do not send updates for powers that were not interrupted
            if (flags.HasFlag(EndPowerFlags.ExplicitCancel) == false && flags.HasFlag(EndPowerFlags.ClientRequest) == false)
                return;

            // Do not send updates when cleaning up
            if (flags.HasFlag(EndPowerFlags.ExitWorld) || flags.HasFlag(EndPowerFlags.Unassign))
                return;

            // Send message if there are any interested clients in proximity
            PlayerConnectionManager networkManager = Owner.Game.NetworkManager;

            // The owner's client should have canceled the power it requested on its own
            bool skipOwner = flags.HasFlag(EndPowerFlags.ClientRequest);

            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            if (networkManager.GetInterestedClients(interestedClientList, Owner, AOINetworkPolicyValues.AOIChannelProximity, skipOwner))
            {
                // NOTE: Although NetMessageCancelPower is not an archive, it uses power prototype enums
                ulong powerPrototypeEnum = (ulong)DataDirectory.Instance.GetPrototypeEnumValue<PowerPrototype>(PrototypeDataRef);
                var cancelPowerMessage = NetMessageCancelPower.CreateBuilder()
                    .SetIdAgent(Owner.Id)
                    .SetPowerPrototypeId(powerPrototypeEnum)
                    .SetEndPowerFlags((uint)flags)
                    .Build();

                networkManager.SendMessageToMultiple(interestedClientList, cancelPowerMessage);
            }

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }

        protected virtual bool OnEndPowerCheckLoopEnd(EndPowerFlags flags)
        {
            // Check flags
            if (flags.HasFlag(EndPowerFlags.ExitWorld) ||
                flags.HasFlag(EndPowerFlags.Unassign) ||
                flags.HasFlag(EndPowerFlags.Interrupting) ||
                flags.HasFlag(EndPowerFlags.ChanneledLoopEnd) ||
                flags.HasFlag(EndPowerFlags.Force))
            {
                return false;
            }

            // Check activation phase
            if (_activationPhase == PowerActivationPhase.Inactive)
                return false;

            if (_activationPhase == PowerActivationPhase.ChannelStarting && IsTravelPower())
                return false;

            // Make sure channel end time is > 0
            TimeSpan channelEndTime = GetChannelEndTime();
            if (channelEndTime <= TimeSpan.Zero)
                return false;

            // Schedule end at the end of the loop
            if (Prototype?.MovementPreventChannelEnd == true)
                Owner?.Locomotor?.Stop();

            if (_endPowerEvent.IsValid)
            {
                _endPowerEvent.Get().Flags |= EndPowerFlags.Force;
                return Logger.WarnReturn(true,
                    $"OnEndPowerCheckLoopEnd(): {this} is trying to schedule the loop end of the power but there is already an end scheduled.  Was the power set up properly?");
            }

            SchedulePowerEnd(channelEndTime, EndPowerFlags.ChanneledLoopEnd);
            return true;
        }

        protected virtual void OnEndPowerConditionalRemove(EndPowerFlags flags)
        {
            // Unassign one-off powers (e.g. throwables)
            // Removal needs to happen with a delay because it can mess up the timing of interactables
            if (Prototype.RemovedOnUse && flags.HasFlag(EndPowerFlags.Unassign) == false)
                Owner.ScheduleUnassignPowerEvent(PrototypeDataRef);
        }

        protected virtual void OnEndChannelingPhase()
        {
            // For overrides in MissilePower and SummonPower
        }

        protected virtual void GenerateActualTargetPosition(ulong targetId, Vector3 originalTargetPosition, out Vector3 actualTargetPosition,
            ref PowerActivationSettings settings)
        {
            actualTargetPosition = originalTargetPosition;

            if (Game == null || Owner == null) return;
            var style = TargetingStylePrototype;
            if (style == null) return;

            Vector3 ownerPosition = Owner.RegionLocation.Position;

            if (Prototype is MovementPowerPrototype movementPowerProto)
            {
                var target = Game.EntityManager.GetEntity<WorldEntity>(targetId);
                if (movementPowerProto.CustomBehavior != null)
                {
                    var context = new MovementBehaviorPrototype.Context(this, Owner, target, originalTargetPosition);
                    if (movementPowerProto.CustomBehavior.GenerateTargetPosition(context, ref actualTargetPosition)) return;
                }

                if (movementPowerProto.TeleportMethod == TeleportMethodType.Teleport && Owner.Properties.HasProperty(PropertyEnum.TeleportLockdown))
                {
                    actualTargetPosition = ownerPosition;
                    return;
                }

                Vector3 direction = Vector3.Zero;

                if (movementPowerProto.MoveToOppositeEdgeOfTarget && target != null)
                {
                    if (movementPowerProto.MoveToExactTargetLocation == false) return;

                    Vector3 targetPosition = target.RegionLocation.Position;
                    direction = targetPosition - ownerPosition;

                    if (!Vector3.IsNearZero(direction))
                    {
                        direction = Vector3.Normalize(direction);
                        float radius = target.Bounds.Radius + Owner.Bounds.Radius;
                        actualTargetPosition = targetPosition + (direction * radius);
                        actualTargetPosition += direction * movementPowerProto.AdditionalTargetPosOffset;
                    }
                    else
                        actualTargetPosition = targetPosition;
                }
                else if (movementPowerProto.MoveToExactTargetLocation)
                {
                    if (movementPowerProto.MoveToSecondaryTarget)
                    {
                        if (target != null)
                            direction = originalTargetPosition - target.RegionLocation.Position;
                    }
                    else
                        direction = originalTargetPosition - ownerPosition;

                    if (!Vector3.IsNearZero(direction))
                    {
                        direction = Vector3.Normalize(direction);

                        if (target != null && Owner.IsMovementAuthoritative)
                            actualTargetPosition -= direction * target.Bounds.Radius;

                        Vector3 offset = direction * movementPowerProto.AdditionalTargetPosOffset;
                        Vector3 offsetDirection = actualTargetPosition + offset - ownerPosition;

                        if (!Vector3.IsNearZero(offsetDirection))
                        {
                            offsetDirection = Vector3.Normalize(offsetDirection);
                            if (Vector3.Dot(direction, offsetDirection) >= 0f)
                                actualTargetPosition += offset;
                        }
                    }
                }
                else if (movementPowerProto.MoveToExactTargetLocation == false)
                {
                    if (targetId == Owner.Id)
                        direction = Owner.Forward;
                    else
                        direction = Vector3.SafeNormalize2D(originalTargetPosition - ownerPosition, Owner.Forward);

                    actualTargetPosition = ownerPosition + direction * GetKnockbackDistance(Owner);
                }

                if (movementPowerProto.NoCollideIncludesTarget || targetId == Entity.InvalidId)
                {
                    float distanceSq = Vector3.DistanceSquared2D(ownerPosition, actualTargetPosition);
                    if (distanceSq < MathHelper.Square(movementPowerProto.MoveMinDistance))
                    {
                        Vector3 direction2D = Vector3.Normalize2D(actualTargetPosition - ownerPosition);
                        actualTargetPosition = ownerPosition + direction2D * movementPowerProto.MoveMinDistance;
                    }
                }

                bool isBlocked = false;
                float rangeOverride = 0.0f;
                if (movementPowerProto.TeleportMethod != TeleportMethodType.None && !movementPowerProto.IgnoreTeleportBlockers)
                {
                    var region = Owner.Region;
                    if (region == null) return;

                    Vector3? collisionPosition = Vector3.Zero;
                    Vector3 sweepVelocity = Vector3.Normalize(actualTargetPosition - ownerPosition) * GetRange();
                    var firstHitEntity = region.SweepToFirstHitEntity(Owner.Bounds, sweepVelocity, ref collisionPosition,
                        new MovementPowerEntityCollideFunc(1 << (int)BoundsMovementPowerBlockType.All));
                    if (firstHitEntity != null)
                    {
                        rangeOverride = Vector3.Distance2D(ownerPosition, collisionPosition.Value);
                        if (Vector3.DistanceSquared(collisionPosition.Value, ownerPosition) < Vector3.DistanceSquared(actualTargetPosition, ownerPosition))
                        {
                            isBlocked = true;
                            actualTargetPosition = collisionPosition.Value;
                        }
                    }
                }

                if (style.RandomPositionRadius > 0)
                {
                    GRandom random = new(settings.PowerRandomSeed);
                    actualTargetPosition += Vector3.RandomUnitVector2D(random) * (random.NextFloat() * style.RandomPositionRadius);
                }

                if (movementPowerProto.MoveFullDistance == false || movementPowerProto.TeleportMethod != TeleportMethodType.None)
                {
                    Vector3? resultPostion = actualTargetPosition;
                    var result = PowerPositionSweep(Owner.RegionLocation, actualTargetPosition, targetId, ref resultPostion, isBlocked, rangeOverride);
                    actualTargetPosition = resultPostion.Value;

                    if (result == PowerPositionSweepResult.Error || result == PowerPositionSweepResult.TargetPositionInvalid)
                    {
                        Logger.Warn($"GenerateActualTargetPosition(): Movement power failed to sweep to target position. Using position {actualTargetPosition}, " +
                            $"which may not be valid. Sweep result code: {result}\nPower: {ToString()}\nOwner: {Owner}\nRegionLocation: {Owner.RegionLocation}");

                        actualTargetPosition = ownerPosition;
                    }
                    else
                    {
                        actualTargetPosition = RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, actualTargetPosition);
                        if (movementPowerProto.TeleportMethod != TeleportMethodType.None)
                            actualTargetPosition = Owner.FloorToCenter(actualTargetPosition);
                    }
                }
            }
            else
            {
                if (style.AOESelfCentered)
                {
                    if (style.TargetingShape == TargetingShapeType.CircleArea
                        || (style.TargetingShape == TargetingShapeType.WedgeArea
                        || style.TargetingShape == TargetingShapeType.ArcArea
                        || style.TargetingShape == TargetingShapeType.BeamSweep)
                        && Vector3.LengthSqr(originalTargetPosition - ownerPosition) < 400.0f)
                        actualTargetPosition = ownerPosition;
                }

                if (style.RandomPositionRadius > 0)
                {
                    GRandom random = new(settings.PowerRandomSeed);
                    actualTargetPosition += Vector3.RandomUnitVector2D(random) * (random.NextFloat() * style.RandomPositionRadius);
                    actualTargetPosition = RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, actualTargetPosition);
                }
            }
        }

        protected virtual bool SetToggleState(bool value, bool doNotStartCooldown = false)
        {
            if (IsToggled() == false) return Logger.WarnReturn(false, "SetToggleState(): Trying to toggle a power that isn't togglable!");

            Owner.Properties[PropertyEnum.PowerToggleOn, PrototypeDataRef] = value;

            if (value)
            {
                HandleTriggerPowerEventOnPowerToggleOn();
            }
            else
            {
                HandleTriggerPowerEventOnPowerToggleOff();

                if (doNotStartCooldown == false)
                    StartCooldown();

                if (HasEnduranceCostRecurring())
                    Game.GameEventScheduler?.CancelEvent(_payRecurringCostEvent);

                RemoveTrackedConditions(false);
            }

            return true;
        }

        private bool CancelTogglePowersInSameGroup()
        {
            // NOTE: This is used only for vanity pets in 1.52, but before that it was used for hero powers as well.

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CancelTogglePowersInSameGroup(): powerProto == null");

            if (powerProto.ToggleGroup == PrototypeId.Invalid)
                return true;

            PowerCollection powerCollection = Owner?.PowerCollection;
            if (powerCollection == null)
                return true;

            foreach (var kvp in powerCollection)
            {
                Power otherPower = kvp.Value.Power;
                if (otherPower == null)
                {
                    Logger.Warn($"CancelTogglePowersInSameGroup(): Encountered empty power record. Power prototype: {kvp.Value.PowerPrototypeRef.GetName()}\nOwner: {Owner}");
                    continue;
                }

                if (otherPower.PrototypeDataRef == PrototypeDataRef)
                    continue;

                PowerPrototype otherPowerProto = otherPower.Prototype;
                if (otherPowerProto == null)
                {
                    Logger.Warn($"CancelTogglePowersInSameGroup(): Failed to get PowerPrototype for a power when testing ToggleGroups for Power [{this}].\nOtherPower: {otherPower}\nOwner: {Owner}");
                    continue;
                }

                if (otherPower.IsToggled() == false)
                    continue;

                if (otherPower.IsToggledOn() && otherPowerProto.ToggleGroup == powerProto.ToggleGroup)
                    otherPower.SetToggleState(false);
            }

            return true;
        }

        private bool CreateSituationalComponent()
        {
            if (Game == null) return Logger.WarnReturn(false, "CreateSituationalComponent(): Game == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CreateSituationalComponent(): powerProto == null");

            if (powerProto?.SituationalComponent?.SituationalTrigger == null)
                return true;

            _situationalComponent = new(Game, powerProto.SituationalComponent, this);
            return true;
        }

        #region AOE Calculations

        private static bool IsTargetInArc(WorldEntity target, WorldEntity owner, float radius, Vector3 position, Vector3 targetPosition,
            PowerPrototype powerProto, TargetingStylePrototype styleProto, PropertyCollection properties)
        {
            return IsTargetInWedge(target, owner, radius, position, targetPosition, powerProto, styleProto)
                && IsTargetInRing(target, radius, position, powerProto, properties);
        }

        private static bool IsTargetInBeamSlice(WorldEntity target, WorldEntity owner, float radius, Vector3 position, Vector3 targetPosition,
            int beamSlice, TimeSpan beamTime, PowerPrototype powerProto, TargetingStylePrototype styleProto)
        {
            float aoeAngle = GetAOEAngle(powerProto);
            if (beamSlice >= 0)
                GetBeamSweepSliceCheckData(powerProto, targetPosition, position, beamSlice, aoeAngle, beamTime, ref aoeAngle, ref targetPosition);
            return IsTargetInWedge(target, owner, radius, position, targetPosition, powerProto, styleProto, aoeAngle);
        }

        private static bool IsTargetInCapsule(WorldEntity target, WorldEntity owner, Vector3 position, Vector3 targetPosition,
            PowerPrototype powerProto, TargetingStylePrototype styleProto, PropertyCollection properties)
        {
            float radius = GetTargetingWidth(powerProto, properties);
            float length = GetAOERadius(powerProto, properties);
            Vector3 direction = GetDirectionCheckData(styleProto, owner, position, targetPosition);
            Vector3 endPosition = position + direction * length;
            var capsule = new Capsule(position, endPosition, radius);
            return target.Bounds.Intersects(capsule);
        }

        private static bool IsTargetInCircle(WorldEntity target, float radius, Vector3 position)
        {
            var sphere = new Sphere(position, radius);
            return target.Bounds.Intersects(sphere);
        }

        private static bool IsTargetInRing(WorldEntity target, float radius, Vector3 position, PowerPrototype powerProto, PropertyCollection properties)
        {
            if (IsTargetInCircle(target, radius, position))
            {
                float targetRadius = target.Bounds.Radius;
                float width = GetTargetingWidth(powerProto, properties);
                float ringRadius = radius - width;

                Vector3 targetPosition = target.RegionLocation.Position;
                Vector3 distance = position - targetPosition;
                float targetDistance = Vector3.Length(distance);
                return targetDistance + targetRadius > ringRadius;
            }

            return false;
        }

        private static bool IsTargetInWedge(WorldEntity target, WorldEntity owner, float radius, Vector3 position, Vector3 targetPosition,
            PowerPrototype powerProto, TargetingStylePrototype styleProto, float aoeAngle = 0.0f)
        {
            if (aoeAngle == 0.0f) aoeAngle = GetAOEAngle(powerProto);
            if (aoeAngle <= 0.0f)
                return Logger.WarnReturn(false, $"IsTargetInWedge(): Trying to use a power with an invalid unsupported obtuse wedge angle! Prototype: {powerProto}");

            float targetRadius = target.Bounds.Radius;
            Vector3 targetPos = target.RegionLocation.Position;
            Vector3 direction = GetDirectionCheckData(styleProto, owner, position, targetPosition);
            Vector3 distance = targetPos - position;
            float lengthSq = Vector3.LengthSquared2D(distance);
            float radiusSq = MathHelper.Square(radius + targetRadius);
            if (lengthSq > radiusSq) return false;

            float halfAngle = MathHelper.ToRadians(aoeAngle / 2.0f);
            float angle = Vector3.Angle2D(distance, direction);
            if (angle < halfAngle) return true;

            Vector3 vectorSide = Vector3.SafeNormalize2D(Vector3.Perp2D(distance)) * targetRadius;

            float angleRight = Vector3.Angle2D(vectorSide + distance, direction);
            if (angleRight < halfAngle) return true;

            float angleLeft = Vector3.Angle2D(-vectorSide + distance, direction);
            if (angleLeft < halfAngle) return true;

            return false;
        }

        private static void GetBeamSweepSliceCheckData(PowerPrototype powerProto, Vector3 targetPosition, Vector3 position, int beamSlice,
            float aoeAngle, TimeSpan totalSweepTime, ref float angleResult, ref Vector3 positionResult)
        {
            TimeSpan sweepUpdateRate = TimeSpan.FromMilliseconds((long)powerProto.Properties[PropertyEnum.AOESweepRateMS]);
            if (sweepUpdateRate >= totalSweepTime)
            {
                Logger.Warn($"GetBeamSweepSliceCheckData(): Trying to get targets for a BeamSweep power whose update rate is slower than the total sweep time!\n[{powerProto}]");
                return;
            }

            float angleTime = Math.Min(aoeAngle, aoeAngle * (float)(sweepUpdateRate.TotalSeconds / totalSweepTime.TotalSeconds));
            float totalAngle = angleTime * (beamSlice + 1);
            float angleSliceCenter = -0.5f * aoeAngle;

            if (totalAngle <= aoeAngle)
            {
                angleResult = angleTime;
                angleSliceCenter += (angleTime / 2.0f) * ((2 * beamSlice) + 1);
            }
            else
            {
                float finalAngle = angleTime - (totalAngle - aoeAngle);
                angleResult = finalAngle;
                angleSliceCenter += (finalAngle / 2.0f) + (angleTime / 2.0f) * (2 * beamSlice);
            }

            float sweepDirection = powerProto.Properties[PropertyEnum.AOESweepDirectionCW] ? 1.0f : -1.0f;
            angleSliceCenter *= sweepDirection;

            Matrix3 rotMat = Matrix3.RotationZ(MathHelper.ToRadians(angleSliceCenter));
            Vector3 toTargetPosition = targetPosition - position;

            positionResult = position + rotMat * toTargetPosition;
        }

        private static Vector3 GetDirectionCheckData(TargetingStylePrototype styleProto, WorldEntity owner, Vector3 position, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - position).To2D();

            if (owner != null && owner.IsInWorld && Vector3.LengthSqr(direction) < Segment.Epsilon)
                direction = owner.Forward.To2D();

            if (styleProto.OrientationOffset != 0.0f)
            {
                Transform3 transform = Transform3.BuildTransform(Vector3.Zero, new Orientation(MathHelper.ToRadians(styleProto.OrientationOffset), 0.0f, 0.0f));
                direction = transform * direction;
            }

            return Vector3.Normalize(direction);
        }

        private static bool GetAOETargets(List<WorldEntity> targetList, Game game, PowerPrototype powerProto, float radius, 
            PropertyCollection properties, WorldEntity primaryTarget, WorldEntity owner, in Vector3 targetPosition, in Vector3 userPosition,
            ulong regionId, ulong userEntityId, AlliancePrototype userAllianceProto, int beamSweepSlice, TimeSpan executionTime, int randomSeed)
        {
            if (game == null) return Logger.WarnReturn(false, "GetAOETargets(): game == null");
            
            TargetingReachPrototype reachProto = powerProto.GetTargetingReach();
            if (reachProto == null) return Logger.WarnReturn(false, "GetAOETargets(): reachProto == null");

            TargetingStylePrototype styleProto = powerProto.GetTargetingStyle();
            if (styleProto == null) return Logger.WarnReturn(false, "GetAOETargets(): styleProto == null");

            // Get AOE position and direction
            Vector3 aoePosition;
            if (styleProto.AOESelfCentered && styleProto.RandomPositionRadius == 0)
                aoePosition = userPosition + styleProto.GetOwnerOrientedPositionOffset(owner);
            else
                aoePosition = targetPosition;

            Vector3 aoeDirection = GetDirectionCheckData(styleProto, owner, aoePosition, targetPosition);

            // Get user
            WorldEntity user = (owner?.Id == userEntityId) ? owner : game.EntityManager.GetEntity<WorldEntity>(userEntityId);

            // Check primary target
            if (primaryTarget != null &&
                primaryTarget.IsInWorld &&                    
                reachProto.ExcludesPrimaryTarget == false &&
                ValidateAOETarget(primaryTarget, powerProto, user, userPosition, userAllianceProto, reachProto.RequiresLineOfSight) &&
                IsTargetInAOE(primaryTarget, owner, userPosition, targetPosition, radius, beamSweepSlice, executionTime, powerProto, properties))
            {
                targetList.Add(primaryTarget);
            }

            // Check if need need to find only a single target and we found it
            if (targetList.Count == 1 && powerProto.MaxAOETargets == 1)
                return true;

            // Get region
            RegionManager regionManager = game.RegionManager;
            if (regionManager == null) return Logger.WarnReturn(false, "GetAOETargets(): regionManager == null");
            Region region = regionManager.GetRegion(regionId);
            if (region == null) return Logger.WarnReturn(false, "GetAOETargets(): region == null");

            // Look for potential targets in the AOE shape
            List<WorldEntity> potentialTargetList = ListPool<WorldEntity>.Instance.Get();
            GetPotentialTargetsInShape(region, radius, in aoePosition, in aoeDirection, powerProto, potentialTargetList);

            // Set up random
            if (reachProto.RandomAOETargets && randomSeed == 0)
            {
                ListPool<WorldEntity>.Instance.Return(potentialTargetList);
                return Logger.WarnReturn(false,
                    $"GetAOETargets(): A power has RandomAOETargets set true, but no random seed to do it with!\n Power: {powerProto}\n Owner: {owner}\n");
            }

            GRandom random = new(randomSeed);

            // Validate potential targets
            int index = 0;
            while (GetNextTargetInAOE(potentialTargetList, ref index, reachProto.RandomAOETargets, random, out WorldEntity target) == true)
            {
                if (target == null)
                {
                    Logger.Warn($"GetAOETargets(): Invalid target in region! {region}");
                    continue;
                }

                // Primary target should already be validated above
                if (target == primaryTarget)
                    continue;

                if (ValidateAOETarget(target, powerProto, user, userPosition, userAllianceProto, reachProto.RequiresLineOfSight) == false)
                    continue;

                if (styleProto.TargetingShape == TargetingShapeType.CircleArea ||
                    IsTargetInAOE(target, owner, aoePosition, targetPosition, radius, beamSweepSlice, executionTime, powerProto, properties))
                {
                    targetList.Add(target);

                    // Break out if we don't need any more targets
                    if (powerProto.MaxAOETargets > 0 && targetList.Count >= powerProto.MaxAOETargets)
                        break;
                }
            }

            ListPool<WorldEntity>.Instance.Return(potentialTargetList);
            return true;
        }

        private static void GetPotentialTargetsInShape(Region region, float radius, in Vector3 position, in Vector3 direction,
            PowerPrototype powerProto, List<WorldEntity> potentialTargetList)
        {
            if (GetTargetingShape(powerProto) == TargetingShapeType.WedgeArea)
            {
                Aabb aabb = Aabb.AabbFromWedge(position, direction, GetAOEAngle(powerProto), radius);
                aabb.Max.Z = float.MaxValue;
                aabb.Min.Z = -float.MaxValue;

                region.GetEntitiesInVolume(potentialTargetList, aabb, new(EntityRegionSPContextFlags.ActivePartition));
                return;
            }

            region.GetEntitiesInVolume(potentialTargetList, new Sphere(position, radius), new(EntityRegionSPContextFlags.ActivePartition));
        }

        private static bool GetNextTargetInAOE(List<WorldEntity> potentialTargetList, ref int index, bool pickRandom, GRandom random, out WorldEntity target)
        {
            target = null;

            if (potentialTargetList.Count == 0 || index >= potentialTargetList.Count)
                return false;

            if (pickRandom)
            {
                // Pick a random element and remove it from the list if requested
                int randomIndex = random.Next(0, potentialTargetList.Count - 1);
                target = potentialTargetList[randomIndex];
                potentialTargetList.RemoveAt(randomIndex);
                return true;
            }

            target = potentialTargetList[index];
            index++;
            return true;
        }

        #endregion

        private static bool GetTargetsFromInventory(List<WorldEntity> targetList, Game game, WorldEntity owner, WorldEntity target,
            PowerPrototype powerProto, AlliancePrototype userAllianceProto, InventoryConvenienceLabel inventoryConvenienceLabel)
        {
            if (game == null) return Logger.WarnReturn(false, "GetTargetsFromInventory(): game == null");
            if (owner == null) return Logger.WarnReturn(false, "GetTargetsFromInventory(): owner == null");

            // If this inventory is not available to the owner, we don't need to do anything
            Inventory inventory = owner.GetInventory(inventoryConvenienceLabel);
            if (inventory == null)
                return true;

            // If the power has only a single target and its null, we don't need to do anything
            TargetingShapeType targetingShape = GetTargetingShape(powerProto);
            if (targetingShape == TargetingShapeType.SingleTarget && target == null)
                return true;

            // Now we look for targets in the inventory
            EntityManager entityManager = game.EntityManager;

            foreach (var entry in inventory)
            {
                WorldEntity entity = entityManager.GetEntity<WorldEntity>(entry.Id);
                if (entity == null)
                {
                    Logger.Warn("GetTargetsFromInventory(): entity == null");
                    continue;
                }

                // Skip invalid targets
                if (IsValidTarget(powerProto, owner, userAllianceProto, entity) == false)
                    continue;

                // When this power has only a single target we use this iteration to look just for this target
                if (targetingShape == TargetingShapeType.SingleTarget)
                {
                    // The second condition here is weird, when does this happen?
                    if (entity == target || (target == owner && inventory.Count == 1))
                    {
                        targetList.Add(entity);
                        break;
                    }

                    continue;
                }

                // For other cases we just add all valid entities to the list
                targetList.Add(entity);
            }

            return true;
        }

        private static bool GetValidMeleeTarget(List<WorldEntity> targetList, PowerPrototype powerProto, AlliancePrototype userAllianceProto,
            WorldEntity user, in Vector3 targetPosition)
        {
            if (user == null)
                return false;

            Region region = user.Region;
            if (region == null) return Logger.WarnReturn(false, "GetValidMeleeTarget(): region == null");

            // Set up search volume
            Vector3 userPosition = user.RegionLocation.Position;
            float userRadius = user.Bounds.Radius;
            Vector3 offset = Vector3.SafeNormalize2D(targetPosition - userPosition) * (userRadius + 25f);
            Sphere sphere = new(userPosition + offset, 25f);

            // Look for a target in the volume
            foreach (WorldEntity target in region.IterateEntitiesInVolume(sphere, new(EntityRegionSPContextFlags.ActivePartition)))
            {
                if (IsValidTarget(powerProto, user, userAllianceProto, target))
                {
                    targetList.Add(target);
                    return true;
                }
            }

            return false;
        }

        private void ComputePowerMovementSettings(MovementPowerPrototype movementPowerProto, ref PowerActivationSettings settings)
        {
            bool isMovementAuthoritative = Owner.IsMovementAuthoritative;
            bool isContinuous = settings.Flags.HasFlag(PowerActivationSettingsFlags.Continuous);
            bool isCombo = movementPowerProto?.PowerCategory == PowerCategoryType.ComboEffect;
            bool isHoldAndReleaseMovementPower = movementPowerProto?.ExtraActivation is SecondaryActivateOnReleasePrototype;

            if (isMovementAuthoritative || isContinuous || isCombo || isHoldAndReleaseMovementPower)
            {
                if (movementPowerProto != null && movementPowerProto.TeleportMethod == TeleportMethodType.None)
                    GenerateMovementPathToTarget(movementPowerProto, ref settings);

                ComputeTimeForPowerMovement(movementPowerProto, ref settings);
            }
        }

        private void GenerateMovementPathToTarget(MovementPowerPrototype movementPowerProto, ref PowerActivationSettings settings)
        {
            Vector3? resultPosition = settings.TargetPosition;
            RegionLocation regionLocation = new(Owner.RegionLocation);
            regionLocation.SetPosition(settings.UserPosition);

            PowerPositionSweepResult result = PowerPositionSweepInternal(regionLocation, settings.TargetPosition,
                settings.TargetEntityId, ref resultPosition, false, false);

            if (result == PowerPositionSweepResult.Clipped)
            {
                if (movementPowerProto.MoveFullDistance)
                {
                    Vector3.SafeNormalAndLength2D(resultPosition.Value - settings.UserPosition, out Vector3 resultNormal, out float resultLength);
                    if (resultLength > 0f)
                        resultPosition += resultNormal * Owner.Bounds.Radius * 0.5f;
                }

                // Update target position in the settings
                settings.TargetPosition = RegionLocation.ProjectToFloor(Owner.Region, Owner.Cell, resultPosition.Value);
            }
        }

        private bool ComputeTimeForPowerMovement(MovementPowerPrototype movementPowerProto, ref PowerActivationSettings settings)
        {
            if (Owner == null) return Logger.WarnReturn(false, "ComputeTimeForPowerMovement(): Owner == null");

            // Clear movement time for non-movement powers
            if (movementPowerProto == null)
            {
                settings.MovementSpeed = 0f;
                settings.MovementTime = TimeSpan.Zero;
                return true;
            }

            // Calculate distance
            float distance = 0f;
            /* Paths are generated in client-only code, so the server most likely does not need this
            if (settings.GeneratedPath != null)
            {
                distance = settings.GeneratedPath.Path.AccurateTotalDistance();
                if (movementPowerProto.MoveFullDistance)
                    distance = MathF.Max(distance, GetKnockbackDistance(Owner));
            } */

            if (movementPowerProto.TeleportMethod == TeleportMethodType.Phase)
            {
                float distanceToTarget = Vector3.Distance2D(settings.UserPosition, settings.TargetPosition);
                distance = MathF.Max(movementPowerProto.MoveMinDistance, distanceToTarget);
            }
            else
            {
                if (movementPowerProto.UserNoEntityCollide || movementPowerProto.MoveFullDistance)
                {
                    float minDistance;
                    if (movementPowerProto.NoCollideIncludesTarget == false && settings.TargetEntityId != Entity.InvalidId)
                        minDistance = 0f;
                    else if (movementPowerProto.MoveMinDistance > 0)
                        minDistance = movementPowerProto.MoveMinDistance;
                    else
                        minDistance = GetKnockbackDistance(Owner);

                    float distanceToTarget = Vector3.Distance2D(settings.UserPosition, settings.TargetPosition);

                    distance = MathF.Max(minDistance, distanceToTarget);
                }
                else
                {
                    Region region = Owner.Region;
                    if (region == null) return Logger.WarnReturn(false, "ComputeTimeForPowerMovement(): region == null");

                    Vector3 targetPosition = settings.TargetPosition;   // Remember target position before sweeping
                    Vector3? resultHitPosition = null;

                    WorldEntity firstHitEntity = region.SweepToFirstHitEntity(settings.UserPosition, settings.TargetPosition, Owner,
                        Entity.InvalidId, false, 0f, ref resultHitPosition);

                    distance = Vector3.Distance2D(settings.UserPosition, targetPosition);
                    if (firstHitEntity == null)
                        distance = MathF.Max(movementPowerProto.MoveMinDistance, distance);
                }
            }

            // Calculate movement speed override
            float movementSpeedOverride = 0f;

            Locomotor ownerLoco = Owner.Locomotor;
            if (ownerLoco == null) return Logger.WarnReturn(false, "ComputeTimeForPowerMovement(): ownerLoco == null");

            float defaultRunSpeed = ownerLoco.DefaultRunSpeed;

            if (movementPowerProto.EvalUserMoveSpeed != null)
            {
                WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(settings.TargetEntityId);

                using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                evalContext.Game = Game;
                evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Entity, Owner.Properties);
                evalContext.SetVar_PropertyCollectionPtr(EvalContext.Other, target?.Properties);
                evalContext.SetVar_Float(EvalContext.Var1, defaultRunSpeed);

                movementSpeedOverride = Eval.RunFloat(movementPowerProto.EvalUserMoveSpeed, evalContext);
            }

            if (Segment.IsNearZero(movementSpeedOverride))
                movementSpeedOverride = defaultRunSpeed;

            if (movementSpeedOverride <= 0f)
                return Logger.WarnReturn(false, $"ComputeTimeForPowerMovement(): Movement power has no movement speed set - {movementPowerProto}");

            // Set movement time fields in settings
            if (movementPowerProto.IsHighFlyingPower)
            {
                settings.MovementSpeed = movementSpeedOverride;
                settings.MovementTime = TimeSpan.Zero;
            }
            else if (movementPowerProto.ConstantMoveTime)
            {
                // Movement time here needs to be set first because it is used to calculate movement speed
                settings.MovementTime = GetFullExecutionTime() - GetActivationTime();

                float movementTimeSeconds = (float)settings.MovementTime.TotalSeconds;
                settings.MovementSpeed = movementTimeSeconds > 0f ? distance / movementTimeSeconds : 0f;     // Avoid division by 0 / negative
            }
            else if (movementPowerProto.ChanneledMoveTime)
            {
                settings.MovementSpeed = movementSpeedOverride;
                settings.MovementTime = GetFullExecutionTime() - GetActivationTime();
            }
            else
            {
                settings.MovementSpeed = movementSpeedOverride;
                settings.MovementTime = TimeSpan.FromSeconds(distance / movementSpeedOverride);
            }

            return true;
        }

        private bool ExecuteTeleport(Vector3 teleportPosition)
        {
            Region region = Owner.Region;
            if (region == null) return Logger.WarnReturn(false, "ExecuteTeleport(): region == null");

            Vector3 currentFloorPosition = RegionLocation.ProjectToFloor(region, Owner.Cell, Owner.RegionLocation.Position);
            Vector3 targetFloorPosition = RegionLocation.ProjectToFloor(region, Owner.Cell, teleportPosition);

            if (Owner.CanPowerTeleportToPosition(targetFloorPosition) == false)
                return Logger.WarnReturn(false, $"ExecuteTeleport(): Cannot teleport to the requested target position. REGION={region} POSITION={targetFloorPosition} ENTITY={Owner} POWER={this}");
            
            Vector3 floorOffset = targetFloorPosition - currentFloorPosition;
            float floorOffsetLength = Vector3.LengthTest(floorOffset);

            if (Segment.IsNearZero(floorOffsetLength) == false)
            {
                Bounds teleportPositionBounds = new(Owner.Bounds);
                teleportPositionBounds.Center = teleportPosition;

                Aabb aabb = Owner.RegionBounds;
                aabb = aabb.Translate(floorOffset);

                float minIntersection = float.MaxValue;

                foreach (WorldEntity worldEntity in region.IterateEntitiesInRegion(new()))
                {
                    if (worldEntity.Properties.HasProperty(PropertyEnum.BlocksTeleports) == false)
                        continue;

                    Bounds blockingEntityBounds = worldEntity.Bounds;

                    if (blockingEntityBounds.Intersects(teleportPositionBounds) == false)
                        continue;

                    float intersection = 0f;

                    if (blockingEntityBounds.Intersects(new Segment(currentFloorPosition, targetFloorPosition), ref intersection))
                    {
                        if (intersection < minIntersection)
                            minIntersection = intersection;
                    }
                }

                if (minIntersection <= 1f)
                {
                    minIntersection -= Owner.Bounds.Radius / floorOffsetLength;
                    teleportPosition = Owner.FloorToCenter(currentFloorPosition + (floorOffset * minIntersection));
                }
            }            

            Owner.ChangeRegionPosition(teleportPosition, null,
                ChangePositionFlags.DoNotSendToServer | ChangePositionFlags.DoNotSendToClients | ChangePositionFlags.Force);

            return true;
        }

        private static bool CheckBeamSweepTick(PowerPayload payload)
        {
            // -1 indicates that this is not a beam sweep payload
            if (payload.AOESweepTick < 0)
                return false;

            // Make sure we have an owner in a valid state
            WorldEntity powerOwner = payload.Game.EntityManager.GetEntity<WorldEntity>(payload.PowerOwnerId);
            if (powerOwner == null || powerOwner.TestStatus(EntityStatus.Destroyed) || powerOwner.IsInWorld == false)
                return false;

            // Make sure the owner still has the power
            Power power = powerOwner.GetPower(payload.PowerProtoRef);
            if (power == null) return Logger.WarnReturn(false, "TryScheduleNextBeamSweepTick(): power == null");

            int nextTick = payload.AOESweepTick + 1;
            TimeSpan sweepRate = payload.AOESweepRate;

            // Check if finished sweeping
            if (nextTick * sweepRate >= payload.ExecutionTime)
                return false;

            // Schedule the next tick of the sweep
            PowerApplication powerApplication = new()
            {
                UserEntityId = payload.PowerOwnerId,
                UserPosition = payload.PowerOwnerPosition,
                TargetEntityId = payload.TargetId,
                TargetPosition = payload.TargetPosition,
                MovementTime = payload.MovementTime,
                BeamSweepTick = nextTick
            };

            power.SchedulePowerApplication(powerApplication, sweepRate);
            return true;
        }

        #region Bouncing

        private static bool TryBouncePayload(PowerPayload payload)
        {
            if (payload == null) return Logger.WarnReturn(false, "TryBouncePayload(): payload == null");

            // Check if there is any bouncing to do for this power
            if (payload.Properties.HasProperty(PropertyEnum.BounceCountPayload) == false)
                return false;

            Game game = payload.Game;

            // Start building the bounce message
            ulong lastTargetId = payload.TargetId;
            float speed = payload.Properties[PropertyEnum.BounceSpeedPayload];

            NetMessagePowerBounce.Builder bounceMessageBuilder = NetMessagePowerBounce.CreateBuilder()
                .SetIdPowerUser(payload.PowerOwnerId)
                .SetIdLastTarget(lastTargetId)
                .SetPowerPrototypeId((ulong)payload.PowerProtoRef)
                .SetUserOriginalAssetId((ulong)payload.Properties[PropertyEnum.CreatorEntityAssetRefBase])
                .SetUserCurrentAssetId((ulong)payload.Properties[PropertyEnum.CreatorEntityAssetRefCurrent])
                .SetProjectileSpeed(speed)
                .SetFxRandomSeed((int)payload.FXRandomSeed);

            // Do bouncing if we still have any to do
            int bounceCount = payload.Properties[PropertyEnum.BounceCountPayload];
            if (bounceCount > 0)
            {
                Region region = game.RegionManager.GetRegion(payload.RegionId);
                if (region == null) return Logger.WarnReturn(false, "TryBouncePayload(): region == null");

                PowerPrototype powerProto = payload.PowerPrototype;
                TargetingReachPrototype reachProto = powerProto.GetTargetingReach();
                Vector3 lastTargetPosition = payload.TargetPosition;
                float range = payload.Properties[PropertyEnum.BounceRangePayload];

                WorldEntity unhitTarget = null;
                WorldEntity repeatTarget = null;
                WorldEntity destructibleTarget = null;

                float shortestDistanceUnhit = float.MaxValue;
                float shortestDistanceDestructible = float.MaxValue;

                int lowestTargetIndex = int.MaxValue;

                Sphere bounds = new(lastTargetPosition, range);
                foreach (WorldEntity potentialTarget in region.IterateEntitiesInVolume(bounds, new(EntityRegionSPContextFlags.ActivePartition)))
                {
                    if (potentialTarget.Id == lastTargetId)
                        continue;

                    if (potentialTarget.Properties[PropertyEnum.InvalidBounceTarget])
                        continue;

                    if (IsValidTarget(powerProto, payload.PowerOwnerId, payload.OwnerAlliance, potentialTarget) == false)
                        continue;

                    if (reachProto.RequiresLineOfSight && potentialTarget.LineOfSightTo(lastTargetPosition) == false)
                        continue;

                    int targetIndex = GetBounceTargetIndex(payload.Properties, potentialTarget.Id);
                    if (targetIndex == -1)
                    {
                        if (potentialTarget.IsDestructible)
                            CheckBounceTarget(potentialTarget, lastTargetPosition, ref destructibleTarget, ref shortestDistanceDestructible);
                        else
                            CheckBounceTarget(potentialTarget, lastTargetPosition, ref unhitTarget, ref shortestDistanceUnhit);
                    }
                    else if (payload.Properties[PropertyEnum.BounceCanRepeatTarget])
                    {
                        if (targetIndex < lowestTargetIndex)
                        {
                            lowestTargetIndex = targetIndex;
                            repeatTarget = potentialTarget;
                        }
                    }
                }

                // Prioritize targets that haven't been hit
                WorldEntity newTarget = unhitTarget != null ? unhitTarget : repeatTarget;
                
                // Prioritize non-destructible targets over destructibles
                newTarget ??= destructibleTarget;

                if (newTarget != null)
                {
                    ulong newTargetId = newTarget.Id;

                    payload.UpdateTarget(newTargetId, newTarget.RegionLocation.Position);
                    RecordBounceTarget(payload.Properties, newTargetId);

                    TimeSpan delay = TimeSpan.Zero;
                    if (speed > 0f)
                    {
                        float distance = Vector3.Length(lastTargetPosition - payload.TargetPosition);
                        delay = TimeSpan.FromSeconds(distance / speed);
                    }

                    payload.Properties[PropertyEnum.BounceCountPayload] = (bounceCount > 1) ? --bounceCount : -1;

                    SchedulePayloadDelivery(payload, delay);

                    bounceMessageBuilder.SetLastTargetPosition(lastTargetPosition.ToNetStructPoint3());
                    bounceMessageBuilder.SetIdNewTarget(newTargetId);

                    game.NetworkManager.SendMessageToInterested(bounceMessageBuilder.Build(), newTarget, AOINetworkPolicyValues.AOIChannelProximity);

                    // More bouncing to do or finalize, so return true
                    return true;
                }
            }

            // Finish bouncing
            WorldEntity ultimateOwner = game.EntityManager.GetEntity<WorldEntity>(payload.UltimateOwnerId);
            if (ultimateOwner != null)
            {
                bounceMessageBuilder.SetIdPowerUser(ultimateOwner.Id);
                bounceMessageBuilder.SetLastTargetPosition(payload.TargetPosition.ToNetStructPoint3());
                bounceMessageBuilder.SetIdNewTarget(Entity.InvalidId);

                game.NetworkManager.SendMessageToInterested(bounceMessageBuilder.Build(), ultimateOwner, AOINetworkPolicyValues.AOIChannelProximity);

                // Return bouncing weapon to the owner (shield, etc.)
                if (payload.PowerPrototype.Properties[PropertyEnum.PowerUsesReturningWeapon])
                {
                    TimeSpan delay = TimeSpan.Zero;
                    if (ultimateOwner.IsInWorld)
                    {
                        float distance = Vector3.Length(ultimateOwner.RegionLocation.Position - payload.TargetPosition);
                        delay = TimeSpan.FromSeconds(distance / speed);
                    }

                    ultimateOwner.ScheduleWeaponReturnEvent(delay);
                }
            }

            // Bouncing over, proceed to ending the power
            return false;
        }

        private static void CheckBounceTarget(WorldEntity targetToCheck, Vector3 lastPosition, ref WorldEntity closestTarget, ref float shortestDistance)
        {
            float distance = Vector3.LengthSquared(lastPosition - targetToCheck.RegionLocation.Position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestTarget = targetToCheck;
            }
        }

        private static int GetBounceTargetIndex(PropertyCollection properties, ulong targetId)
        {
            int targetIndex = -1;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.PowerPreviousTargetsID))
            {
                if (kvp.Value != targetId)
                    continue;

                Property.FromParam(kvp.Key, 0, out targetIndex);
                break;
            }

            return targetIndex;
        }

        private static void RecordBounceTarget(PropertyCollection properties, ulong targetId)
        {
            int maxIndex = -1;
            int oldIndex = -1;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.PowerPreviousTargetsID))
            {
                Property.FromParam(kvp.Key, 0, out int targetIndex);
                maxIndex = Math.Max(targetIndex, maxIndex);

                if (kvp.Value == targetId)
                    oldIndex = targetIndex;
            }

            // Remove the old recorded index
            if (oldIndex > -1)
                properties.RemoveProperty(new(PropertyEnum.PowerPreviousTargetsID, (PropertyParam)oldIndex));

            // Record the target
            properties[PropertyEnum.PowerPreviousTargetsID, ++maxIndex] = targetId;
        }

        /// <summary>
        /// Ends non-missile powers that have the PowerActiveUntilProjExpire property (e.g. chain powers where the user is the one bouncing around).
        /// </summary>
        private static bool TryEndNonMissilePowerActiveUntilProjExpire(PowerPayload payload)
        {
            PowerPrototype powerProto = payload.PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "TryEndNonMissilePowerActiveUntilProjExpire(): powerProto == null");
            if (powerProto.Properties == null) return Logger.WarnReturn(false, "TryEndNonMissilePowerActiveUntilProjExpire(): powerProto.Properties == null");

            // Missile powers handle PowerActiveUntilProjExpire separately
            if (powerProto is MissilePowerPrototype || powerProto.Properties[PropertyEnum.PowerActiveUntilProjExpire] == false)
                return true;

            // Check if the owner still exists and is in the world
            WorldEntity powerOwner = payload.Game.EntityManager.GetEntity<WorldEntity>(payload.PowerOwnerId);
            if (powerOwner == null || powerOwner.IsInWorld == false)
                return true;

            // Check if the owner still has the power
            Power power = powerOwner.GetPower(powerProto.DataRef);
            if (power == null)
                return true;

            // Wait for the projectile to return if we need to
            if (powerProto.ProjectileReturnsToUser)
            {
                Vector3 ownerPosition = powerOwner.RegionLocation.Position;
                Vector3 targetPosition = payload.TargetPosition;

                float distance = Vector3.Distance(ownerPosition, targetPosition);
                float speed = power.GetProjectileSpeed(ownerPosition, targetPosition);
                TimeSpan delay = TimeSpan.FromSeconds(distance / speed);

                power.SchedulePowerEnd(delay);
                return true;
            }

            // End straight away if we don't need to wait for the projectile to return
            power.EndPower(EndPowerFlags.None);
            return true;
        }

        #endregion

        private void DoRandomTargetSelection(Power triggeredPower, ref PowerActivationSettings settings)
        {
            // Not all powers need random targets
            if (triggeredPower.GetTargetingShape() != TargetingShapeType.SingleTargetRandom)
                return;

            // Find a valid random target
            WorldEntity target = triggeredPower.GetRandomTarget();
            if (target == null)
                return;

            // If found, update settings
            settings.TargetEntityId = target.Id;
            settings.TargetPosition = target.RegionLocation.Position;
            settings.Flags |= PowerActivationSettingsFlags.NotifyOwner | PowerActivationSettingsFlags.ServerCombo;
        }

        private bool FillOutProcEffectPowerApplication(WorldEntity target, ref PowerActivationSettings settings, PowerApplication powerApplication)
        {
            // NOTE: We don't need anything other than properties from these results,
            // so we can potentially get rid of them if we implement results pooling.
            powerApplication.PowerResults = settings.PowerResults;

            if (Prototype.GetTargetingStyle().TargetingShape == TargetingShapeType.Self)
            {
                powerApplication.TargetEntityId = Owner.Id;
                powerApplication.TargetPosition = Owner.RegionLocation.Position;
            }
            else
            {
                powerApplication.TargetEntityId = target != null ? target.Id : Entity.InvalidId;
                powerApplication.TargetPosition = settings.TargetPosition;
            }

            PrototypeId triggeringPowerRef = settings.TriggeringPowerRef;
            if (triggeringPowerRef != PrototypeId.Invalid)
                Properties[PropertyEnum.TriggeringPowerRef, PrototypeDataRef] = triggeringPowerRef;

            return true;
        }

        private bool RunActivateEval(EvalContextData evalContext)
        {
            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "RunActivateEval(): powerProto == null");

            bool success = true;

            foreach (EvalPrototype evalProto in powerProto.EvalOnActivate)
            {
                bool evalSuccess = Eval.RunBool(evalProto, evalContext);
                success &= evalSuccess;
                if (evalSuccess == false)
                    Logger.Warn($"RunActivateEval(): The following EvalOnActivate Eval in a power failed:\nEval: [{evalProto.ExpressionString()}]\nPower: [{powerProto}]");
            }

            return success;
        }

        private bool RunPreApplyEval(EvalContextData evalContext)
        {
            // TODO: Merge this with RunActivateEval?

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "RunPreApplyEval(): powerProto == null");

            bool success = true;

            foreach (EvalPrototype evalProto in powerProto.EvalOnPreApply)
            {
                bool evalSuccess = Eval.RunBool(evalProto, evalContext);
                success &= evalSuccess;
                if (evalSuccess == false)
                    Logger.Warn($"RunPreApplyEval(): The following EvalOnPreApply Eval in a power failed:\nEval: [{evalProto.ExpressionString()}]\nPower: [{powerProto}]");
            }

            return success;
        }

        #region Scheduled Events

        private bool ScheduleChannelStart()
        {
            if (Owner == null) return Logger.WarnReturn(false, "ScheduleChannelStart(): Owner == null");
            if (Game == null) return Logger.WarnReturn(false, "ScheduleChannelStart(): Game == null");

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "ScheduleChannelStart(): scheduler == null");

            if (_startChannelingEvent.IsValid)
                scheduler.CancelEvent(_startChannelingEvent);

            TimeSpan channelStartTime = GetChannelStartTime();
            _activationPhase = PowerActivationPhase.ChannelStarting;

            if (Prototype?.MovementPreventChannelStart == true)
                Owner.Locomotor?.Stop();

            scheduler.ScheduleEvent(_startChannelingEvent, channelStartTime, _pendingActivationPhaseEvents);
            _startChannelingEvent.Get().Initialize(this);

            return true;
        }

        private bool SchedulePowerApplication(PowerApplication powerApplication, TimeSpan applicationDelay)
        {
            if (powerApplication == null) return Logger.WarnReturn(false, "SchedulePowerApplication(): powerApplication == null");
            if (applicationDelay <= TimeSpan.Zero) return Logger.WarnReturn(false, "SchedulePowerApplication(): applicationDelay <= TimeSpan.Zero");
            if (Owner == null) return Logger.WarnReturn(false, "SchedulePowerApplication(): Owner == null");

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "SchedulePowerApplication(): scheduler == null");

            EventPointer<PowerApplyEvent> powerApplyEvent = new();
            scheduler.ScheduleEvent(powerApplyEvent, applicationDelay, _pendingPowerApplicationEvents);
            powerApplyEvent.Get().Initialize(this, powerApplication);

            return true;
        }

        public void CancelScheduledPowerApplicationsForTarget(ulong targetId)
        {
            PowerApplyEvent.TargetFilter filter = new(targetId);
            Game.GameEventScheduler.CancelEventsFiltered(_pendingPowerApplicationEvents, filter);
        }

        private static bool SchedulePayloadDelivery(PowerPayload payload, TimeSpan deliveryDelay)
        {
            if (payload == null) return Logger.WarnReturn(false, "SchedulePayloadDelivery(): payload == null");

            EventScheduler scheduler = payload.Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "SchedulePayloadDelivery(): scheduler == null");

            EventPointer<DeliverPayloadEvent> deliverPayloadEvent = new();
            scheduler.ScheduleEvent(deliverPayloadEvent, deliveryDelay, payload.PendingEvents);
            deliverPayloadEvent.Get().Initialize(payload);

            return true;
        }

        private bool ScheduleDelayedActivation(TimeSpan delay)
        {
            // Skipping the first stage of delayed activation shouldn't go through scheduling
            if (delay <= TimeSpan.Zero) return Logger.WarnReturn(false, "ScheduleDelayedActivation(): delay <= TimeSpan.Zero");

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "ScheduleDelayedActivation(): scheduler == null");

            if (_delayedActivationEvent.IsValid)
            {
                scheduler.RescheduleEvent(_delayedActivationEvent, delay);
            }
            else
            {
                scheduler.ScheduleEvent(_delayedActivationEvent, delay, _pendingEvents);
                _delayedActivationEvent.Get().Initialize(this);
            }

            return true;
        }

        private bool CancelDelayedActivation()
        {
            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "CancelDelayedActivation(): scheduler == null");

            scheduler.CancelEvent(_delayedActivationEvent);

            _hasDelayedActivationSettings = false;
            _delayedActivationSettings = default;

            return true;
        }

        private bool ScheduleExtraActivationTimeout(ExtraActivateOnSubsequentPrototype extraActivateOnSubsequent)
        {
            //Logger.Debug($"ScheduleExtraActivationTimeout(): [{this}]");

            int timeoutLengthMS = extraActivateOnSubsequent.GetTimeoutLengthMS(Properties[PropertyEnum.PowerRank]);
            
            if (timeoutLengthMS == 0)
                return true;

            if (extraActivateOnSubsequent.ExtraActivateEffect == SubsequentActivateType.DestroySummonedEntity)
            {
                if (IsOnExtraActivation() == false)
                    return Logger.WarnReturn(false, "ScheduleExtraActivationTimeout(): IsOnExtraActivation() == false");

                if (_subsequentActivationTimeoutEvent.IsValid)
                    return Logger.WarnReturn(false, "ScheduleExtraActivationTimeout(): _subsequentActivationTimeoutEvent.IsValid");
            }

            if (_subsequentActivationTimeoutEvent.IsValid == false)
            {
                EventScheduler scheduler = Game?.GameEventScheduler;
                if (scheduler == null) return Logger.WarnReturn(false, "ScheduleExtraActivationTimeout(): scheduler == null");

                scheduler.ScheduleEvent(_subsequentActivationTimeoutEvent, TimeSpan.FromMilliseconds(timeoutLengthMS), _pendingEvents);
                _subsequentActivationTimeoutEvent.Get().Initialize(this);
            }

            return true;
        }

        private bool CancelExtraActivationTimeout()
        {
            EventScheduler scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "CancelExtraActivationTimeout(): scheduler == null");

            scheduler.CancelEvent(_subsequentActivationTimeoutEvent);
            return true;
        }

        protected bool SchedulePowerEnd(ref PowerActivationSettings settings)
        {
            if (Owner == null) return Logger.WarnReturn(false, "SchedulePowerEnd(): Owner == null");

            EndPowerFlags flags = EndPowerFlags.None;

            if (Properties[PropertyEnum.PowerActiveUntilProjExpire])
            {
                if (Prototype is MissilePowerPrototype)
                    return true;

                float speed = GetProjectileSpeed(GetRange());
                if (speed <= 0f) return Logger.WarnReturn(false, "SchedulePowerEnd(): speed <= 0f");

                float distance = 2 * GetRange() * (1 + Properties[PropertyEnum.BounceCount]);
                TimeSpan delay = TimeSpan.FromSeconds(distance / speed);

                return SchedulePowerEnd(delay);
            }

            TimeSpan executionTime = GetFullExecutionTime() - GetChannelEndTime();

            if (Prototype is MovementPowerPrototype movementPowerProto)
            {
                if (movementPowerProto.ConstantMoveTime == false && movementPowerProto.ChanneledMoveTime == false)
                    executionTime += settings.MovementTime;
            }

            if (settings.Flags.HasFlag(PowerActivationSettingsFlags.Cancel) && CanBeUserCanceledNow())
            {
                TimeSpan activationTime = GetActivationTime();

                float animSpeed = GetAnimSpeed();
                float timeMult = animSpeed > 0f ? 1f / animSpeed : 0f;

                TimeSpan adjustedTime = activationTime + (Prototype.NoInterruptPostWindowTime * timeMult);
                if (adjustedTime < executionTime)
                {
                    flags |= EndPowerFlags.ExplicitCancel;
                    executionTime = adjustedTime;
                }
            }

            return SchedulePowerEnd(executionTime, flags);
        }

        protected bool SchedulePowerEnd(TimeSpan delay, EndPowerFlags flags = EndPowerFlags.None, bool doNotReschedule = false)
        {
            PowerPrototype powerProto = Prototype;

            if (powerProto.ActiveUntilCancelled == false || flags.HasFlag(EndPowerFlags.ChanneledLoopEnd) || flags.HasFlag(EndPowerFlags.WaitForMinTime))
            {
                EventScheduler scheduler = Game.GameEventScheduler;

                if (_endPowerEvent.IsValid)
                {
                    if (doNotReschedule == false)
                    {
                        scheduler.RescheduleEvent(_endPowerEvent, delay);
                        _endPowerEvent.Get().Initialize(this, flags);
                    }

                    return true;
                }

                scheduler.ScheduleEvent(_endPowerEvent, delay > TimeSpan.Zero ? delay : TimeSpan.FromMilliseconds(1), _pendingActivationPhaseEvents);
                _endPowerEvent.Get().Initialize(this, flags);
            }

            return true;
        }

        private bool ScheduleScheduledActivation(TimeSpan delay, Power triggeredPower, PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
        {
            if (Game == null) return Logger.WarnReturn(false, "ScheduleScheduledActivation(): Game == null");

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "ScheduleScheduledActivation(): scheduler == null");

            if (delay <= TimeSpan.Zero) return Logger.WarnReturn(false, "ScheduleScheduledActivation(): delay <= TimeSpan.Zero");

            EventPointer<ScheduledActivateEvent> scheduledActivateEvent = new();
            scheduler.ScheduleEvent(scheduledActivateEvent, delay, _pendingEvents);
            scheduledActivateEvent.Get().Initialize(this, triggeredPower.PrototypeDataRef, triggeredPowerEvent, ref settings);

            // Initialize the event pointer list if this is the first scheduled activation for this power
            _scheduledActivateEventList ??= new();
            _scheduledActivateEventList.Add(scheduledActivateEvent);
            
            return true;
        }

        private bool CancelScheduledActivation(PrototypeId scheduledPowerProtoRef)
        {
            if (Game == null) return Logger.WarnReturn(false, "CancelScheduledActivation(): Game == null");
            if (scheduledPowerProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "CancelScheduledActivation(): scheduledPowerProtoRef == PrototypeId.Invalid");

            if (_scheduledActivateEventList == null)
                return false;

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "CancelScheduledActivation(): scheduler == null");

            // Use a standard for loop to be able to remove the event from the list when we find it
            for (int i = 0; i < _scheduledActivateEventList.Count; i++)
            {
                EventPointer<ScheduledActivateEvent> activateEvent = _scheduledActivateEventList[i];
                if (activateEvent.IsValid && activateEvent.Get().TriggeredPowerProtoRef == scheduledPowerProtoRef)
                {
                    scheduler.CancelEvent(activateEvent);
                    _scheduledActivateEventList.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private bool CancelAllScheduledActivations()
        {
            if (Game == null) return Logger.WarnReturn(false, "CancelAllScheduledActivations(): Game == null");

            // Nothing to cancel if the list hasn't even been created
            if (_scheduledActivateEventList == null)
                return true;

            EventScheduler scheduler = Game.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "CancelAllScheduledActivations(): scheduler == null");

            foreach (EventPointer<ScheduledActivateEvent> activateEvent in _scheduledActivateEventList)
                scheduler.CancelEvent(activateEvent);

            _scheduledActivateEventList.Clear();

            return true;
        }

        private bool ScheduleRecurringCostEvent()
        {
            EventScheduler scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return Logger.WarnReturn(false, "ScheduleRecurringCostEvent(): scheduler == null");

            PowerPrototype powerProto = Prototype;
            if (powerProto == null) return Logger.WarnReturn(false, "ScheduleRecurringCostEvent(): powerProto == null");

            if (_payRecurringCostEvent.IsValid)
            {
                Logger.Warn($"ScheduleRecurringCostEvent(): Overwriting recurring cost event for power [{this}]");
                scheduler.CancelEvent(_payRecurringCostEvent);
            }

            scheduler.ScheduleEvent(_payRecurringCostEvent, powerProto.GetRecurringCostInterval(), _pendingEvents);
            _payRecurringCostEvent.Get().Initialize(this);

            return true;
        }

        private class ScheduledActivateEvent : TargetedScheduledEvent<Power>
        {
            private static readonly Logger Logger = LogManager.CreateLogger();

            private PrototypeId _triggeredPowerProtoRef;
            private PowerEventActionPrototype _triggeredPowerEvent;
            private PowerActivationSettings _settings;
            // TODO: We can avoid doing an extra copy of PowerActivationSettings by using a ref field when we upgrade to C# 11

            public PrototypeId TriggeredPowerProtoRef { get => _triggeredPowerProtoRef; }

            public void Initialize(Power power, PrototypeId triggeredPowerProtoRef, PowerEventActionPrototype triggeredPowerEvent, ref PowerActivationSettings settings)
            {
                _eventTarget = power;
                _triggeredPowerProtoRef = triggeredPowerProtoRef;
                _triggeredPowerEvent = triggeredPowerEvent;
                _settings = settings;
            }

            public override bool OnTriggered()
            {
                if (_eventTarget == null) return Logger.WarnReturn(false, "OnTriggered(): _eventTarget == null");
                if (_eventTarget.Game == null) return Logger.WarnReturn(false, "OnTriggered(): _eventTarget.Game == null");
                _eventTarget.ScheduledActivateCallback(_triggeredPowerProtoRef, _triggeredPowerEvent, ref _settings);
                return true;
            }

            public override bool OnCancelled()
            {
                if (_eventTarget == null) return Logger.WarnReturn(false, "OnCancelled(): _eventTarget == null");
                if (_eventTarget.Game == null) return Logger.WarnReturn(false, "OnCancelled(): _eventTarget.Game == null");
                return true;
            }
        }

        private class DelayedActivationEvent : CallMethodEvent<Power>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.DelayedActivateCallback();
        }

        private class StopChargingEvent : CallMethodEvent<Power>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.StopCharging();
        }

        private class StartChannelingEvent : CallMethodEvent<Power>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.StartChanneling();
        }

        private class StopChannelingEvent : CallMethodEvent<Power>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.StopChanneling();
            public override bool OnCancelled() => OnTriggered();
        }

        private class PowerApplyEvent : ScheduledEvent
        {
            private static readonly Logger Logger = LogManager.CreateLogger();

            private Power _power;
            private PowerApplication _powerApplication;

            public void Initialize(Power power, PowerApplication powerApplication)
            {
                _power = power;
                _powerApplication = powerApplication;
            }

            public override bool OnTriggered()
            {
                if (_power == null) return Logger.WarnReturn(false, "OnTriggered(): _power == null");
                if (_powerApplication == null) return Logger.WarnReturn(false, "OnTriggered(): _powerApplication == null");

                if (_power.Owner.IsSimulated)
                    _power.ApplyPower(_powerApplication);

                return true;
            }

            public override void Clear()
            {
                _power = default;
                _powerApplication = default;
            }

            public readonly struct TargetFilter : IScheduledEventFilter
            {
                private readonly ulong _targetId;

                public TargetFilter(ulong targetId)
                {
                    _targetId = targetId;
                }

                public bool Filter(ScheduledEvent @event)
                {
                    if (@event is not PowerApplyEvent powerApplyEvent)
                        return false;

                    PowerApplication powerApplication = powerApplyEvent._powerApplication;
                    if (powerApplication == null)
                        return false;

                    return powerApplication.TargetEntityId == _targetId;
                }
            }
        }

        private class DeliverPayloadEvent : ScheduledEvent
        {
            private PowerPayload _payload;

            public void Initialize(PowerPayload payload)
            {
                _payload = payload;
            }

            public override bool OnTriggered()
            {
                return DeliverPayload(_payload);
            }

            public override void Clear()
            {
                _payload = default;
            }
        }

        private class EndCooldownEvent : CallMethodEvent<Power>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.OnCooldownEndCallback();
        }

        private class PowerSubsequentActivationTimeoutEvent : CallMethodEvent<Power>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.ExtraActivateTimeoutCallback();
        }

        private class EndPowerEvent : CallMethodEventParam1<Power, EndPowerFlags>
        {
            public EndPowerFlags Flags { get => _param1; set => _param1 = value; }
            protected override CallbackDelegate GetCallback() => (t, p1) => t.EndPower(p1);
        }

        private class ReapplyIndexPropertiesEvent : CallMethodEventParam1<Power, PowerIndexPropertyFlags>
        {
            public PowerIndexPropertyFlags Flags { get => _param1; set => _param1 = value; }
            protected override CallbackDelegate GetCallback() => (t, p1) => t.ReapplyIndexProperties(p1);
        }

        private class PayRecurringCostEvent : CallMethodEvent<Power>
        {
            protected override CallbackDelegate GetCallback() => (t) => t.PayRecurringCost();
        }

        private class CancelIfTargetIsKilledEvent : CallMethodEventParam1<Power, ulong>
        {
            protected override CallbackDelegate GetCallback() => (t, p1) => t.CancelIfTargetIsKilled(p1);
        }

        #endregion
    }
}
