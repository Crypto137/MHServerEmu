using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionHotspotContains : MissionConditionContains
    {
        protected MissionConditionHotspotContainsPrototype Proto => Prototype as MissionConditionHotspotContainsPrototype;
        public Action<EntityEnteredMissionHotspotGameEvent> EntityEnteredMissionHotspotAction { get; private set; }
        public Action<EntityLeftMissionHotspotGameEvent> EntityLeftMissionHotspotAction { get; private set; }
        public Action<EntityDeadGameEvent> EntityDeadAction { get; private set; }

        public MissionConditionHotspotContains(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            EntityEnteredMissionHotspotAction = OnEntityEnteredMissionHotspot;
            EntityLeftMissionHotspotAction = OnEntityLeftMissionHotspot;
            EntityDeadAction = OnEntityDeadAction;
        }

        protected override long CountMin => Proto.CountMin;
        protected override long CountMax => Proto.CountMax;

        protected override bool Contains()
        {
            var proto = Proto;
            if (proto == null) return false;
            var manager = Game.EntityManager;
            if (proto.TargetFilter != null && Mission.GetMissionHotspots(out var hotspots))
                foreach(var hotspotId in hotspots)
                {
                    var hotspot = manager.GetEntity<Hotspot>(hotspotId);
                    if (hotspot != null && EvaluateEntityFilter(proto.EntityFilter, hotspot))
                        return true;
                }

            return false;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            long count = 0;
            if (proto.TargetFilter != null) 
            {  
                var manager = Game.EntityManager;
                var missionRef = Mission.PrototypeDataRef;
                if (Mission.GetMissionHotspots(out var hotspots))
                    foreach (var hotspotId in hotspots)
                    {
                        var hotspot = manager.GetEntity<Hotspot>(hotspotId);
                        if (hotspot != null && EvaluateEntityFilter(proto.EntityFilter, hotspot))
                            count += hotspot.GetMissionConditionCount(missionRef, proto);
                    }
            }

            SetCount(count);
            return true;
        }

        private bool EvaluateEntity(WorldEntity target, Hotspot hotspot)
        {
            var proto = Proto;
            if (proto == null || target == null || hotspot == null) return false;
            if (EvaluateEntityFilter(proto.EntityFilter, hotspot) == false) return false;
            if (target is Hotspot || target is Missile) return false;
            if (EvaluateEntityFilter(proto.TargetFilter, target) == false) return false;

            return true;
        }

        private void OnEntityEnteredMissionHotspot(EntityEnteredMissionHotspotGameEvent evt)
        {
            if (EvaluateEntity(evt.Target, evt.Hotspot))
                Count++;
        }

        private void OnEntityLeftMissionHotspot(EntityLeftMissionHotspotGameEvent evt)
        {
            if (EvaluateEntity(evt.Target, evt.Hotspot))
                Count--;
        }

        private void OnEntityDeadAction(EntityDeadGameEvent evt)
        {
            var proto = Proto;
            if (proto == null) return;
            var entity = evt.Defender;
            if (entity == null) return;
            var manager = Game.EntityManager;

            if (Mission.GetMissionHotspots(out var hotspots))
                foreach (var hotspotId in hotspots)
                {
                    var hotspot = manager.GetEntity<Hotspot>(hotspotId);
                    if (hotspot != null && EvaluateEntityFilter(proto.EntityFilter, hotspot))
                    {
                        if (hotspot.Physics.IsOverlappingEntity(entity.Id) && EvaluateEntity(entity, hotspot))
                            Count--;
                        return;
                    }
                }
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.EntityEnteredMissionHotspotEvent.AddActionBack(EntityEnteredMissionHotspotAction);
            region.EntityLeftMissionHotspotEvent.AddActionBack(EntityLeftMissionHotspotAction);
            region.EntityDeadEvent.AddActionBack(EntityDeadAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.EntityEnteredMissionHotspotEvent.RemoveAction(EntityEnteredMissionHotspotAction);
            region.EntityLeftMissionHotspotEvent.RemoveAction(EntityLeftMissionHotspotAction);
            region.EntityDeadEvent.RemoveAction(EntityDeadAction);
        }
    }
}
