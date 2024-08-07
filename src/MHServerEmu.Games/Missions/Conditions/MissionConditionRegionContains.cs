using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionRegionContains : MissionConditionContains
    {
        protected MissionConditionRegionContainsPrototype Proto => Prototype as MissionConditionRegionContainsPrototype;
        public Action<EntityEnteredWorldGameEvent> EntityEnteredWorldAction { get; private set; }
        public Action<EntityExitedWorldGameEvent> EntityExitedWorldAction { get; private set; }
        public Action<EntityDeadGameEvent> EntityDeadAction { get; private set; }

        public MissionConditionRegionContains(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            EntityEnteredWorldAction = OnEntityEnteredWorld;
            EntityExitedWorldAction = OnEntityExitedWorld;
            EntityDeadAction = OnEntityDeadAction;
        }

        protected override long CountMin => Proto.CountMin;
        protected override long CountMax => Proto.CountMax;
        protected override long MaxCount => Proto.CountMin;

        protected override bool Contains()
        {
            var proto = Proto;
            if (proto == null) return false; 
            var region = Region;
            if (region == null) return false;
            return region.FilterRegion(proto.Region, proto.RegionIncludeChildren, proto.RegionsExclude);
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            long count = 0;
            if (Contains())
                foreach (var entity in Region.Entities)
                    if (EvaluateEntityFilter(proto.TargetFilter, entity as WorldEntity))
                        count++;

            SetCount(count);
            return true;
        }

        private bool EvaluateEntity(WorldEntity entity, Region region)
        {
            var proto = Proto;
            if (proto == null || entity == null || region == null) return false;
            if (region.FilterRegion(proto.Region, proto.RegionIncludeChildren, proto.RegionsExclude) == false) return false;
            if (entity is Hotspot || entity is Missile) return false;
            if (EvaluateEntityFilter(proto.TargetFilter, entity) == false) return false;

            return true;
        }

        private void OnEntityEnteredWorld(EntityEnteredWorldGameEvent evt)
        {
            var entity = evt.Entity;
            if (entity == null) return;
            if (EvaluateEntity(entity, entity.Region))
                Count++;
        }

        private void OnEntityExitedWorld(EntityExitedWorldGameEvent evt)
        {
            var region = Region;
            if(region == null || region.TestStatus(RegionStatus.Shutdown)) return;
            var entity = evt.Entity;
            if (entity == null || entity.IsDead) return;           
            if (EvaluateEntity(entity, entity.Region))
                Count--;
        }

        private void OnEntityDeadAction(EntityDeadGameEvent evt)
        {
            var entity = evt.Defender;
            if (entity == null || entity.IsInWorld == false) return;
            if (EvaluateEntity(entity, entity.Region))
                Count--;
        }

        public override void RegisterEvents(Region region)
        {
            base.RegisterEvents(region);
            region.EntityEnteredWorldEvent.AddActionBack(EntityEnteredWorldAction);
            region.EntityExitedWorldEvent.AddActionBack(EntityExitedWorldAction);
            region.EntityDeadEvent.AddActionBack(EntityDeadAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            base.UnRegisterEvents(region);
            region.EntityEnteredWorldEvent.RemoveAction(EntityEnteredWorldAction);
            region.EntityExitedWorldEvent.RemoveAction(EntityExitedWorldAction);
            region.EntityDeadEvent.RemoveAction(EntityDeadAction);
        }
    }
}
