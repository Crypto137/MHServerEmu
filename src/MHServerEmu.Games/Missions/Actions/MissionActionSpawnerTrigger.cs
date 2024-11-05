using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionSpawnerTrigger : MissionActionEntityTarget
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private MissionActionSpawnerTriggerPrototype _proto;
        public MissionActionSpawnerTrigger(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // TRRestaurantKronan
            _proto = prototype as MissionActionSpawnerTriggerPrototype;
        }

        public override bool Evaluate(WorldEntity entity)
        {
            if (entity is not Spawner) return false;
            if (base.Evaluate(entity) == false) return false;         
            return true;
        }

        public override bool RunEntity(WorldEntity entity)
        {
            var spawner = entity as Spawner;
            Logger.Trace($"Spawner [{spawner.PrototypeName}] set {_proto.Trigger}");
            spawner.Trigger(_proto.Trigger);
            return true;
        }
    }
}
