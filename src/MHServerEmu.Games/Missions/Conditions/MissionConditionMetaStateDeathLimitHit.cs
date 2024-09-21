using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.MetaGames.MetaStates;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMetaStateDeathLimitHit : MissionPlayerCondition
    {
        private MissionConditionMetaStateDeathLimitHitPrototype _proto;
        private Action<PlayerDeathLimitHitGameEvent> _playerDeathLimitHitAction;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionMetaStateDeathLimitHit(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // DRMissionBonusChestDeathTracker
            _proto = prototype as MissionConditionMetaStateDeathLimitHitPrototype;
            _playerDeathLimitHitAction = OnPlayerDeathLimitHit;
        }

        public override bool OnReset()
        {
            ResetCompleted();

            var region = Mission.Region;
            if (region == null) return false;
            var manager = Game.EntityManager;
            foreach (var metaGameId in region.MetaGames)
            {
                var metaGame = manager.GetEntity<MetaGame>(metaGameId);
                if (metaGame != null)
                {
                    var metaState = metaGame.GetState(_proto.MetaState);
                    if (metaState is MetaStateLimitPlayerDeaths deathState && deathState.DeathLimit()) 
                        SetCompleted();
                }
            }
            return true;
        }

        private void OnPlayerDeathLimitHit(PlayerDeathLimitHitGameEvent evt)
        {
            var player = evt.Player;
            var metaStateRef = evt.MetaStateRef;

            if (player == null || metaStateRef == PrototypeId.Invalid) return;
            if (_proto.MetaState != metaStateRef) return;

            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerDeathLimitHitEvent.AddActionBack(_playerDeathLimitHitAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerDeathLimitHitEvent.RemoveAction(_playerDeathLimitHitAction);
        }
    }
}
