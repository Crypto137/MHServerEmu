using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCellEnter : MissionPlayerCondition
    {
        private MissionConditionCellEnterPrototype _proto;
        private Action<PlayerEnteredCellGameEvent> _playerEnteredCellAction;

        public MissionConditionCellEnter(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // RaftNPEPathingIndicatorController
            _proto = prototype as MissionConditionCellEnterPrototype;
            _playerEnteredCellAction = OnPlayerEnteredCell;
        }

        public override bool OnReset()
        {
            bool cellEnter = false;
            foreach (var player in Mission.GetParticipants())
            {
                var cell = player.CurrentAvatar?.Cell;
                if (cell != null && _proto.Contains(cell.PrototypeDataRef))
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
            region.PlayerEnteredCellEvent.AddActionBack(_playerEnteredCellAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerEnteredCellEvent.RemoveAction(_playerEnteredCellAction);
        }
    }
}
