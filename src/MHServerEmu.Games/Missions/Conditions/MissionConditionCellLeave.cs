using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCellLeave : MissionPlayerCondition
    {
        protected MissionConditionCellLeavePrototype Proto => Prototype as MissionConditionCellLeavePrototype;
        public Action<PlayerLeftCellGameEvent> PlayerLeftCellAction { get; private set; }
        public MissionConditionCellLeave(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PlayerLeftCellAction = OnPlayerLeftCell;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            List<Entity> participants = new();
            Mission.GetParticipants(participants);

            bool cellLeave = true;
            foreach (var participant in participants)
                if (participant is Player player)
                {
                    var cell = player.CurrentAvatar?.Cell;
                    if (cell != null && proto.Contains(cell.PrototypeDataRef))
                    {
                        cellLeave = false;
                        break;
                    }
                }

            SetCompletion(cellLeave);
            return true;
        }

        private void OnPlayerLeftCell(PlayerLeftCellGameEvent evt)
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
            region.PlayerLeftCellEvent.AddActionBack(PlayerLeftCellAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerLeftCellEvent.RemoveAction(PlayerLeftCellAction);
        }
    }
}
