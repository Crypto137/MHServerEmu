using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMissionComplete : MissionPlayerCondition
    {
        private MissionConditionMissionCompletePrototype _proto;
        protected override PrototypeId MissionProtoRef => _proto.MissionPrototype;
        protected override long RequiredCount => _proto.Count;

        private Action<OpenMissionCompleteGameEvent> _openMissionCompleteAction;
        private Action<PlayerCompletedMissionGameEvent> _playerCompletedMissionAction;
        private Action<AvatarEnteredRegionGameEvent> _avatarEnteredRegionAction;

        public MissionConditionMissionComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH00RaftTutorial
            _proto = prototype as MissionConditionMissionCompletePrototype;
            _openMissionCompleteAction = OnOpenMissionComplete;
            _playerCompletedMissionAction = OnPlayerCompletedMission;
            _avatarEnteredRegionAction = OnAvatarEnteredRegion;
        }

        public override bool OnReset()
        {
            bool completed = EvaluateOnReset() && GetCompletion();
            SetCompletion(completed);
            return true;
        }

        protected override bool GetCompletion()
        {
            Mission mission = GetMission();
            if (mission == null) return false;
            return mission.State == MissionState.Completed;
        }

        public override bool EvaluateOnReset()
        {
            if (_proto.Count != 1) return false;
            if (_proto.MissionPrototype == PrototypeId.Invalid) return false;
            if (_proto.WithinRegions.HasValue()) return false;
            if (_proto.WithinAreas.HasValue()) return false;
            if (GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype) is OpenMissionPrototype) return false;

            return _proto.EvaluateOnReset;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype);
            if (missionProto == null || missionProto is OpenMissionPrototype)
                region.OpenMissionCompleteEvent.AddActionBack(_openMissionCompleteAction); 
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerCompletedMissionEvent.AddActionBack(_playerCompletedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.AddActionBack(_avatarEnteredRegionAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype);
            if (missionProto == null || missionProto is OpenMissionPrototype)
                region.OpenMissionCompleteEvent.RemoveAction(_openMissionCompleteAction);
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerCompletedMissionEvent.RemoveAction(_playerCompletedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(_avatarEnteredRegionAction);
        }

        private void OnOpenMissionComplete(OpenMissionCompleteGameEvent evt)
        {
            throw new NotImplementedException();
        }

        private void OnPlayerCompletedMission(PlayerCompletedMissionGameEvent evt)
        {
            throw new NotImplementedException();
        }

        private void OnAvatarEnteredRegion(AvatarEnteredRegionGameEvent evt)
        {
            throw new NotImplementedException();
        }
    }
}
