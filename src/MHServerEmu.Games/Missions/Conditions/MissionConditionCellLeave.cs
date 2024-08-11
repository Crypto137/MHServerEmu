using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCellLeave : MissionPlayerCondition
    {
        private MissionConditionCellLeavePrototype _proto;
        private Action<PlayerLeftCellGameEvent> _playerLeftCellAction;

        public MissionConditionCellLeave(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionCellLeavePrototype;
            _playerLeftCellAction = OnPlayerLeftCell;
        }

        public override bool OnReset()
        {
            bool cellLeave = true;
            foreach (var player in Mission.GetParticipants())
            {
                var cell = player.CurrentAvatar?.Cell;
                if (cell != null && _proto.Contains(cell.PrototypeDataRef))
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
            var player = evt.Player;
            var cellRef = evt.CellRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.Contains(cellRef) == false) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerLeftCellEvent.AddActionBack(_playerLeftCellAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerLeftCellEvent.RemoveAction(_playerLeftCellAction);
        }
    }
}
