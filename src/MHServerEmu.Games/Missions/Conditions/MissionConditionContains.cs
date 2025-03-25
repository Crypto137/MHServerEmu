using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionContains : MissionPlayerCondition
    {
        private Event<PlayerEnteredRegionGameEvent>.Action _playerEnteredRegionAction;

        public MissionConditionContains(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) : base(mission, owner, prototype)
        {
            _playerEnteredRegionAction = OnPlayerEnteredRegion;
        }

        protected override long RequiredCount => CountMin;
        protected virtual long CountMin => 0;
        protected virtual long CountMax => 0;
        protected virtual bool Contains() => true;

        public override bool IsCompleted()
        {
            if (Contains() == false) return false;
            long count = Count;
            return count >= CountMin && count <= CountMax;
        }

        private void OnPlayerEnteredRegion(in PlayerEnteredRegionGameEvent evt)
        {
            var player = evt.Player;
            var regionRef = evt.RegionRef;
            var region = Region;
            if (region == null || regionRef != region.PrototypeDataRef || player == null || IsMissionPlayer(player) == false) return;
            if (Contains()) Reset();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            if (region.IsPrivate || Mission.MissionManager.IsPlayerMissionManager())
                region.PlayerEnteredRegionEvent.AddActionBack(_playerEnteredRegionAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            if (region.IsPrivate || Mission.MissionManager.IsPlayerMissionManager())
                region.PlayerEnteredRegionEvent.RemoveAction(_playerEnteredRegionAction);
        }
    }
}
