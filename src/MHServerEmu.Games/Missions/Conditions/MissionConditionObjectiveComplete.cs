using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionObjectiveComplete : MissionPlayerCondition
    {
        private MissionConditionObjectiveCompletePrototype _proto;
        protected override PrototypeId MissionProtoRef => _proto.MissionPrototype;
        protected override long RequiredCount => _proto.Count;

        private Action<AvatarEnteredRegionGameEvent> _avatarEnteredRegionAction;
        private Action<PlayerCompletedMissionObjectiveGameEvent> _playerCompletedMissionObjectiveAction;
        private Action<MissionObjectiveUpdatedGameEvent> _missionObjectiveUpdatedAction;

        public MissionConditionObjectiveComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH00NPETrainingRoom
            _proto = prototype as MissionConditionObjectiveCompletePrototype;
            _avatarEnteredRegionAction = OnAvatarEnteredRegion;
            _playerCompletedMissionObjectiveAction = OnPlayerCompletedMissionObjective;
            _missionObjectiveUpdatedAction = OnMissionObjectiveUpdated;
        }

        public override bool OnReset()
        {
            bool completed = EvaluateOnReset() && GetCompletion(false);
            SetCompletion(completed);
            return true;
        }

        private bool GetCompletion(bool creditTo)
        {
            Mission mission = GetMission();
            if (mission == null) return false;

            if (creditTo)
            {
                var player = Player;
                switch (_proto.CreditTo)
                {
                    case DistributionType.Participants:

                        if (player == null || mission.HasParticipant(player) == false) return false;
                        break;

                    case DistributionType.Contributors:

                        if (player == null || mission.GetContribution(player) <= 0.0f) return false;
                        break;
                }
            }

            var objectiveState = MissionObjectiveState.Invalid;
            var objective = mission.GetObjectiveByObjectiveID(_proto.ObjectiveID);
            if (objective != null) objectiveState = objective.State;
            return objectiveState == MissionObjectiveState.Completed || mission.State == MissionState.Completed;
        }

        public override bool EvaluateOnReset()
        {
            if (_proto.Count != 1) return false;
            return _proto.EvaluateOnReset;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;

            region.PlayerCompletedMissionObjectiveEvent.AddActionBack(_playerCompletedMissionObjectiveAction);
            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.AddActionBack(_avatarEnteredRegionAction);
            if (_proto.ShowCountFromTargetObjective)
                region.MissionObjectiveUpdatedEvent.AddActionBack(_missionObjectiveUpdatedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;

            region.PlayerCompletedMissionObjectiveEvent.RemoveAction(_playerCompletedMissionObjectiveAction);
            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(_avatarEnteredRegionAction);
            if (_proto.ShowCountFromTargetObjective)
                region.MissionObjectiveUpdatedEvent.RemoveAction(_missionObjectiveUpdatedAction);
        }

        private void OnAvatarEnteredRegion(AvatarEnteredRegionGameEvent evt)
        {
            throw new NotImplementedException();
        }

        private void OnPlayerCompletedMissionObjective(PlayerCompletedMissionObjectiveGameEvent evt)
        {
            throw new NotImplementedException();
        }

        private void OnMissionObjectiveUpdated(MissionObjectiveUpdatedGameEvent evt)
        {
            throw new NotImplementedException();
        }
    }
}
