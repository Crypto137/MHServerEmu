using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityDestroy : MissionActionEntityTarget
    {
        public MissionActionEntityDestroy(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // MGNgaraiInvasion
        }

        public override bool Evaluate(WorldEntity entity)
        {
            if (base.Evaluate(entity) == false) return false;
            if (entity.IsControlledEntity) return false;
            return true;
        }

        public override bool RunEntity(WorldEntity entity)
        {
            if (entity.IsDestroyed) return false;
            if (entity is Avatar || entity.IsTeamUpAgent) return true;

            var spec = entity.SpawnSpec;
            if (spec != null) 
                spec.Destroy();
            else 
                entity.Destroy();

            return true;
        }

        public override bool RunOnStart() => false;
    }
}
