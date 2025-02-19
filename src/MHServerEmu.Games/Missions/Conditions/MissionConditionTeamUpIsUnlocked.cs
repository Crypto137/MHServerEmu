using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionTeamUpIsUnlocked : MissionPlayerCondition
    {
        private MissionConditionTeamUpIsUnlockedPrototype _proto;
        private Action<PlayerUnlockedTeamUpGameEvent> _playerUnlockedTeamUpAction;

        public MissionConditionTeamUpIsUnlocked(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // SHIELDTeamUpController
            _proto = prototype as MissionConditionTeamUpIsUnlockedPrototype;
            _playerUnlockedTeamUpAction = OnPlayerUnlockedTeamUp;
        }

        public override bool OnReset()
        {
            bool isUnlocked = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    if (player.IsTeamUpAgentUnlocked(_proto.TeamUpPrototype))
                    {
                        isUnlocked = true;
                        break;
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants);

            SetCompletion(isUnlocked);
            return true;
        }

        private void OnPlayerUnlockedTeamUp(PlayerUnlockedTeamUpGameEvent evt)
        {
            var player = evt.Player;
            var teamUpRef = evt.TeamUpRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.TeamUpPrototype != teamUpRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerUnlockedTeamUpEvent.AddActionBack(_playerUnlockedTeamUpAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerUnlockedTeamUpEvent.RemoveAction(_playerUnlockedTeamUpAction);
        }
    }
}
