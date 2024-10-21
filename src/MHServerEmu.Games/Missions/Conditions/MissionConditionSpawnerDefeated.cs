using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionSpawnerDefeated : MissionPlayerCondition
    {
        private MissionConditionSpawnerDefeatedPrototype _proto;
        private Action<SpawnerDefeatedGameEvent> _spawnerDefeatedAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionSpawnerDefeated(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CivilWarDailyCapOM04SaveDumDum
            _proto = prototype as MissionConditionSpawnerDefeatedPrototype; 
            _spawnerDefeatedAction = OnSpawnerDefeated;
        }

        private void OnSpawnerDefeated(SpawnerDefeatedGameEvent evt)
        {
            var player = evt.Player;
            var spawner = evt.Spawner;
            if (spawner == null) return;

            if (Mission.IsOpenMission == false)
            {
                bool tagged = false;
                var tagPlayers = spawner.TagPlayers;
                foreach (var tagPlayer in tagPlayers.GetPlayers())
                    if (IsMissionPlayer(tagPlayer))
                    {
                        tagged = true;
                        break;
                    }

                if (tagged == false && (player == null || IsMissionPlayer(player) == false)) return;
            }

            if (EvaluateEntityFilter(_proto.EntityFilter, spawner) == false) return;

            if (player != null) UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.SpawnerDefeatedEvent.AddActionBack(_spawnerDefeatedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.SpawnerDefeatedEvent.RemoveAction(_spawnerDefeatedAction);
        }
    }
}
