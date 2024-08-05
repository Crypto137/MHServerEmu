using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMissionComplete : MissionPlayerCondition
    {
        protected MissionConditionMissionCompletePrototype Proto => Prototype as MissionConditionMissionCompletePrototype;
        protected override PrototypeId MissionProtoRef => Proto.MissionPrototype;
        protected override long MaxCount => Proto.Count;
        public Action<OpenMissionCompleteGameEvent> OpenMissionCompleteAction { get; private set; }
        public Action<PlayerCompletedMissionGameEvent> PlayerCompletedMissionAction { get; private set; }
        public Action<AvatarEnteredRegionGameEvent> AvatarEnteredRegionAction { get; private set; }

        public MissionConditionMissionComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            OpenMissionCompleteAction = OnOpenMissionComplete;
            PlayerCompletedMissionAction = OnPlayerCompletedMission;
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
            return mission.State == MissionState.Completed;
        }

        public override bool EvaluateOnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            if (proto.Count != 1) return false;
            if (proto.MissionPrototype == PrototypeId.Invalid) return false;
            if (proto.WithinRegions.HasValue()) return false;
            if (proto.WithinAreas.HasValue()) return false;
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
                region.OpenMissionCompleteEvent.AddActionBack(OpenMissionCompleteAction); 
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerCompletedMissionEvent.AddActionBack(PlayerCompletedMissionAction);

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
                region.OpenMissionCompleteEvent.RemoveAction(OpenMissionCompleteAction);
            if (missionProto == null || missionProto is not OpenMissionPrototype)
                region.PlayerCompletedMissionEvent.RemoveAction(PlayerCompletedMissionAction);

            if (proto.EvaluateOnRegionEnter)
                region.AvatarEnteredRegionEvent.RemoveAction(AvatarEnteredRegionAction);
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
