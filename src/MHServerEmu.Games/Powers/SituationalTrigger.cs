using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public class SituationalTrigger
    {
        public static bool Debug = false;
        protected static readonly Logger Logger = LogManager.CreateLogger();
        private readonly SituationalTriggerPrototype _proto;
        private readonly Action<EntityCollisionEvent> _overlapBeginAction;
        private readonly Action<EntityCollisionEvent> _overlapEndAction;

        public SituationalPowerComponent PowerComponent { get; private set; }
        public WorldEntity PowerOwner { get => PowerComponent.Power?.Owner; }
        public Region Region { get => PowerOwner?.Region; }
        public bool HasTriggerRadius { get => _proto.TriggerCollider != PrototypeId.Invalid && _proto.TriggerRadiusScaling > 0.0f; }

        public SituationalTrigger(SituationalTriggerPrototype prototype, SituationalPowerComponent powerComponent)
        {
            _proto = prototype;
            PowerComponent = powerComponent;
            _overlapBeginAction = OnOverlap;
            _overlapEndAction = OnOverlap;
        }

        public virtual bool RegisterEvents()
        {
            var region = Region;
            if (region == null) return false;

            if (HasTriggerRadius == false) return true;

            var power = PowerComponent.Power;
            if (power == null) return false;

            var powerOwner = power.Owner;
            if (powerOwner == null) return false;

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = _proto.TriggerCollider;
            settings.RegionId = region.Id;
            settings.Position = powerOwner.RegionLocation.Position;
            settings.Orientation = powerOwner.RegionLocation.Orientation;
            settings.BoundsScaleOverride = _proto.TriggerRadiusScaling;

            var manager = powerOwner.Game?.EntityManager;
            if (manager == null) return false;

            var collider = manager.CreateEntity(settings) as WorldEntity;
            if (collider == null) return false;

            power.Properties[PropertyEnum.SituationalTriggerColliderId] = collider.Id;

            collider.AttachToEntity(powerOwner);

            collider.OverlapBeginEvent.AddActionBack(_overlapBeginAction);
            collider.OverlapEndEvent.AddActionBack(_overlapEndAction);

            return true;
        }

        public virtual void UnRegisterEvents()
        {
            if (HasTriggerRadius == false) return;

            var power = PowerComponent.Power;
            if (power == null) return;

            var powerOwner = power.Owner;
            if (powerOwner == null) return;

            var manager = powerOwner.Game?.EntityManager;
            if (manager == null) return;

            ulong colliderId = power.Properties[PropertyEnum.SituationalTriggerColliderId];

            var collider = manager.GetEntity<WorldEntity>(colliderId);
            if (collider == null) return;

            collider.OverlapBeginEvent.RemoveAction(_overlapBeginAction);
            collider.OverlapEndEvent.RemoveAction(_overlapEndAction);

            collider.Destroy();

            power.Properties.RemoveProperty(PropertyEnum.SituationalTriggerColliderId);
        }

        public virtual bool CanTrigger(WorldEntity powerOwner, WorldEntity target)
        {
            if (powerOwner == null || target == null) return false;

            bool canTrigger = true;

            if (HasTriggerRadius)
            {
                if (powerOwner == target) return false;
                if (target.Bounds.CollisionType != BoundsCollisionType.Blocking) return false;

                ulong colliderId = PowerComponent.Power.Properties[PropertyEnum.SituationalTriggerColliderId];
                var manager = powerOwner.Game?.EntityManager;
                if (manager == null) return false;

                var collider = manager.GetEntity<WorldEntity>(colliderId);
                if (collider == null) return false;

                canTrigger = collider.Bounds.Intersects(target.Bounds);
            }

            if (canTrigger && _proto.EntityFilter != null)
                canTrigger = _proto.EntityFilter.Evaluate(target, new(powerOwner.Id, powerOwner.PartyId));

            if (canTrigger && _proto.AllowDead == false)
                canTrigger = target.IsDead == false;

            return canTrigger;
        }

        private void OnOverlap(EntityCollisionEvent evt)
        {
            if (evt.Who == null || evt.Whom == null || evt.Who == evt.Whom) return;
            if (Debug) Logger.Debug($"OnOverlap[{PowerOwner.PrototypeName}] {evt.Who}");
            PowerComponent.OnTrigger(evt.Whom);
        }

        public virtual void OnPowerAssigned() { }
        public virtual void OnPowerEquipped() { }
    }

    public class SituationalTriggerOnStatusEffect : SituationalTrigger
    {
        private readonly SituationalTriggerOnStatusEffectPrototype _proto;
        private readonly Action<EntityStatusEffectGameEvent> _entityStatusEffectAction; 
        
        public PropertyEnum StatusProp;
        public bool Status;
        public SituationalTriggerOnStatusEffect(SituationalTriggerPrototype prototype, SituationalPowerComponent powerComponent) : base(prototype, powerComponent)
        {
            _proto = prototype as SituationalTriggerOnStatusEffectPrototype;
            _entityStatusEffectAction = OnStatusEffect;
        }

        public override bool CanTrigger(WorldEntity powerOwner, WorldEntity target)
        {
            if (base.CanTrigger(powerOwner, target) == false) return false;

            if (_proto.TriggeringProperties.HasValue())
                foreach (var propRef in _proto.TriggeringProperties)
                {
                    var statusProp = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(propRef);
                    bool status;

                    if (StatusProp == statusProp)
                        status = Status;
                    else
                        status = target.Properties[statusProp];

                    if (status == _proto.TriggersOnStatusApplied) return true;
                }

            if (_proto.TriggeringConditionKeywords.HasValue())
                foreach (var keyword in _proto.TriggeringConditionKeywords)
                    if (target.HasConditionWithKeyword(keyword))
                        return true;

            return false;
        }

        public override bool RegisterEvents()
        {
            if (base.RegisterEvents() == false) return false;
            Region.EntityStatusEffectEvent.AddActionBack(_entityStatusEffectAction);
            return true;
        }

        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();
            Region?.EntityStatusEffectEvent.RemoveAction(_entityStatusEffectAction);
        }

        private void OnStatusEffect(EntityStatusEffectGameEvent evt)
        {
            StatusProp = evt.StatusProp;
            Status = evt.Status;
            if (Debug) Logger.Debug($"OnStatusEffect[{PowerOwner.PrototypeName}] {evt.Entity}");
            PowerComponent.OnTrigger(evt.Entity);
        }

    }

    public class SituationalTriggerInvAndWorld : SituationalTrigger
    {
        private readonly SituationalTriggerInvAndWorldPrototype _proto;
        private readonly Action<EntitySetSimulatedGameEvent> _entitySetSimulatedAction;
        private readonly Action<EntityEnteredWorldGameEvent> _entityEnteredWorldAction;
        private readonly Action<EntityExitedWorldGameEvent> _entityExitedWorldAction;
        private readonly Action<EntityInventoryChangedEvent> _entityInventoryChangedAction;
        private readonly Action<EntityResurrectEvent> _entityRessurectAction;

        public SituationalTriggerInvAndWorld(SituationalTriggerPrototype prototype, SituationalPowerComponent powerComponent) : base(prototype, powerComponent)
        {
            _proto = prototype as SituationalTriggerInvAndWorldPrototype;

            _entityEnteredWorldAction = OnEnteredWorld;
            _entityExitedWorldAction = OnExitedWorld;
            _entityInventoryChangedAction = OnInventoryChanged;
            _entityRessurectAction = OnRessurect;
            _entitySetSimulatedAction = OnSetSimulated;
        }

        public override bool CanTrigger(WorldEntity powerOwner, WorldEntity target)
        {
            if (base.CanTrigger(powerOwner, target) == false) return false;

            if (target.IsInWorld == false) return false;

            var invLoc = target.InventoryLocation;
            if (invLoc.ContainerId != powerOwner.Id) return false;
            if (invLoc.InventoryRef != _proto.InventoryRef) return false;

            return true;
        }

        public override bool RegisterEvents()
        {
            if (base.RegisterEvents() == false) return false;

            var powerOwner = PowerOwner;
            var region = Region;
            if (powerOwner == null || region == null) return false;

            region.EntitySetSimulatedEvent.AddActionBack(_entitySetSimulatedAction);
            //region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);
            region.EntityExitedWorldEvent.AddActionBack(_entityExitedWorldAction);
            region.EntityResurrectEvent.AddActionBack(_entityRessurectAction);
            powerOwner.EntityInventoryChangedEvent.AddActionBack(_entityInventoryChangedAction);
            return true;
        }

        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();

            var powerOwner = PowerOwner; 
            if (powerOwner == null) return;
            powerOwner.EntityInventoryChangedEvent.RemoveAction(_entityInventoryChangedAction);

            var region = Region;
            if (region == null) return;
            region.EntitySetSimulatedEvent.RemoveAction(_entitySetSimulatedAction);
            //region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
            region.EntityExitedWorldEvent.RemoveAction(_entityExitedWorldAction);
            region.EntityResurrectEvent.RemoveAction(_entityRessurectAction);
        }

        public override void OnPowerAssigned() => TriggerInventory();
        public override void OnPowerEquipped() => TriggerInventory();

        private void TriggerInventory()
        {
            var powerOwner = PowerOwner;
            if (powerOwner == null) return;

            var manager = powerOwner.Game?.EntityManager;
            if (manager == null) return;

            var inventory = powerOwner.GetInventoryByRef(_proto.InventoryRef);
            if (inventory == null) return;

            foreach (var entry in inventory)
            {
                var entity = manager.GetEntity<WorldEntity>(entry.Id);
                if (entity == null) continue;
                if (PowerComponent.OnTrigger(entity)) return;
            }
        }

        private void OnSetSimulated(EntitySetSimulatedGameEvent evt)
        {
            if (evt.Entity == null) return;
            if (Debug) Logger.Debug($"OnSetSimulated[{PowerOwner.PrototypeName}] {evt.Entity}");
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnEnteredWorld(EntityEnteredWorldGameEvent evt)
        {
            if (evt.Entity == null) return;
            if (Debug) Logger.Debug($"OnEnteredWorld[{PowerOwner.PrototypeName}] {evt.Entity}");
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnExitedWorld(EntityExitedWorldGameEvent evt)
        {
            if (evt.Entity == null) return;
            if (Debug) Logger.Debug($"OnExitedWorld[{PowerOwner.PrototypeName}] {evt.Entity}");
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnRessurect(EntityResurrectEvent evt)
        {
            if (evt.Entity == null) return;
            if (Debug) Logger.Debug($"OnRessurect[{PowerOwner.PrototypeName}] {evt.Entity}");
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnInventoryChanged(EntityInventoryChangedEvent evt)
        {
            if (evt.Entity is not WorldEntity entity) return;
            if (Debug) Logger.Debug($"OnInventoryChanged[{PowerOwner.PrototypeName}] {evt.Entity}");
            PowerComponent.OnTrigger(entity);
        }
    }
}
