using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionRegionBeginTravelTo : MissionPlayerCondition
    {
        private MissionConditionRegionBeginTravelToPrototype _proto;
        private Event<PlayerBeginTravelToRegionGameEvent>.Action _playerBeginTravelToRegionAction;

        public MissionConditionRegionBeginTravelTo(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // RaftNPEMotionComicRaftEscape
            _proto = prototype as MissionConditionRegionBeginTravelToPrototype;
            _playerBeginTravelToRegionAction = OnPlayerBeginTravelToRegion;
        }

        public override bool OnReset()
        {
            ResetCompleted();
            return true;
        }

        private void OnPlayerBeginTravelToRegion(in PlayerBeginTravelToRegionGameEvent evt)
        {
            var player = evt.Player;
            var regionRef = evt.RegionRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.RegionPrototype != regionRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerBeginTravelToRegionEvent.AddActionBack(_playerBeginTravelToRegionAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerBeginTravelToRegionEvent.RemoveAction(_playerBeginTravelToRegionAction);
        }
    }
}
