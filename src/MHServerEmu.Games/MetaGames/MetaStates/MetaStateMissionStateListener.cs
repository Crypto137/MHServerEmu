using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionStateListener : MetaState
    {
	    private MetaStateMissionStateListenerPrototype _proto;
        private Event<OpenMissionCompleteGameEvent>.Action _openMissionCompleteAction;
        private Event<OpenMissionFailedGameEvent>.Action _openMissionFailedAction;

        public MetaStateMissionStateListener(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionStateListenerPrototype;

            _openMissionCompleteAction = OnOpenMissionComplete;
            _openMissionFailedAction = OnOpenMissionFailed;
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.CompleteMissions.HasValue())
                region.OpenMissionCompleteEvent.AddActionBack(_openMissionCompleteAction);

            if (_proto.FailMissions.HasValue())
                region.OpenMissionFailedEvent.AddActionBack(_openMissionFailedAction);
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            if (_proto.CompleteMissions.HasValue())
                region.OpenMissionCompleteEvent.RemoveAction(_openMissionCompleteAction);

            if (_proto.CompleteMissions.HasValue())
                region.OpenMissionFailedEvent.RemoveAction(_openMissionFailedAction);

            base.OnRemove();
        }

        private void OnOpenMissionComplete(in OpenMissionCompleteGameEvent evt)
        {
            var missionRef = evt.MissionRef;
            if (_proto.CompleteMissions.Contains(missionRef))
                MetaGame.ScheduleActivateGameMode(_proto.CompleteMode);
        }

        private void OnOpenMissionFailed(in OpenMissionFailedGameEvent evt)
        {
            var missionRef = evt.MissionRef;
            if (_proto.FailMissions.Contains(missionRef))
                MetaGame.ScheduleActivateGameMode(_proto.FailMode);
        }
    }
}
