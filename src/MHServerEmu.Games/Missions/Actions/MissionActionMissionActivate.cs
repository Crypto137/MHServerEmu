using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionMissionActivate : MissionAction
    {
        private MissionActionMissionActivatePrototype _proto;

        public MissionActionMissionActivate(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH03M1MeetInMadripoor
            _proto = prototype as MissionActionMissionActivatePrototype;
        }

        public override void Run()
        {
            var missionManager = MissionManager;
            if (missionManager == null) return;
            var missionProtoRef = _proto.MissionPrototype;

            if (missionProtoRef == PrototypeId.Invalid && _proto.WeightedMissionPickList.HasValue())
            {                
                var region = Region; 
                int seed = (region != null) ? region.RandomSeed : 0;
                GRandom random = new(seed);
                Picker<PrototypeId> picker = new(random);

                foreach (var weightProto in _proto.WeightedMissionPickList)
                {
                    var missionRef = weightProto.Mission;
                    var weightMissionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                    if (weightMissionProto == null || weightMissionProto.IsLiveTuningEnabled() == false) continue;

                    var weightMission = missionManager.FindMissionByDataRef(missionRef);
                    if (weightMission != null && weightMission.State == MissionState.Active) 
                    {
                        if (weightMission.IsSuspended) continue;
                        else return;
                    }

                    picker.Add(missionRef, weightProto.Weight);
                }

                if (picker.Empty()) return;
                missionProtoRef = picker.Pick();
            }

            if (missionProtoRef == PrototypeId.Invalid) return;
            if (missionProtoRef != MissionRef) return;

            var mission = missionManager.FindMissionByDataRef(missionProtoRef);
            if (mission != null)
            {
                var missionState = mission.State;
                if (missionState == MissionState.Completed || missionState == MissionState.Failed)
                    mission.RestartMission();
            }

            missionManager.ActivateMission(missionProtoRef);
        }

        public override bool RunOnStart()
        {
            return _proto.MissionPrototype != PrototypeId.Invalid && _proto.WeightedMissionPickList.HasValue();
        } 
    }
}
