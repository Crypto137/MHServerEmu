using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaContains : MissionConditionContains
    {
        protected MissionConditionAreaContainsPrototype Proto => Prototype as MissionConditionAreaContainsPrototype;
        public Action<EntityEnteredAreaGameEvent> EntityEnteredAreaAction { get; private set; }
        public Action<EntityLeftAreaGameEvent> EntityLeftAreaAction { get; private set; }
        public Action<EntityDeadGameEvent> EntityDeadAction { get; private set; }
        protected override long CountMin => Proto.CountMin;
        protected override long CountMax => Proto.CountMax;
        protected override long MaxCount => Proto.CountMin;

        public MissionConditionAreaContains(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            EntityEnteredAreaAction = OnEntityEnteredArea;
            EntityLeftAreaAction = OnEntityLeftArea;
            EntityDeadAction = OnEntityDead;
        }

        protected override bool Contains()
        {
            var proto = Proto;
            if (proto == null) return false;
            var region = Region;
            if (region == null) return false;
            var area = region.GetArea(proto.Area);
            return area != null;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;
            var region = Region;
            if (region == null) return false;

            long count = 0;
            var area = region.GetArea(proto.Area);
            if (area != null)
                foreach(var entity in area.Entities)
                    if (EvaluateEntityFilter(proto.TargetFilter, entity as WorldEntity))
                        count++;

            SetCount(count);
            return true;
        }

        private bool EvaluateEntity(WorldEntity entity, Area area)
        {
            var proto = Proto;
            if (proto == null || entity == null || area == null) return false;
            if (area.PrototypeDataRef != proto.Area) return false;
            if (entity is Hotspot || entity is Missile) return false;
            if (EvaluateEntityFilter(proto.TargetFilter, entity) == false) return false;

            return true;
        }

        private void OnEntityEnteredArea(EntityEnteredAreaGameEvent evt)
        {
            if (EvaluateEntity(evt.Entity, evt.Area))
                Count++;
        }

        private void OnEntityLeftArea(EntityLeftAreaGameEvent evt)
        {
            var entity = evt.Entity;
            if (entity == null || entity.IsDead) return;
            if (EvaluateEntity(entity, evt.Area))
                Count--;
        }

        private void OnEntityDead(EntityDeadGameEvent evt)
        {
            var entity = evt.Defender;
            if (entity == null || entity.IsInWorld == false) return;
            if (EvaluateEntity(entity, entity.Area))
                Count--;
        }

        public override void RegisterEvents(Region region)
        {
            base.RegisterEvents(region);
            region.EntityEnteredAreaEvent.AddActionBack(EntityEnteredAreaAction);
            region.EntityLeftAreaEvent.AddActionBack(EntityLeftAreaAction);
            region.EntityDeadEvent.AddActionBack(EntityDeadAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            base.UnRegisterEvents(region);
            region.EntityEnteredAreaEvent.RemoveAction(EntityEnteredAreaAction);
            region.EntityLeftAreaEvent.RemoveAction(EntityLeftAreaAction);
            region.EntityDeadEvent.RemoveAction(EntityDeadAction);
        }
    }
}
