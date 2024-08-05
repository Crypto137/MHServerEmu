using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionPlayerCondition : MissionCondition
    {
        protected virtual PrototypeId MissionProtoRef => PrototypeId.Invalid;
        protected Player Player => Mission.MissionManager.Player;

        private long _count;
        public long Count { get => _count; set => SetCount(value); }
        protected virtual long MaxCount => 1;

        public MissionPlayerCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
            : base(mission, owner, prototype)
        {
            _count = 0;
        }

        public override bool Initialize(int conditionIndex)
        {
            ConditionIndex = conditionIndex++;
            return base.Initialize(conditionIndex);
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

        public override bool IsCompleted() => Count >= MaxCount;

        protected virtual bool GetCompletion() => false;

        protected virtual void SetCompletion(bool completed)
        {
            if (completed) SetCompleted();
            else ResetCompleted();
        }

        protected virtual void SetCount(long count)
        {
            _count = Math.Clamp(count, 0, MaxCount);
            OnUpdate();
        }

        protected void ResetCompleted()
        {
            SetCount(0);
        }

        public override bool OnReset()
        {
            ResetCompleted();
            return true;
        }
    }
}
