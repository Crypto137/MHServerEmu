using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntSelEvtBroadcast : MissionActionEntityTarget
    {
        private MissionActionEntSelEvtBroadcastPrototype _proto;
        public MissionActionEntSelEvtBroadcast(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // HeliInvBehaviorController
            _proto = prototype as MissionActionEntSelEvtBroadcastPrototype;
        }

        public override bool Evaluate(WorldEntity entity)
        {
            if (base.Evaluate(entity) == false) return false;
            if (entity.IsDead) return false;
            return true;
        }

        public override bool RunEntity(WorldEntity entity)
        {
            entity.TriggerEntityActionEvent(_proto.EventToBroadcast);
            return true;
        }

        public override bool RunOnStart() => false;
    }
}
