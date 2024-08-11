using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaLeave : MissionPlayerCondition
    {
        private MissionConditionAreaLeavePrototype _proto;
        private Action<PlayerLeftAreaGameEvent> _playerLeftAreaAction;

        public MissionConditionAreaLeave(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionAreaLeavePrototype;
            _playerLeftAreaAction = OnPlayerLeftArea;
        }

        public override bool OnReset()
        {

            bool areaLeave = true;
            foreach (var player in Mission.GetParticipants())
            {
                var area = player.CurrentAvatar?.Area;
                if (area != null && area.PrototypeDataRef == _proto.AreaPrototype)
                {
                    areaLeave = false;
                    break;
                }
            }

            SetCompletion(areaLeave);
            return true;
        }

        private void OnPlayerLeftArea(PlayerLeftAreaGameEvent evt)
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
            region.PlayerLeftAreaEvent.AddActionBack(_playerLeftAreaAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerLeftAreaEvent.RemoveAction(_playerLeftAreaAction);
        }
    }
}
