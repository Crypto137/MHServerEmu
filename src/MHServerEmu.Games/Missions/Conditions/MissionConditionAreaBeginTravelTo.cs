using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaBeginTravelTo : MissionPlayerCondition
    {
        protected MissionConditionAreaBeginTravelToPrototype Proto => Prototype as MissionConditionAreaBeginTravelToPrototype;
        public Action<PlayerBeginTravelToAreaGameEvent> PlayerBeginTravelToAreaAction { get; private set; }

        public MissionConditionAreaBeginTravelTo(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PlayerBeginTravelToAreaAction = OnPlayerBeginTravelToArea;
        }

        public override bool OnReset()
        {
            SetCompletion(false);
            return true;
        }

        private void OnPlayerBeginTravelToArea(PlayerBeginTravelToAreaGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var areaRef = evt.AreaRef;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (proto.AreaPrototype != areaRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerBeginTravelToAreaEvent.AddActionBack(PlayerBeginTravelToAreaAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerBeginTravelToAreaEvent.RemoveAction(PlayerBeginTravelToAreaAction);
        }
    }
}
