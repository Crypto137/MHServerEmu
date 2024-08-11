using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionRegionContains : MissionConditionContains
    {
        private MissionConditionRegionContainsPrototype _proto;
        private Action<EntityEnteredWorldGameEvent> _entityEnteredWorldAction;
        private Action<EntityExitedWorldGameEvent> _entityExitedWorldAction;
        private Action<EntityDeadGameEvent> _entityDeadAction;

        public MissionConditionRegionContains(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionRegionContainsPrototype;
            _entityEnteredWorldAction = OnEntityEnteredWorld;
            _entityExitedWorldAction = OnEntityExitedWorld;
            _entityDeadAction = OnEntityDead;
        }

        protected override long CountMin => _proto.CountMin;
        protected override long CountMax => _proto.CountMax;

        protected override bool Contains()
        {
            var region = Region;
            if (region == null) return false;
            return region.FilterRegion(_proto.Region, _proto.RegionIncludeChildren, _proto.RegionsExclude);
        }

        public override bool OnReset()
        {
            long count = 0;
            if (Contains())
                foreach (var entity in Region.Entities)
                    if (EvaluateEntityFilter(_proto.TargetFilter, entity as WorldEntity))
                        count++;

            SetCount(count);
            return true;
        }

        private bool EvaluateEntity(WorldEntity entity, Region region)
        {
            if (entity == null || region == null) return false;
            if (region.FilterRegion(_proto.Region, _proto.RegionIncludeChildren, _proto.RegionsExclude) == false) return false;
            if (entity is Hotspot || entity is Missile) return false;
            if (EvaluateEntityFilter(_proto.TargetFilter, entity) == false) return false;

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

        private void OnEntityDead(EntityDeadGameEvent evt)
        {
            var entity = evt.Defender;
            if (entity == null || entity.IsInWorld == false) return;
            if (EvaluateEntity(entity, entity.Region))
                Count--;
        }

        public override void RegisterEvents(Region region)
        {
            base.RegisterEvents(region);
            region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);
            region.EntityExitedWorldEvent.AddActionBack(_entityExitedWorldAction);
            region.EntityDeadEvent.AddActionBack(_entityDeadAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            base.UnRegisterEvents(region);
            region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
            region.EntityExitedWorldEvent.RemoveAction(_entityExitedWorldAction);
            region.EntityDeadEvent.RemoveAction(_entityDeadAction);
        }
    }
}
