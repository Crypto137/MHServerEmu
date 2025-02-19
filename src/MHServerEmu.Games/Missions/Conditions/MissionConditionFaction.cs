using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionFaction : MissionPlayerCondition
    {
        private MissionConditionFactionPrototype _proto;
        private Action<PlayerFactionChangedGameEvent> _playerFactionChangedAction;

        public MissionConditionFaction(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // Faction1PortalController
            _proto = prototype as MissionConditionFactionPrototype;
            _playerFactionChangedAction = OnPlayerFactionChanged;
        }

        public override bool OnReset()
        {
            bool factionFound = false;
            if (_proto.EventOnly == false)
            {
                List<Player> participants = ListPool<Player>.Instance.Get();
                foreach (var player in participants)
                {
                    if (player.Faction == _proto.Faction)
                    {
                        factionFound = true;
                        break;
                    }
                }
                ListPool<Player>.Instance.Return(participants);
            }

            SetCompletion(factionFound);
            return true;
        }

        private void OnPlayerFactionChanged(PlayerFactionChangedGameEvent evt)
        {
            var player = evt.Player;
            var factionRef = evt.FactionRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (factionRef != _proto.Faction) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerFactionChangedEvent.AddActionBack(_playerFactionChangedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerFactionChangedEvent.RemoveAction(_playerFactionChangedAction);
        }
    }
}
