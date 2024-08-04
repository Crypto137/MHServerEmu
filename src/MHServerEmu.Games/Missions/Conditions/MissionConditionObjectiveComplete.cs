using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionObjectiveComplete : MissionPlayerCondition
    {
        protected MissionConditionObjectiveCompletePrototype Proto => Prototype as MissionConditionObjectiveCompletePrototype;
        protected override PrototypeId MissionProtoRef => Proto.MissionPrototype;
        protected override long Count => Proto.Count;
        public Action<AvatarEnteredRegionGameEvent> AvatarEnteredRegionAction { get; private set; }
        public Action<PlayerCompletedMissionObjectiveGameEvent> PlayerCompletedMissionObjectiveAction { get; private set; }
        public Action<MissionObjectiveUpdatedGameEvent> MissionObjectiveUpdatedAction { get; private set; }

        public MissionConditionObjectiveComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            AvatarEnteredRegionAction = OnAvatarEnteredRegion;
            PlayerCompletedMissionObjectiveAction = OnPlayerCompletedMissionObjective;
            MissionObjectiveUpdatedAction = OnMissionObjectiveUpdated;
        }

        public override bool OnReset()
        {
            bool completed = EvaluateOnReset() && GetCompletion(false);
            SetCompletion(completed);
            return true;
        }

        private bool GetCompletion(bool creditTo)
        {
            var proto = Proto;
            if (proto == null) return false;
            Mission mission = GetMission();
            if (mission == null) return false;

            if (creditTo)
            {
                var player = Player;
                switch (proto.CreditTo)
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
            var objective = mission.GetObjectiveByObjectiveID(proto.ObjectiveID);
            if (objective != null) objectiveState = objective.State;
            return objectiveState == MissionObjectiveState.Completed || mission.State == MissionState.Completed;
        }

        public override bool EvaluateOnReset()
        {
            if (Prototype is not MissionConditionObjectiveCompletePrototype proto) return false;
            if (proto.Count != 1) return false;
            return proto.EvaluateOnReset;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            var proto = Proto;
            if (proto == null) return;

            region.PlayerCompletedMissionObjectiveEvent.AddActionBack(PlayerCompletedMissionObjectiveAction);
            if (proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.AddActionBack(AvatarEnteredRegionAction);
            if (proto.ShowCountFromTargetObjective)
                region.MissionObjectiveUpdatedEvent.AddActionBack(MissionObjectiveUpdatedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            var proto = Proto;
            if (proto == null) return;

            region.PlayerCompletedMissionObjectiveEvent.RemoveAction(PlayerCompletedMissionObjectiveAction);
            if (proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(AvatarEnteredRegionAction);
            if (proto.ShowCountFromTargetObjective)
                region.MissionObjectiveUpdatedEvent.RemoveAction(MissionObjectiveUpdatedAction);
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
