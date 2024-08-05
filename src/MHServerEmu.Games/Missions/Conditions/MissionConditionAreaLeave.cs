using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAreaLeave : MissionPlayerCondition
    {
        protected MissionConditionAreaLeavePrototype Proto => Prototype as MissionConditionAreaLeavePrototype;
        public Action<PlayerLeftAreaGameEvent> PlayerLeftAreaAction { get; private set; }

        public MissionConditionAreaLeave(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PlayerLeftAreaAction = OnPlayerLeftArea;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            List<Entity> participants = new();
            Mission.GetParticipants(participants);

            bool areaLeave = true;
            foreach (var participant in participants)
                if (participant is Player player)
                {
                    var area = player.CurrentAvatar?.Area;
                    if (area != null && area.PrototypeDataRef == proto.AreaPrototype)
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
            region.PlayerLeftAreaEvent.AddActionBack(PlayerLeftAreaAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerLeftAreaEvent.RemoveAction(PlayerLeftAreaAction);
        }
    }
}
