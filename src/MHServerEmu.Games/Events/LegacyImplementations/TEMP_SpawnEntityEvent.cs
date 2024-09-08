using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Populations;

namespace MHServerEmu.Games.Events.LegacyImplementations
{
    public class TEMP_SpawnEntityEvent : ScheduledEvent
    {
        private SpawnSpec _spawnSpec;

        public void Initialize(SpawnSpec spawnSpec)
        {
            _spawnSpec = spawnSpec;
        }

        public override bool OnTriggered()
        {
            WorldEntity activeEntity = _spawnSpec.ActiveEntity;
            if (activeEntity != null && activeEntity.IsDestructible && activeEntity.IsDead)
                activeEntity.Destroy();

            _spawnSpec.Spawn();
            return true;
        }
    }
}
