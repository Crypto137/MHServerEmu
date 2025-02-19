using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaEnter : MissionPlayerCondition
    {
        private MissionConditionAreaEnterPrototype _proto;
        private Action<PlayerEnteredAreaGameEvent> _playerEnteredAreaAction;

        public MissionConditionAreaEnter(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH01Main2VenomsVengeance
            _proto = prototype as MissionConditionAreaEnterPrototype;
            _playerEnteredAreaAction = OnPlayerEnteredArea;
        }

        public override bool OnReset()
        {
            bool areaEnter = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    var area = player.CurrentAvatar?.Area;
                    if (area != null && area.PrototypeDataRef == _proto.AreaPrototype)
                    {
                        areaEnter = true;
                        break;
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants);

            SetCompletion(areaEnter);
            return true;
        }

        private void OnPlayerEnteredArea(PlayerEnteredAreaGameEvent evt)
        {
            var player = evt.Player;
            var areaRef = evt.AreaRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.AreaPrototype != areaRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerEnteredAreaEvent.AddActionBack(_playerEnteredAreaAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerEnteredAreaEvent.RemoveAction(_playerEnteredAreaAction);
        }
    }
}
