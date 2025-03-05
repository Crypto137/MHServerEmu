using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;
        private readonly SituationalPowerComponentPrototype _prototype;
        private readonly HashSet<ulong> _triggeringEntities = new();
        private bool _registered;

        private readonly Action<EntityDeadGameEvent> _deadAction;
        private readonly EventPointer<PowerRelockEvent> _relockEvent = new();
        private readonly EventGroup _eventGroup = new();

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

        private void RegisterEvents()
        {
            if (_registered) return;

            var region = Power.Owner?.Region;
            if (region == null) return;

            if (SituationalTrigger.RegisterEvents() == false) return;
            region.EntityDeadEvent.AddActionBack(_deadAction);

             _registered = true;
        }

        private void UnRegisterEvents()
        {
            if (_registered == false) return;
            _registered = false;

            var region = Power.Owner?.Region;
            if (region == null) return;

            SituationalTrigger.UnRegisterEvents();
            region.EntityDeadEvent.RemoveAction(_deadAction);
        }

        public void OnPowerAssigned()
        {
            if (_registered == false) return;
            if (Power.Owner?.Region == null) return;

            SituationalTrigger.OnPowerAssigned();
        }

        public void OnPowerEquipped()
        {
            if (_registered == false) return;
            if (Power.Owner?.Region == null) return;

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
                    var nearestEntity = GetNearestTriggeringEntity();
                    if (nearestEntity != null)
                        targetId = nearestEntity.Id;
;               }

                OnTriggerRevert(targetId);
            }
        }

        private void OnTriggerRevert(ulong targetId)
        {
            if (targetId == Entity.InvalidId) return;

            RemoveTriggeringEntity(targetId);

            if (_triggeringEntities.Count > 0) return;

            if (_relockEvent.IsValid == false || _prototype.ForceRelockOnTriggerRevert)  
                PowerRelock();
        }

        public bool OnTrigger(WorldEntity target)
        {
            if (target == null) return false;

            if (CanTrigger(target))
            {
                if (SituationalTrigger.Debug) Logger.Debug($"OnTrigger[{Power.Owner.PrototypeName}] Passed {target.PrototypeName} for {Power.PrototypeDataRef.GetNameFormatted()}");
                if (_prototype.SituationalTrigger.ActivateOnTriggerSuccess)
                    ActivatePower(target);
                else
                    TriggerSuccess(target.Id);

                return true;
            }
            else OnTriggerRevert(target.Id);
            
            return false;
        }

        private bool CanTrigger(WorldEntity target)
        {
            var powerOwner = Power.Owner;
            if (powerOwner == null) return false;
            
            if (TargetsTriggeringEntity) 
            {
                if (SituationalTrigger.Debug) Logger.Debug($"CanTrigger[{powerOwner.PrototypeName}] {target.PrototypeName} for {Power.PrototypeDataRef.GetNameFormatted()}");
                if (Power.IsValidTarget(target) == false) return false;
                if (Power.IsInRange(target, RangeCheckType.Activation) == false) return false;
            }

            if (SituationalTrigger.Debug) Logger.Debug($"CanTrigger ChanceToTrigger[{powerOwner.PrototypeName}] {target.PrototypeName} for {Power.PrototypeDataRef.GetNameFormatted()}");

            // chance to trigger
            if (_prototype.ChanceToTrigger == null) return false;
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, Power.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, powerOwner.Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, target.Properties);
            float chance = Eval.RunFloat(_prototype.ChanceToTrigger, evalContext);
            if (_game.Random.NextFloat() > chance) return false;

            return SituationalTrigger.CanTrigger(powerOwner, target);
        }

        private void ActivatePower(WorldEntity target)
        {
            var powerOwner = Power.Owner;
            if (powerOwner == null) return;            
            
            if (SituationalTrigger.Debug) Logger.Debug($"ActivatePower[{powerOwner.PrototypeName}] {target.PrototypeName} for {Power.PrototypeDataRef.GetNameFormatted()}");

            var powerRef = Power.PrototypeDataRef;
            if (powerRef == PrototypeId.Invalid) return;

            ulong targetId = powerOwner.Id;
            var ownerPosition = powerOwner.RegionLocation.Position;
            var targetPositon = ownerPosition;

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

        private void AddTriggeringEntity(ulong targetId)
        {
            if (targetId == Entity.InvalidId) return;
            _triggeringEntities.Add(targetId);
            SendUpdateSituationalTarget(targetId, true);
        }

        private void RemoveTriggeringEntity(ulong targetId)
        {
            if (_triggeringEntities.Remove(targetId))
                SendUpdateSituationalTarget(targetId, false);
        }

        private void SendUpdateSituationalTarget(ulong targetId, bool addTarget)
        {
            var powerOwner = Power.Owner;
            if (powerOwner == null) return;
            var networkManager = _game.NetworkManager;
            if (networkManager == null) return;

            var message = NetMessageUpdateSituationalTarget.CreateBuilder()
                .SetPowerOwnerId(powerOwner.Id)
                .SetSituationalPowerProtoId((ulong)Power.PrototypeDataRef)
                .SetSituationalTargetId(targetId)
                .SetAddTarget(addTarget)
                .Build();

            networkManager.SendMessageToInterested(message, powerOwner, AOINetworkPolicyValues.AOIChannelOwner);
        }

        private WorldEntity GetNearestTriggeringEntity()
        {
            var powerOwner = Power.Owner;
            if (powerOwner == null) return null;

            var manager = _game.EntityManager;
            WorldEntity nearestEntity = null;
            float minDistance = float.MaxValue;

            foreach (var targetId in _triggeringEntities)
            {
                var target = manager.GetEntity<WorldEntity>(targetId);
                if (target == null) continue;

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
            var owner = Power.Owner;
            if (owner == null || Power.PrototypeDataRef == PrototypeId.Invalid) return;
            owner.Properties[PropertyEnum.SinglePowerLock, Power.PrototypeDataRef] = true;
        }

        private void PowerUnlock()
        {
            var owner = Power.Owner;
            if (owner == null || Power.PrototypeDataRef == PrototypeId.Invalid) return;
            owner.Properties.RemoveProperty(new(PropertyEnum.SinglePowerLock, Power.PrototypeDataRef));
        }

        private void SchedulePowerRelock()
        {
            var timeOffset = TimeSpan.FromMilliseconds(_prototype.ActivationWindowMS);
            if (timeOffset <= TimeSpan.Zero) return;

            var scheduler = _game.GameEventScheduler;
            if (scheduler == null) return;

            if (_relockEvent.IsValid)
                scheduler.RescheduleEvent(_relockEvent, timeOffset);
            else
            {
                scheduler.ScheduleEvent(_relockEvent, timeOffset, _eventGroup);
                _relockEvent.Get().Initialize(this);
            }
        }

        private void OnDead(EntityDeadGameEvent evt)
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
