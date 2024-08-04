using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMissionFailed : MissionPlayerCondition
    {
        protected MissionConditionMissionFailedPrototype Proto => Prototype as MissionConditionMissionFailedPrototype;
        protected override PrototypeId MissionProtoRef => Proto.MissionPrototype;
        protected override long Count => Proto.Count;
        public Action<OpenMissionFailedGameEvent> OpenMissionFailedAction { get; private set; }
        public Action<PlayerFailedMissionGameEvent> PlayerFailedMissionAction { get; private set; }
        public Action<AvatarEnteredRegionGameEvent> AvatarEnteredRegionAction { get; private set; }

        public MissionConditionMissionFailed(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            OpenMissionFailedAction = OnOpenMissionFailed;
            PlayerFailedMissionAction = OnPlayerFailedMission;
            AvatarEnteredRegionAction = OnAvatarEnteredRegion;
        }

        public override bool OnReset()
        {
            bool completed = EvaluateOnReset() && GetCompletion();
            SetCompletion(completed);
            return true;
        }

        protected override bool GetCompletion()
        {
            if (Proto == null) return false;
            Mission mission = GetMission();
            if (mission == null) return false;
            return mission.State == MissionState.Failed;
        }

        public override bool EvaluateOnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            if (proto.Count != 1) return false;
            if (proto.MissionPrototype == PrototypeId.Invalid) return false;
            if (proto.WithinRegions.HasValue()) return false;
            if (GameDatabase.GetPrototype<MissionPrototype>(proto.MissionPrototype) is OpenMissionPrototype) return false;

            return proto.EvaluateOnReset;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            var proto = Proto;
            if (proto == null) return;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(proto.MissionPrototype);
            if (missionProto == null || missionProto is OpenMissionPrototype)
                region.OpenMissionFailedEvent.AddActionBack(OpenMissionFailedAction);
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerFailedMissionEvent.AddActionBack(PlayerFailedMissionAction);

            if (proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.AddActionBack(AvatarEnteredRegionAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            var proto = Proto;
            if (proto == null) return;

            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(proto.MissionPrototype);
            if (missionProto == null || missionProto is OpenMissionPrototype)
                region.OpenMissionFailedEvent.RemoveAction(OpenMissionFailedAction);
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerFailedMissionEvent.RemoveAction(PlayerFailedMissionAction);

            if (proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(AvatarEnteredRegionAction);
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
