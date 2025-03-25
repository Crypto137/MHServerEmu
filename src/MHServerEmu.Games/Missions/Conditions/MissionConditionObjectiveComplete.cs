using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
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

        private Event<AvatarEnteredRegionGameEvent>.Action _avatarEnteredRegionAction;
        private Event<PlayerCompletedMissionObjectiveGameEvent>.Action _playerCompletedMissionObjectiveAction;
        private Event<MissionObjectiveUpdatedGameEvent>.Action _missionObjectiveUpdatedAction;

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

        public override bool EvaluateOnReset()
        {
            if (_proto.Count != 1) return false;
            return _proto.EvaluateOnReset;
        }

        public override bool GetCompletionCount(ref long currentCount, ref long requiredCount, bool isRequired)
        {
            if (_proto.ShowCountFromTargetObjective == false)
                return base.GetCompletionCount(ref currentCount, ref requiredCount, isRequired);

            var mission = GetMission();
            if (mission == null) return false;

            var objective = mission.GetObjectiveByObjectiveID(_proto.ObjectiveID);
            if (objective == null) return false;

            ushort objectiveCount = 0;
            ushort objectiveReqCount = 0;

            if (objective.GetCompletionCount(ref objectiveCount, ref objectiveReqCount) || isRequired)
            {
                currentCount += objectiveCount;
                requiredCount += objectiveReqCount;
            }
            return requiredCount > 0;
        }

        private bool GetCompletion(bool creditTo, Player player = null)
        {
            var mission = GetMission();
            if (mission == null) return false;

            if (creditTo)
                switch (_proto.CreditTo)
                {
                    case DistributionType.Participants:

                        if (player == null || mission.HasParticipant(player) == false) return false;
                        break;

                    case DistributionType.Contributors:

                        if (player == null || mission.GetContribution(player) <= 0.0f) return false;
                        break;
                }

            var objectiveState = MissionObjectiveState.Invalid;
            var objective = mission.GetObjectiveByObjectiveID(_proto.ObjectiveID);
            if (objective != null) objectiveState = objective.State;
            return objectiveState == MissionObjectiveState.Completed || mission.State == MissionState.Completed;
        }

        private void OnAvatarEnteredRegion(in AvatarEnteredRegionGameEvent evt)
        {
            var player = evt.Player;
            if (player == null || IsMissionPlayer(player) == false) return;
            if (GetCompletion(true, player) == false) return;

            UpdatePlayerContribution(player);
            Count++;
        }

        private void OnPlayerCompletedMissionObjective(in PlayerCompletedMissionObjectiveGameEvent evt)
        {
            var player = evt.Player;
            var missionRef = evt.MissionRef;
            var objectiveId = evt.ObjectiveId;
            bool participant = evt.Participant;
            bool contributor = evt.Contributor;

            if (player == null || IsMissionPlayer(player) == false) return;

            if (MissionProtoRef != PrototypeId.Invalid)
            {            
                if (MissionProtoRef != missionRef) return; 
            }
            else
            {
                if (Mission.PrototypeDataRef != missionRef) return;
            }

            if (_proto.ObjectiveID != objectiveId) return;

            switch (_proto.CreditTo)
            {
                case DistributionType.Participants:
                    if (participant == false) return;
                    break;

                case DistributionType.Contributors:
                    if (contributor == false) return;
                    break;
            }

            UpdatePlayerContribution(player);
            Count++;
        }

        private void OnMissionObjectiveUpdated(in MissionObjectiveUpdatedGameEvent evt)
        {
            var player = evt.Player;
            var missionRef = evt.MissionRef;
            var objectiveId = evt.ObjectiveId;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.MissionPrototype != missionRef) return;
            if (_proto.ObjectiveID != objectiveId) return;

            OnUpdate();
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
    }
}
