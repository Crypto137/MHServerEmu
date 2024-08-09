using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCellEnter : MissionPlayerCondition
    {
        protected MissionConditionCellEnterPrototype Proto => Prototype as MissionConditionCellEnterPrototype;
        public Action<PlayerEnteredCellGameEvent> PlayerEnteredCellAction { get; private set; }
        public MissionConditionCellEnter(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PlayerEnteredCellAction = OnPlayerEnteredCell;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            List<Entity> participants = new();
            Mission.GetParticipants(participants);

            bool cellEnter = false;
            foreach (var participant in participants)
                if (participant is Player player)
                {
                    var cell = player.CurrentAvatar?.Cell;
                    if (cell != null && proto.Contains(cell.PrototypeDataRef))
                    {
                        cellEnter = true;
                        break;
                    }
                }

            SetCompletion(cellEnter);
            return true;
        }

        private void OnPlayerEnteredCell(PlayerEnteredCellGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var cellRef = evt.CellRef;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (proto.Contains(cellRef) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerEnteredCellEvent.AddActionBack(PlayerEnteredCellAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerEnteredCellEvent.RemoveAction(PlayerEnteredCellAction);
        }
    }
}
