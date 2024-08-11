using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaBeginTravelTo : MissionPlayerCondition
    {
        private MissionConditionAreaBeginTravelToPrototype _proto;
        private Action<PlayerBeginTravelToAreaGameEvent> _playerBeginTravelToAreaAction;

        public MissionConditionAreaBeginTravelTo(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionAreaBeginTravelToPrototype;
            _playerBeginTravelToAreaAction = OnPlayerBeginTravelToArea;
        }

        public override bool OnReset()
        {
            ResetCompleted();
            return true;
        }

        private void OnPlayerBeginTravelToArea(PlayerBeginTravelToAreaGameEvent evt)
        {
            var player = evt.Player;
            var areaRef = evt.AreaRef;

            if (_proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (_proto.AreaPrototype != areaRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerBeginTravelToAreaEvent.AddActionBack(_playerBeginTravelToAreaAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerBeginTravelToAreaEvent.RemoveAction(_playerBeginTravelToAreaAction);
        }
    }
}
