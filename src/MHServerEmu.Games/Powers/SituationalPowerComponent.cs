using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public class SituationalPowerComponent
    {
        private readonly Game _game;
        private readonly SituationalPowerComponentPrototype _prototype;
        private readonly HashSet<ulong> _triggeringEntities = new();

        private readonly Event<EntityDeadGameEvent>.Action _deadAction;
        private readonly EventPointer<PowerRelockEvent> _relockEvent = new();
        private readonly EventGroup _eventGroup = new();

        private bool _registered;

        public Power Power { get; private set; }
        public bool TargetsTriggeringEntity { get => _prototype.TargetsTriggeringEntity; }
        public SituationalTrigger SituationalTrigger { get; private set; }

        public SituationalPowerComponent(Game game, SituationalPowerComponentPrototype prototype, Power power)
        {
            _game = game;
            _prototype = prototype;
            Power = power;
            _registered = false;

            _deadAction = OnDead;

            SituationalTrigger = _prototype.SituationalTrigger?.AllocateTrigger(this);
        }

        public void Destroy()
        {
            Shutdown();
        }

        public void Initialize()
        {
            RegisterEvents();
            PowerLock();
        }

        public void Shutdown()
        {
            UnRegisterEvents();

            _game.GameEventScheduler?.CancelAllEvents(_eventGroup);
            PowerUnlock();
        }

        public bool IsTriggeringSituation(WorldEntity entity)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return _triggeringEntities.Contains(entity.Id);
        }

        private void RegisterEvents()
        {
            if (_registered)
                return;

            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            Region region = powerOwner.Region;
            if (region == null)
                return;

            if (SituationalTrigger.RegisterEvents() == false)
                return;

            region.EntityDeadEvent.AddActionBack(_deadAction);

             _registered = true;
        }

        private void UnRegisterEvents()
        {
            if (_registered == false)
                return;

            _registered = false;

            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            Region region = powerOwner.Region;
            if (!Verify.IsNotNull(region)) return;

            SituationalTrigger.UnRegisterEvents();
            region.EntityDeadEvent.RemoveAction(_deadAction);
        }

        public void OnPowerAssigned()
        {
            if (_registered == false)
                return;

            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            Region region = powerOwner.Region;
            if (region == null)
                return;

            SituationalTrigger.OnPowerAssigned();
        }

        public void OnPowerEquipped()
        {
            if (_registered == false)
                return;

            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            Region region = powerOwner.Region;
            if (region == null)
                return;

            SituationalTrigger.OnPowerEquipped();
        }

        public void OnPowerActivated(WorldEntity target)
        {
            if (_prototype.ForceRelockOnActivate)
            {
                PowerRelock();
            }
            else if (_prototype.RemoveTriggeringEntityOnActivate)
            {
                ulong targetId = Entity.InvalidId;
                if (target != null)
                {
                    targetId = target.Id;
                }
                else
                {
                    WorldEntity nearestEntity = GetNearestTriggeringEntity();
                    if (nearestEntity != null)
                        targetId = nearestEntity.Id;
;               }

                OnTriggerRevert(targetId);
            }
        }

        private void OnTriggerRevert(ulong targetId)
        {
            if (!Verify.IsTrue(targetId != Entity.InvalidId)) return;

            RemoveTriggeringEntity(targetId);

            if (_triggeringEntities.Count == 0)
            {
                if (_relockEvent.IsValid == false || _prototype.ForceRelockOnTriggerRevert)
                    PowerRelock();
            }
        }

        public bool OnTrigger(WorldEntity target)
        {
            if (!Verify.IsNotNull(target)) return false;

            if (CanTrigger(target))
            {
                if (_prototype.SituationalTrigger.ActivateOnTriggerSuccess)
                    ActivatePower(target);
                else
                    TriggerSuccess(target.Id);

                return true;
            }
            else
            {
                OnTriggerRevert(target.Id);
            }
            
            return false;
        }

        private bool CanTrigger(WorldEntity target)
        {
            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return false;
            
            // Target
            if (TargetsTriggeringEntity) 
            {
                if (Power.IsValidTarget(target) == false)
                    return false;

                if (Power.IsInRange(target, RangeCheckType.Activation) == false)
                    return false;
            }

            // ChanceToTrigger eval
            if (!Verify.IsNotNull(_prototype.ChanceToTrigger)) return false;

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, Power.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, powerOwner.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);

            float chance = Eval.RunFloat(_prototype.ChanceToTrigger, evalContext);
            if (_game.Random.NextFloat() > chance)
                return false;
            
            // SituationalTrigger
            if (SituationalTrigger.CanTrigger(powerOwner, target) == false)
                return false;

            return true;
        }

        private void ActivatePower(WorldEntity target)
        {
            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            PrototypeId powerRef = Power.PrototypeDataRef;
            if (!Verify.IsTrue(powerRef != PrototypeId.Invalid)) return;

            ulong targetId = powerOwner.Id;
            Vector3 ownerPosition = powerOwner.RegionLocation.Position;
            Vector3 targetPositon = ownerPosition;

            if (TargetsTriggeringEntity)
            {
                targetId = target.Id;
                targetPositon = target.RegionLocation.Position;
            }

            PowerActivationSettings settings = new(targetId, targetPositon, ownerPosition);
            powerOwner.ActivatePower(powerRef, ref settings);
        }

        private void TriggerSuccess(ulong targetId)
        {
            PowerUnlock();
            SchedulePowerRelock();
            AddTriggeringEntity(targetId);
        }

        private void AddTriggeringEntity(ulong triggeringEntityId)
        {
            if (!Verify.IsTrue(triggeringEntityId != Entity.InvalidId)) return;

            _triggeringEntities.Add(triggeringEntityId);
            SendUpdateSituationalTarget(triggeringEntityId, true);
        }

        private void RemoveTriggeringEntity(ulong triggeringEntityId)
        {
            if (!Verify.IsTrue(triggeringEntityId != Entity.InvalidId)) return;

            if (_triggeringEntities.Remove(triggeringEntityId))
                SendUpdateSituationalTarget(triggeringEntityId, false);
        }

        private void SendUpdateSituationalTarget(ulong targetId, bool addTarget)
        {
            if (!Verify.IsTrue(targetId != Entity.InvalidId)) return;

            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            var message = NetMessageUpdateSituationalTarget.CreateBuilder()
                .SetPowerOwnerId(powerOwner.Id)
                .SetSituationalPowerProtoId((ulong)Power.PrototypeDataRef)
                .SetSituationalTargetId(targetId)
                .SetAddTarget(addTarget)
                .Build();

            _game.NetworkManager.SendMessageToInterested(message, powerOwner, AOINetworkPolicyValues.AOIChannelOwner);
        }

        private WorldEntity GetNearestTriggeringEntity()
        {
            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return null;

            WorldEntity nearestEntity = null;
            float minDistance = float.MaxValue;

            EntityManager entityManager = _game.EntityManager;
            foreach (ulong targetId in _triggeringEntities)
            {
                WorldEntity target = entityManager.GetEntity<WorldEntity>(targetId);
                if (target == null)
                    continue;

                float distance = powerOwner.GetDistanceTo(target, false);
                if (powerOwner.GetDistanceTo(target, false) < minDistance)
                {
                    nearestEntity = target;
                    minDistance = distance;
                }
            }

            return nearestEntity;
        }

        private void PowerRelock()
        {
            PowerLock();

            if (_relockEvent.IsValid)
                _game.GameEventScheduler?.CancelEvent(_relockEvent);

            _triggeringEntities.Clear();
        }

        private void PowerLock()
        {
            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            PrototypeId powerRef = Power.PrototypeDataRef;
            if (!Verify.IsTrue(powerRef != PrototypeId.Invalid)) return;

            powerOwner.Properties[PropertyEnum.SinglePowerLock, powerRef] = true;
        }

        private void PowerUnlock()
        {
            WorldEntity powerOwner = Power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            PrototypeId powerRef = Power.PrototypeDataRef;
            if (!Verify.IsTrue(powerRef != PrototypeId.Invalid)) return;

            powerOwner.Properties.RemoveProperty(new(PropertyEnum.SinglePowerLock, Power.PrototypeDataRef));
        }

        private void SchedulePowerRelock()
        {
            TimeSpan activationWindow = TimeSpan.FromMilliseconds(_prototype.ActivationWindowMS);
            if (activationWindow <= TimeSpan.Zero)
                return;

            EventScheduler scheduler = _game.GameEventScheduler;
            if (!Verify.IsNotNull(scheduler)) return;

            if (_relockEvent.IsValid)
            {
                scheduler.RescheduleEvent(_relockEvent, activationWindow);
            }
            else
            {
                scheduler.ScheduleEvent(_relockEvent, activationWindow, _eventGroup);
                _relockEvent.Get().Initialize(this);
            }
        }

        private void OnDead(in EntityDeadGameEvent evt)
        {
            if (evt.Defender == null) return;
            OnTriggerRevert(evt.Defender.Id);
        }

        private class PowerRelockEvent : CallMethodEvent<SituationalPowerComponent>
        {
            protected override CallbackDelegate GetCallback() => (t) => (t).PowerRelock();
        }
    }
}
