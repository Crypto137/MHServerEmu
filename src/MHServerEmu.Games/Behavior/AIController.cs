﻿using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Behavior
{
    public class AIController
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private EventGroup _pendingEvents = new();
        private EventPointer<AIThinkEvent> _thinkEvent = new();
        private ulong _thinkCount = 0;
        public Agent Owner { get; private set; }
        public Game Game { get; private set; }
        public ProceduralAI.ProceduralAI Brain { get; private set; }
        public BehaviorSensorySystem Senses { get; private set; }
        public BehaviorBlackboard Blackboard { get; private set; }   
        public bool IsEnabled { get; private set; }
        public PrototypeId ActivePowerRef => GetActivePowerRef();
        public WorldEntity TargetEntity => Senses.GetCurrentTarget();
        public WorldEntity InteractEntity => GetInteractEntityHelper();
        public WorldEntity AssistedEntity => GetAssistedEntityHelper();
        public Action<EntityDeadGameEvent> EntityDeadEvent { get; private set; }
        public Action<EntityAggroedGameEvent> EntityAggroedEvent { get; private set; }
        public Action<AIBroadcastBlackboardGameEvent> AIBroadcastBlackboardEvent { get; private set; }
        public Action<PlayerInteractGameEvent> PlayerInteractEvent { get; private set; }
        public Action MissileReturnEvent { get; private set; }

        public AIController(Game game, Agent owner)
        {
            Game = game;
            Owner = owner;
            Senses = new ();
            Blackboard = new (owner);
            Brain = new (game, this);
            EntityDeadEvent = OnAIEntityDeadEvent;
            EntityAggroedEvent = OnAIEntityAggroedGameEvent;
            AIBroadcastBlackboardEvent = OnAIBroadcastBlackboardEvent;
            PlayerInteractEvent = OnAIOnPlayerInteractEvent;
            MissileReturnEvent = OnAIMissileReturnEvent;
        }

        public bool Initialize(BehaviorProfilePrototype profile, SpawnSpec spec, PropertyCollection collection)
        {
            Senses.Initialize(this, profile, spec);
            Blackboard.Initialize(profile, spec, collection);
            Brain.Initialize(profile);
            return true;
        }

        public void OnInitAIOverride(BehaviorProfilePrototype profile, PropertyCollection collection)
        {
            Initialize(profile, null, collection);
            SetIsEnabled(true);
            ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(0));
        }

        public bool IsOwnerValid()
        {
            if (Owner == null 
                || Owner.IsInWorld == false 
                || Owner.IsSimulated == false 
                || Owner.TestStatus(EntityStatus.PendingDestroy) 
                || Owner.TestStatus(EntityStatus.Destroyed))
                return false;
            
            return true;
        }

        public bool GetDesiredIsWalkingState(MovementSpeedOverride speedOverride)
        {
            var locomotor = Owner?.Locomotor;            
            if (locomotor != null && locomotor.SupportsWalking)
            {
                if (speedOverride == MovementSpeedOverride.Walk)               
                    return true;
                 else if (speedOverride == MovementSpeedOverride.Default) 
                    return Senses.GetCurrentTarget() == null;
            }
            else
            {
                if (speedOverride == MovementSpeedOverride.Walk)
                    Logger.Warn("An AI agent's behavior has a movement context with a MovementSpeed of Walk, " +
                        $"but the agent's Locomotor doesn't support Walking.\nAgent [{Owner}]");
            }            
            return false;
        }
        
        private WorldEntity GetInteractEntityHelper()
        {
            ulong interactEntityId = Blackboard.PropertyCollection[PropertyEnum.AIInteractEntityId];
            WorldEntity interactedEntity = Game.EntityManager.GetEntity<WorldEntity>(interactEntityId);
            return interactedEntity;
        }

        private WorldEntity GetAssistedEntityHelper()
        {
            if (Game == null) return null;
            ulong assistedEntityId = Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID];
            if (assistedEntityId == Entity.InvalidId) return null;

            Entity entity = Game.EntityManager.GetEntity<Entity>(assistedEntityId);
            if (entity == null) return null;
            if (entity is Player player) return player.CurrentAvatar;
            if (entity is not WorldEntity assistedEntity)
            {
                Logger.Warn($"Assisted entity [{entity}] is not a WorldEntity.");
                return null;
            }
            return assistedEntity;
        }

        private PrototypeId GetActivePowerRef()
        {
            PrototypeId activePowerRef = PrototypeId.Invalid;
            foreach (var kvp in Blackboard.PropertyCollection.IteratePropertyRange(PropertyEnum.AIPowerStarted))
            {
                Property.FromParam(kvp.Key, 0, out activePowerRef);
                break;
            }
            return activePowerRef;
        }

        public float AggroRangeAlly 
        { 
            get => 
                Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIAggroRangeOverrideAlly) ?
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeOverrideAlly] :
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeAlly];
        }

        public float AggroRangeHostile 
        { 
            get => 
                Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIAggroRangeOverrideHostile) ?
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeOverrideHostile] : 
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeHostile]; 
        }

        public void OnAIActivated()
        {
            OnAIEnabled();
        }

        public void OnAIEnteredWorld()
        {
            OnAIEnabled();
        }

        public void OnAIAllianceChange()
        {
            ResetCurrentTargetState();
        }

        public void ScheduleAIThinkEvent(TimeSpan timeOffset, bool useGlobalThinkVariance = false, bool ignoreActivePower = false)
        {
            if (Game == null || Owner == null) return;
            if (IsEnabled == false
                || Owner.TestStatus(EntityStatus.PendingDestroy) 
                || Owner.TestStatus(EntityStatus.Destroyed) 
                || !Owner.IsInWorld || Owner.IsDead) return;

            if (!ignoreActivePower && Owner.IsExecutingPower)
            {
                Power activePower = Owner.ActivePower;
                if (activePower == null || activePower.IsChannelingPower() == false) return;
            }

            EventScheduler eventScheduler = Game.GameEventScheduler;
            if (eventScheduler == null) return;
            TimeSpan fixedTimeOffset = timeOffset;

            if (useGlobalThinkVariance && Blackboard.PropertyCollection[PropertyEnum.AIUseGlobalThinkVariance])
            {
                AIGlobalsPrototype aiGlobalsPrototype = GameDatabase.AIGlobalsPrototype;
                if (aiGlobalsPrototype == null) return;
                int thinkVariance = aiGlobalsPrototype.RandomThinkVarianceMS;
                fixedTimeOffset += TimeSpan.FromMilliseconds(Game.Random.Next(0, thinkVariance));
            }

            if (_thinkEvent.IsValid && Game.CurrentTime + timeOffset < _thinkEvent.Get().FireTime)
            {
                if (HasNotExceededMaxThinksPerFrame(timeOffset))
                    eventScheduler.RescheduleEvent(_thinkEvent, fixedTimeOffset);
            }
            else if (_thinkEvent.IsValid == false)
            {
                TimeSpan nextThinkTimeOffset = fixedTimeOffset;

                if (HasNotExceededMaxThinksPerFrame(timeOffset) == false)
                    nextThinkTimeOffset = TimeSpan.FromMilliseconds(Math.Max(100, (int)nextThinkTimeOffset.TotalMilliseconds));

                if (nextThinkTimeOffset != TimeSpan.Zero)
                    nextThinkTimeOffset += Clock.Max(Game.RealGameTime - Game.CurrentTime, TimeSpan.Zero);

                if (nextThinkTimeOffset < TimeSpan.Zero)
                {
                    Logger.Warn($"Agent tried to schedule a negative think time of {(long)nextThinkTimeOffset.TotalMilliseconds}MS!\n  Agent: {Owner}");
                    nextThinkTimeOffset = TimeSpan.Zero;
                }

                eventScheduler.ScheduleEvent(_thinkEvent, nextThinkTimeOffset, _pendingEvents);
                _thinkEvent.Get().OwnerController = this;
            }
        }

        public void ClearScheduledThinkEvent()
        {
            if (Game == null) return;            
            EventScheduler eventScheduler = Game.GameEventScheduler;
            if (eventScheduler == null) return;
            eventScheduler.CancelEvent(_thinkEvent);
        }

        public void SetIsEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (enabled)
                OnAIEnabled();
            else
                OnAIDisabled();
        }

        private void OnAIEnabled()
        {
            ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(0), true, false);
        }        

        private void OnAIDisabled()
        {
            ClearScheduledThinkEvent();
        }

        public void OnAIExitedWorld()
        {
            OnAIDisabled();
            Brain?.OnOwnerExitWorld();
        }

        public void ProcessInterrupts(BehaviorInterruptType interrupt)
        {
            Brain?.ProcessInterrupts(interrupt);
        }

        public void OnAIOverlapBegin(WorldEntity other)
        {
            Brain?.OnOwnerOverlapBegin(other);
        }

        private bool HasNotExceededMaxThinksPerFrame(TimeSpan timeOffset)
        {
            if (Game == null || Brain == null) return false;

            if (timeOffset == TimeSpan.Zero && Brain.LastThinkQTime == Game.NumQuantumFixedTimeUpdates && Brain.ThinkCountPerFrame > 4)
            {
                Logger.Warn($"Tried to schedule too many thinks on the same frame. Frame: {Game.NumQuantumFixedTimeUpdates}, " +
                    $"Agent: {Owner}, Think count: {Brain?.ThinkCountPerFrame}");
                return false;
            }

            return true;
        }

        public void AddPowersToPicker(Picker<ProceduralUsePowerContextPrototype> powerPicker, ProceduralUsePowerContextPrototype[] powers)
        {
            if (Owner.Region != null && powers.HasValue())
                foreach (var power in powers)
                    AddPowersToPicker(powerPicker, power);
        }

        public void AddPowersToPicker(Picker<ProceduralUsePowerContextPrototype> powerPicker, ProceduralUsePowerContextPrototype power)
        {
            if (power == null) return;
            Region region = Owner.Region;
            if (region != null && power.AllowedInDifficulty(region.DifficultyTierRef))
                powerPicker.Add(power, power.PickWeight);
        }

        public void ResetCurrentTargetState()
        {
            SetTargetEntity(null);
            Senses.Interrupt = BehaviorInterruptType.NoTarget;
            var collection = Blackboard.PropertyCollection;
            collection.RemoveProperty(PropertyEnum.AINextSensoryUpdate);
            collection.RemoveProperty(PropertyEnum.AINextHostileSense);
            collection.RemoveProperty(PropertyEnum.AINextAllySense);
            collection.RemoveProperty(PropertyEnum.AINextItemSense);
        }

        public void SetTargetEntity(WorldEntity target)
        {
            ulong oldTarget = Blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID];
            ulong newTarget = target?.Id ?? 0;

            if (oldTarget != newTarget)
            {
                Brain?.OnOwnerTargetSwitch(oldTarget, newTarget);
                Blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] = newTarget;
            }

            // TODO update think event
        }

        public void Think()
        {
            if (IsOwnerValid() == false || Owner.IsDead || IsEnabled == false ) return;

            if (Brain.LastThinkQTime == Game.NumQuantumFixedTimeUpdates)
                Brain.ThinkCountPerFrame++;
            else
            {
                Brain.LastThinkQTime = Game.NumQuantumFixedTimeUpdates;
                Brain.ThinkCountPerFrame = 0;
            }
            bool thinking = true;

            if (Owner.TestStatus(EntityStatus.PendingDestroy) == false 
                && Owner.TestStatus(EntityStatus.Destroyed) == false && thinking)
            {
                float thinkTime;
                int aiCustomThinkRateMS = Blackboard.AICustomThinkRateMS;
                if (aiCustomThinkRateMS <= 0)
                {
                    thinkTime = TargetEntity != null || AssistedEntity != null
                        ? 100.0f    // slow think
                        : 500.0f;   // fast think
                }
                else
                {
                    thinkTime = aiCustomThinkRateMS;
                }

                ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(thinkTime) * Game.Random.NextFloat(0.9f, 1.1f));
            }

            Brain?.Think();
            //Logger.Debug($"Think [{Owner.PrototypeName}] {_thinkCount}");
            _thinkCount++;
        }

        public void OnAIKilled()
        {
            OnAIDisabled();
            Brain?.OnOwnerKilled();
            Senses?.NotifyAlliesOnOwnerKilled();
        }

        public void OnAIBehaviorChange()
        {
            if (IsEnabled)
                ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(50), false);
            Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AINextSensoryUpdate);
        }

        public bool AttemptActivatePower(PrototypeId powerRef, ulong targetEntityId, Vector3 targetPosition)
        {
            PowerActivationSettings activateSettings = new(targetEntityId, targetPosition, Owner.RegionLocation.Position);
            activateSettings.Flags |= PowerActivationSettingsFlags.NotifyOwner;     // Force send team-up powers to the owner's client
            return Owner.ActivatePower(powerRef, ref activateSettings) == PowerUseResult.Success;
        }

        public void OnAILeaderDeath()
        {
            ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(50), true);
            Brain?.OnAllyGotKilled();          
        }

        private void OnAIMissileReturnEvent()
        {
            Brain?.OnMissileReturnEvent();
        }

        private void OnAIEntityDeadEvent(EntityDeadGameEvent deadEvent)
        {
            Brain?.OnEntityDeadEvent(deadEvent);
        }

        private void OnAIBroadcastBlackboardEvent(AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            Brain?.OnAIBroadcastBlackboardEvent(broadcastEvent);
        }

        private void OnAIOnPlayerInteractEvent(PlayerInteractGameEvent broadcastEvent)
        {
            Brain?.OnPlayerInteractEvent(broadcastEvent);
        }

        private void OnAIEntityAggroedGameEvent(EntityAggroedGameEvent broadcastEvent)
        {
            Brain?.OnEntityAggroedEvent(broadcastEvent);
        }

        public void RegisterForEntityAggroedEvents(Region region, bool register)
        {
            if (register)
                region.EntityAggroedEvent.AddActionBack(EntityAggroedEvent);
            else
                region.EntityAggroedEvent.RemoveAction(EntityAggroedEvent);
        }

        public void RegisterForEntityDeadEvents(Region region, bool register)
        {
            if (register)
                region.EntityDeadEvent.AddActionBack(EntityDeadEvent);
            else
                region.EntityDeadEvent.RemoveAction(EntityDeadEvent);
        }

        public void RegisterForAIBroadcastBlackboardEvents(Region region, bool register)
        {
            if (register)
                region.AIBroadcastBlackboardEvent.AddActionBack(AIBroadcastBlackboardEvent);
            else
                region.AIBroadcastBlackboardEvent.RemoveAction(AIBroadcastBlackboardEvent);
        }

        public void RegisterForPlayerInteractEvents(Region region, bool register)
        {
            if (register)
                region.PlayerInteractEvent.AddActionBack(PlayerInteractEvent);
            else
                region.PlayerInteractEvent.RemoveAction(PlayerInteractEvent);
        }

        public void OnAIDramaticEntranceEnd()
        {
            if (Owner == null) return;
            ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(0), false, false);
            Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AINextSensoryUpdate);
        }

        public override string ToString()
        {
            return $"AIController: {Owner}";
        }

        public void OnAIDeallocate()
        {
            EventScheduler scheduler = Game.GameEventScheduler;
            scheduler?.CancelAllEvents(_pendingEvents);
        }

        public void OnAISetSimulated(bool simulated)
        {
            SetIsEnabled(simulated);
            Brain?.OnSetSimulated(simulated);
        }

        public void OnAIPowerEnded(PrototypeId powerProtoRef, EndPowerFlags flags)
        {
            ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(50), true);

            if (flags.HasFlag(EndPowerFlags.Unassign))
            {
                if (powerProtoRef == Blackboard.PropertyCollection[PropertyEnum.AIThrowPower] 
                    || powerProtoRef == Blackboard.PropertyCollection[PropertyEnum.AIThrowCancelPower])
                {
                    Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIThrowPower);
                    Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIThrowCancelPower);
                    Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIThrownObjectPickedUp);
                }
            }
        }

        public void OnAIOnGotHit(WorldEntity attacker)
        {            
            if (Owner == null) return;
            if (attacker != null && Owner.IsHostileTo(attacker))
                Blackboard.PropertyCollection[PropertyEnum.AILastAttackerID] = attacker.Id;

            if (Blackboard.PropertyCollection[PropertyEnum.AIRawTargetEntityID] == Entity.InvalidId)
            {
                OnAIEnabled();
                Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AINextSensoryUpdate);
            }
            else
                ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(50), true);
        }

        public void OnAIStartThrowing(WorldEntity throwableEntity, PrototypeId throwablePowerRef, PrototypeId throwableCancelPowerRef)
        {
            if (Owner == null) return;

            // ignore no target override
            Blackboard.PropertyCollection[PropertyEnum.AIIgnoreNoTgtOverrideProfile] = true;

            // set powers
            Blackboard.PropertyCollection[PropertyEnum.AIThrowPower] = throwablePowerRef;
            Blackboard.PropertyCollection[PropertyEnum.AIThrowCancelPower] = throwableCancelPowerRef;

            // schedule event
            EventPointer<ThrownObjectPickedUpEvent> pickUpEvent = new();
            TimeSpan pickupTime = GetThrowableTime(throwableEntity.Properties, PropertyEnum.AIThrowPowerPickupLength);
            TimeSpan loopDelay = GetThrowableTime(throwableEntity.Properties, PropertyEnum.AIThrowPowerLoopDelay);
            ScheduleAIEvent(pickUpEvent, pickupTime, loopDelay);
        }

        private TimeSpan GetThrowableTime(PropertyCollection throwableProp, PropertyEnum throwEnum)
        {
            TimeSpan time = TimeSpan.Zero;
            var propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(throwEnum);
            var throwPropId = new PropertyId(throwEnum, Owner.PrototypeDataRef, propInfo.DefaultParamValues[1]);

            if (throwableProp.HasProperty(throwPropId))
                return throwableProp[throwPropId];
            else
            {
                var keywords = Owner.Keywords;
                if (keywords.HasValue())
                    foreach (var keyword in keywords)
                    {
                        throwPropId = new PropertyId(throwEnum, propInfo.DefaultParamValues[0], keyword);
                        if (throwableProp.HasProperty(throwPropId))
                            return throwableProp[throwPropId];
                    }
            }
            
            return time;
        }

        #region Events

        private void ThrownObjectPickedUp(TimeSpan time)
        {
            Blackboard.PropertyCollection[PropertyEnum.AIThrownObjectPickedUp] = true;
            EventPointer<StartThrowPowerEvent> startThrowEvent = new();
            ScheduleAIEvent(startThrowEvent, time);
        }

        private void StartThrownPower()
        {
            if (Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIThrowPower))
            {
                // throw object to target or forward from owner
                PrototypeId throwPowerRef = Blackboard.PropertyCollection[PropertyEnum.AIThrowPower];
                var targetEntity = TargetEntity;
                if (targetEntity != null)
                {
                    if (AttemptActivatePower(throwPowerRef, targetEntity.Id, targetEntity.RegionLocation.Position) == false)
                        ThrowForwardFromOwner(throwPowerRef);
                }
                else
                    ThrowForwardFromOwner(throwPowerRef);
            }

            // return no target override
            Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIIgnoreNoTgtOverrideProfile);
        }

        private void ThrowForwardFromOwner(PrototypeId throwPowerRef)
        {
            var throwPowerProto = GameDatabase.GetPrototype<PowerPrototype>(throwPowerRef);
            if (throwPowerProto != null)
            {
                using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                float range = throwPowerProto.GetRange(properties, Owner.Properties);
                Vector3 targetPosition = Owner.RegionLocation.Position + (Owner.Forward * range);
                AttemptActivatePower(throwPowerRef, Owner.Id, targetPosition);
            }
        }

        public void ScheduleAIEvent<TEvent>(EventPointer<TEvent> eventPointer, TimeSpan timeOffset)
            where TEvent : CallMethodEvent<AIController>, new()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
            eventPointer.Get().Initialize(this);
        }

        public void ScheduleAIEvent<TEvent, TParam1>(EventPointer<TEvent> eventPointer, TimeSpan timeOffset, TParam1 param1)
            where TEvent : CallMethodEventParam1<AIController, TParam1>, new()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(eventPointer, timeOffset, _pendingEvents);
            eventPointer.Get().Initialize(this, param1);
        }

        public class StartThrowPowerEvent : CallMethodEvent<AIController>
        {
            protected override CallbackDelegate GetCallback() => (controller) => controller.StartThrownPower();
        }

        public class ThrownObjectPickedUpEvent : CallMethodEventParam1<AIController, TimeSpan>
        {
            protected override CallbackDelegate GetCallback() => (controller, time) => controller.ThrownObjectPickedUp(time);
        }

        public class EnableAIEvent : CallMethodEvent<AIController>
        {
            protected override CallbackDelegate GetCallback() => (controller) => controller.SetIsEnabled(true);
        }

        #endregion
    }
}
