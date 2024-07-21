using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionCondition : IMissionConditionOwner
    {
        public Mission Mission { get; private set; }
        public IMissionConditionOwner Owner { get; private set; }
        public MissionConditionPrototype Prototype { get; private set; }

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

        public virtual bool Initialize(int conditionIndex) => true;
    }
}
