using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMissionFailed : MissionPlayerCondition
    {
        private MissionConditionMissionFailedPrototype _proto;
        protected override PrototypeId MissionProtoRef => _proto.MissionPrototype;
        protected override long RequiredCount => _proto.Count;

        private Action<OpenMissionFailedGameEvent> _openMissionFailedAction;
        private Action<PlayerFailedMissionGameEvent> _playerFailedMissionAction;
        private Action<AvatarEnteredRegionGameEvent> _avatarEnteredRegionAction;

        public MissionConditionMissionFailed(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionMissionFailedPrototype;
            _openMissionFailedAction = OnOpenMissionFailed;
            _playerFailedMissionAction = OnPlayerFailedMission;
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
            return mission.State == MissionState.Failed;
        }

        public override bool EvaluateOnReset()
        {
            if (_proto.Count != 1) return false;
            if (_proto.MissionPrototype == PrototypeId.Invalid) return false;
            if (_proto.WithinRegions.HasValue()) return false;
            if (GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype) is OpenMissionPrototype) return false;

            return _proto.EvaluateOnReset;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype);
            if (missionProto == null || missionProto is OpenMissionPrototype)
                region.OpenMissionFailedEvent.AddActionBack(_openMissionFailedAction);
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerFailedMissionEvent.AddActionBack(_playerFailedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.AddActionBack(_avatarEnteredRegionAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(_proto.MissionPrototype);
            if (missionProto == null || missionProto is OpenMissionPrototype)
                region.OpenMissionFailedEvent.RemoveAction(_openMissionFailedAction);
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerFailedMissionEvent.RemoveAction(_playerFailedMissionAction);

            if (_proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(_avatarEnteredRegionAction);
        }

        private void OnOpenMissionFailed(OpenMissionFailedGameEvent evt)
        {
            throw new NotImplementedException();
        }

        private void OnPlayerFailedMission(PlayerFailedMissionGameEvent evt)
        {
            throw new NotImplementedException();
        }

        private void OnAvatarEnteredRegion(AvatarEnteredRegionGameEvent evt)
        {
            throw new NotImplementedException();
        }
    }
}
