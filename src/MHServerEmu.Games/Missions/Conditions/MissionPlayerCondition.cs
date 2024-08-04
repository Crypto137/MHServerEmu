using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionPlayerCondition : MissionCondition
    {
        protected virtual PrototypeId MissionProtoRef => PrototypeId.Invalid;
        protected Player Player => Mission.MissionManager.Player;
        protected virtual long Count => 1;

        public MissionPlayerCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
            : base(mission, owner, prototype)
        {

        }

        protected Mission GetMission()
        {
            var missionRef = MissionProtoRef;
            if (missionRef != PrototypeId.Invalid)
            {
                var missionRegion = Mission.MissionManager.GetRegion();
                var manager = MissionManager.FindMissionManagerForMission(Player, missionRegion, missionRef);
                return manager?.FindMissionByDataRef(missionRef);
            }
            else
                return Mission;
        }

        protected virtual bool GetCompletion() => false;

        protected virtual void SetCompletion(bool completed)
        {
            if (completed) SetCompleted();
            else ResetCompleted();
        }

        protected void ResetCompleted()
        {
            throw new NotImplementedException();
        }
    }
}
