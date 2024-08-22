using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityKill : MissionActionEntityTarget
    {
        private MissionActionEntityKillPrototype _proto;
        public MissionActionEntityKill(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // MGNgaraiInvasion
            _proto = prototype as MissionActionEntityKillPrototype;
        }

        public override bool Evaluate(WorldEntity entity)
        {
            if (base.Evaluate(entity) == false) return false;
            if (entity.IsDead || entity.IsDestroyed) return false;
            return true;
        }

        public override bool RunEntity(WorldEntity entity)
        {
            entity.Kill(null, _proto.KillFlags);
            return true;
        }

        public override bool RunOnStart() => false;
    }
}
