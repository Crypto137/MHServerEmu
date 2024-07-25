using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionCondition : IMissionConditionOwner
    {
        public Mission Mission { get; private set; }
        public IMissionConditionOwner Owner { get; private set; }
        public MissionConditionPrototype Prototype { get; private set; }
        public Region Region { get => Mission.Region; }
        public bool EventsRegistered { get; protected set; }
        public virtual bool IsCompleted { get => false; }
        public bool IsReseting { get; private set; }

        public MissionCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
        {
            Mission = mission;
            Owner = owner;
            Prototype = prototype;
        }

        public static MissionCondition CreateCondition(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype conditionProto)
        {
            return conditionProto.AllocateCondition(mission, owner);
        }

        public bool Reset()
        {
            IsReseting = true;
            bool result = OnReset();
            IsReseting = false;
            return result;
        }

        protected virtual bool OnReset() => false;
        public virtual bool Initialize(int conditionIndex) => true;
        public virtual void RegisterEvents(Region region) { }
        public virtual void UnRegisterEvents(Region region) { }
    }
}
