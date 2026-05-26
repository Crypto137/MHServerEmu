using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    public class SituationalTrigger
    {
        protected readonly SituationalTriggerPrototype _proto;

        private readonly Event<EntityCollisionEvent>.Action _overlapBeginAction;
        private readonly Event<EntityCollisionEvent>.Action _overlapEndAction;

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
            Region region = Region;
            if (region == null)
                return false;

            if (HasTriggerRadius == false)
                return true;

            Power power = PowerComponent.Power;
            if (!Verify.IsNotNull(power)) return false;

            WorldEntity powerOwner = power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return false;

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = _proto.TriggerCollider;
            settings.RegionId = region.Id;
            settings.Position = powerOwner.RegionLocation.Position;
            settings.Orientation = powerOwner.RegionLocation.Orientation;
            settings.BoundsScaleOverride = _proto.TriggerRadiusScaling;

            EntityManager entityManager = powerOwner.Game?.EntityManager;
            if (!Verify.IsNotNull(entityManager)) return false;

            WorldEntity collider = entityManager.CreateEntity(settings) as WorldEntity;
            if (!Verify.IsNotNull(collider)) return false;

            power.Properties[PropertyEnum.SituationalTriggerColliderId] = collider.Id;

            collider.AttachToEntity(powerOwner);

            collider.OverlapBeginEvent.AddActionBack(_overlapBeginAction);
            collider.OverlapEndEvent.AddActionBack(_overlapEndAction);

            return true;
        }

        public virtual void UnRegisterEvents()
        {
            if (HasTriggerRadius == false)
                return;

            Power power = PowerComponent.Power;
            if (!Verify.IsNotNull(power)) return;

            WorldEntity powerOwner = power.Owner;
            if (!Verify.IsNotNull(powerOwner)) return;

            ulong colliderId = power.Properties[PropertyEnum.SituationalTriggerColliderId];

            EntityManager entityManager = powerOwner.Game?.EntityManager;
            if (!Verify.IsNotNull(entityManager)) return;

            WorldEntity collider = entityManager.GetEntity<WorldEntity>(colliderId);
            if (!Verify.IsNotNull(collider)) return;

            collider.OverlapBeginEvent.RemoveAction(_overlapBeginAction);
            collider.OverlapEndEvent.RemoveAction(_overlapEndAction);

            collider.Destroy();

            power.Properties.RemoveProperty(PropertyEnum.SituationalTriggerColliderId);
        }

        public virtual bool CanTrigger(WorldEntity powerOwner, WorldEntity target)
        {
            if (!Verify.IsNotNull(powerOwner)) return false;
            if (!Verify.IsNotNull(target)) return false;

            bool canTrigger = true;

            if (HasTriggerRadius)
            {
                if (powerOwner == target)
                    return false;

                if (target.Bounds.CollisionType != BoundsCollisionType.Blocking)
                    return false;

                ulong colliderId = PowerComponent.Power.Properties[PropertyEnum.SituationalTriggerColliderId];

                EntityManager entityManager = powerOwner.Game?.EntityManager;
                if (!Verify.IsNotNull(entityManager)) return false;

                WorldEntity collider = entityManager.GetEntity<WorldEntity>(colliderId);
                if (!Verify.IsNotNull(collider)) return false;

                canTrigger = collider.Bounds.Intersects(ref target.Bounds);
            }

            if (canTrigger && _proto.EntityFilter != null)
                canTrigger = _proto.EntityFilter.Evaluate(target, new(powerOwner.Id, powerOwner.PartyId));

            if (canTrigger && _proto.AllowDead == false)
                canTrigger = target.IsDead == false;

            return canTrigger;
        }

        private void OnOverlap(in EntityCollisionEvent evt)
        {
            if (!Verify.IsNotNull(evt.Who)) return;
            if (!Verify.IsNotNull(evt.Whom)) return;
            if (!Verify.IsTrue(evt.Who != evt.Whom)) return;

            PowerComponent.OnTrigger(evt.Whom);
        }

        public virtual void OnPowerAssigned() { }

        public virtual void OnPowerEquipped() { }
    }

    public class SituationalTriggerOnStatusEffect : SituationalTrigger
    {
        private readonly Event<EntityStatusEffectGameEvent>.Action _entityStatusEffectAction;

        private PropertyEnum _lastStatusProp;
        private bool _lastStatusValue;

        public SituationalTriggerOnStatusEffect(SituationalTriggerPrototype prototype, SituationalPowerComponent powerComponent) : base(prototype, powerComponent)
        {
            _entityStatusEffectAction = OnStatusEffect;
        }

        public override bool RegisterEvents()
        {
            if (base.RegisterEvents() == false)
                return false;

            Region.EntityStatusEffectEvent.AddActionBack(_entityStatusEffectAction);
            return true;
        }

        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();

            Region?.EntityStatusEffectEvent.RemoveAction(_entityStatusEffectAction);
        }

        public override bool CanTrigger(WorldEntity powerOwner, WorldEntity target)
        {
            if (base.CanTrigger(powerOwner, target) == false)
                return false;

            SituationalTriggerOnStatusEffectPrototype onStatusEffectProto = _proto as SituationalTriggerOnStatusEffectPrototype;
            if (!Verify.IsNotNull(onStatusEffectProto)) return false;

            if (onStatusEffectProto.TriggeringProperties.HasValue())
            {
                foreach (PrototypeId propProtoRef in onStatusEffectProto.TriggeringProperties)
                {
                    PropertyEnum statusProp = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(propProtoRef);

                    bool status = statusProp == _lastStatusProp ? _lastStatusValue : target.Properties[statusProp];

                    if (status == onStatusEffectProto.TriggersOnStatusApplied)
                        return true;
                }
            }

            if (onStatusEffectProto.TriggeringConditionKeywords.HasValue())
            {
                foreach (PrototypeId keywordProtoRef in onStatusEffectProto.TriggeringConditionKeywords)
                {
                    if (target.HasConditionWithKeyword(keywordProtoRef))
                        return true;
                }
            }

            return false;
        }

        private void OnStatusEffect(in EntityStatusEffectGameEvent evt)
        {
            _lastStatusProp = evt.StatusProp;
            _lastStatusValue = evt.Status;

            PowerComponent.OnTrigger(evt.Entity);
        }
    }

    public class SituationalTriggerInvAndWorld : SituationalTrigger
    {
        private readonly Event<EntitySetSimulatedGameEvent>.Action _entitySetSimulatedAction;
        private readonly Event<EntityEnteredWorldGameEvent>.Action _entityEnteredWorldAction;
        private readonly Event<EntityExitedWorldGameEvent>.Action _entityExitedWorldAction;
        private readonly Event<EntityInventoryChangedEvent>.Action _entityInventoryChangedAction;
        private readonly Event<EntityResurrectEvent>.Action _entityResurrectAction;

        public SituationalTriggerInvAndWorld(SituationalTriggerPrototype prototype, SituationalPowerComponent powerComponent) : base(prototype, powerComponent)
        {
            _entityEnteredWorldAction = OnEnteredWorld;
            _entityExitedWorldAction = OnExitedWorld;
            _entityInventoryChangedAction = OnInventoryChanged;
            _entityResurrectAction = OnResurrect;
            _entitySetSimulatedAction = OnSetSimulated;
        }

        public override bool RegisterEvents()
        {
            if (base.RegisterEvents() == false)
                return false;

            WorldEntity powerOwner = PowerOwner;
            if (!Verify.IsNotNull(powerOwner)) return false;

            Region region = Region;
            if (region == null)
                return false;

            region.EntitySetSimulatedEvent.AddActionBack(_entitySetSimulatedAction);
            //region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);
            region.EntityExitedWorldEvent.AddActionBack(_entityExitedWorldAction);
            region.EntityResurrectEvent.AddActionBack(_entityResurrectAction);

            powerOwner.EntityInventoryChangedEvent.AddActionBack(_entityInventoryChangedAction);

            return true;
        }

        public override void UnRegisterEvents()
        {
            base.UnRegisterEvents();

            WorldEntity powerOwner = PowerOwner;
            if (!Verify.IsNotNull(powerOwner)) return;

            powerOwner.EntityInventoryChangedEvent.RemoveAction(_entityInventoryChangedAction);

            Region region = Region;
            if (region == null)
                return;

            region.EntitySetSimulatedEvent.RemoveAction(_entitySetSimulatedAction);
            //region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
            region.EntityExitedWorldEvent.RemoveAction(_entityExitedWorldAction);
            region.EntityResurrectEvent.RemoveAction(_entityResurrectAction);
        }

        public override bool CanTrigger(WorldEntity powerOwner, WorldEntity target)
        {
            if (base.CanTrigger(powerOwner, target) == false)
                return false;

            SituationalTriggerInvAndWorldPrototype invAndWorldProto = _proto as SituationalTriggerInvAndWorldPrototype;
            if (!Verify.IsNotNull(invAndWorldProto)) return false;

            if (target.IsInWorld == false)
                return false;

            ref InventoryLocation invLoc = ref target.InventoryLocation;

            if (invLoc.ContainerId != powerOwner.Id)
                return false;

            if (invLoc.InventoryRef != invAndWorldProto.InventoryRef)
                return false;

            return true;
        }

        public override void OnPowerAssigned()
        {
            TriggerInventory();
        }

        public override void OnPowerEquipped()
        {
            TriggerInventory();
        }

        private void TriggerInventory()
        {
            WorldEntity powerOwner = PowerOwner;
            if (!Verify.IsNotNull(powerOwner)) return;

            SituationalTriggerInvAndWorldPrototype invAndWorldProto = _proto as SituationalTriggerInvAndWorldPrototype;
            if (!Verify.IsNotNull(invAndWorldProto)) return;

            Inventory inventory = powerOwner.GetInventoryByRef(invAndWorldProto.InventoryRef);
            if (inventory == null)
                return;

            EntityManager entityManager = powerOwner.Game?.EntityManager;
            if (!Verify.IsNotNull(entityManager)) return;

            foreach (var entry in inventory)
            {
                WorldEntity entity = entityManager.GetEntity<WorldEntity>(entry.Id);
                if (entity == null)
                    continue;

                if (PowerComponent.OnTrigger(entity))
                    return;
            }
        }

        private void OnSetSimulated(in EntitySetSimulatedGameEvent evt)
        {
            if (!Verify.IsNotNull(evt.Entity)) return;
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnEnteredWorld(in EntityEnteredWorldGameEvent evt)
        {
            if (!Verify.IsNotNull(evt.Entity)) return;
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnExitedWorld(in EntityExitedWorldGameEvent evt)
        {
            if (!Verify.IsNotNull(evt.Entity)) return;
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnResurrect(in EntityResurrectEvent evt)
        {
            if (!Verify.IsNotNull(evt.Entity)) return;
            PowerComponent.OnTrigger(evt.Entity);
        }

        private void OnInventoryChanged(in EntityInventoryChangedEvent evt)
        {
            if (evt.Entity is not WorldEntity entity) return;
            PowerComponent.OnTrigger(entity);
        }
    }
}
