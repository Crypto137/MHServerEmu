using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaContains : MissionConditionContains
    {
        private MissionConditionAreaContainsPrototype _proto;
        private Event<EntityEnteredAreaGameEvent>.Action _entityEnteredAreaAction;
        private Event<EntityLeftAreaGameEvent>.Action _entityLeftAreaAction;
        private Event<EntityDeadGameEvent>.Action _entityDeadAction;
        protected override long CountMin => _proto.CountMin;
        protected override long CountMax => _proto.CountMax;

        public MissionConditionAreaContains(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH04M2PoisonInTheStreets
            _proto = prototype as MissionConditionAreaContainsPrototype;
            _entityEnteredAreaAction = OnEntityEnteredArea;
            _entityLeftAreaAction = OnEntityLeftArea;
            _entityDeadAction = OnEntityDead;
        }

        protected override bool Contains()
        {
            var region = Region;
            if (region == null) return false;
            var area = region.GetArea(_proto.Area);
            return area != null;
        }

        public override bool OnReset()
        {
            var region = Region;
            if (region == null) return false;

            long count = 0;
            var area = region.GetArea(_proto.Area);
            if (area != null)
                foreach(var entity in area.Entities)
                    if (EvaluateEntityFilter(_proto.TargetFilter, entity as WorldEntity))
                        count++;

            SetCount(count);
            return true;
        }

        private bool EvaluateEntity(WorldEntity entity, Area area)
        {
            if (entity == null || area == null) return false;
            if (area.PrototypeDataRef != _proto.Area) return false;
            if (entity is Hotspot || entity is Missile) return false;
            if (EvaluateEntityFilter(_proto.TargetFilter, entity) == false) return false;

            return true;
        }

        private void OnEntityEnteredArea(in EntityEnteredAreaGameEvent evt)
        {
            if (EvaluateEntity(evt.Entity, evt.Area))
                Count++;
        }

        private void OnEntityLeftArea(in EntityLeftAreaGameEvent evt)
        {
            var entity = evt.Entity;
            if (entity == null || entity.IsDead) return;
            if (EvaluateEntity(entity, evt.Area))
                Count--;
        }

        private void OnEntityDead(in EntityDeadGameEvent evt)
        {
            var entity = evt.Defender;
            if (entity == null || entity.IsInWorld == false) return;
            if (EvaluateEntity(entity, entity.Area))
                Count--;
        }

        public override void RegisterEvents(Region region)
        {
            base.RegisterEvents(region);
            region.EntityEnteredAreaEvent.AddActionBack(_entityEnteredAreaAction);
            region.EntityLeftAreaEvent.AddActionBack(_entityLeftAreaAction);
            region.EntityDeadEvent.AddActionBack(_entityDeadAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            base.UnRegisterEvents(region);
            region.EntityEnteredAreaEvent.RemoveAction(_entityEnteredAreaAction);
            region.EntityLeftAreaEvent.RemoveAction(_entityLeftAreaAction);
            region.EntityDeadEvent.RemoveAction(_entityDeadAction);
        }
    }
}
