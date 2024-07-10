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
            _spawnSpec.Spawn();
            return true;
        }
    }
}
