using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMemberOfEventTeam : MissionPlayerCondition
    {
        private MissionConditionMemberOfEventTeamPrototype _proto;
        private Action<PlayerEventTeamChangedGameEvent> _playerEventTeamChangedAction;

        public MissionConditionMemberOfEventTeam(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CivilWarWeeklyCap01
            _proto = prototype as MissionConditionMemberOfEventTeamPrototype;
            _playerEventTeamChangedAction = OnPlayerEventTeamChanged;
        }

        public override bool OnReset()
        {
            var eventTeamRef = _proto.Team;
            if (eventTeamRef == PrototypeId.Invalid) return false;
            var teamProto = GameDatabase.GetPrototype<PublicEventTeamPrototype>(eventTeamRef);
            if (teamProto == null || teamProto.PublicEventRef == PrototypeId.Invalid) return false;
            var eventProto = GameDatabase.GetPrototype<PublicEventPrototype>(teamProto.PublicEventRef);

            bool eventTeam = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    if (eventTeamRef == player.GetPublicEventTeam(eventProto))
                    {
                        eventTeam = true;
                        break;
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants);

            SetCompletion(eventTeam);
            return true;
        }

        private void OnPlayerEventTeamChanged(PlayerEventTeamChangedGameEvent evt)
        {
            var player = evt.Player;
            var eventTeamRef = evt.EventTeamRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.Team != eventTeamRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerEventTeamChangedEvent.AddActionBack(_playerEventTeamChangedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerEventTeamChangedEvent.RemoveAction(_playerEventTeamChangedAction);
        }
    }
}
