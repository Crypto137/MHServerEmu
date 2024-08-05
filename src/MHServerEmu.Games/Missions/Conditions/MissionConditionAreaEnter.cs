using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaEnter : MissionPlayerCondition
    {
        protected MissionConditionAreaEnterPrototype Proto => Prototype as MissionConditionAreaEnterPrototype;
        public Action<PlayerEnteredAreaGameEvent> PlayerEnteredAreaAction { get; private set; }

        public MissionConditionAreaEnter(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PlayerEnteredAreaAction = OnPlayerEnteredArea;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            List<Entity> participants = new();
            Mission.GetParticipants(participants);

            bool areaEnter = false;
            foreach (var participant in participants)
                if (participant is Player player)
                {
                    var area = player.CurrentAvatar?.Area;
                    if (area != null && area.PrototypeDataRef == proto.AreaPrototype)
                    {
                        areaEnter = true;
                        break;
                    }
                }

            SetCompletion(areaEnter);
            return true;
        }

        private void OnPlayerEnteredArea(PlayerEnteredAreaGameEvent evt)
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
            region.PlayerEnteredAreaEvent.AddActionBack(PlayerEnteredAreaAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerEnteredAreaEvent.RemoveAction(PlayerEnteredAreaAction);
        }
    }
}
