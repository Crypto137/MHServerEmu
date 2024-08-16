using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionLogicTrue : MissionPlayerCondition
    {
        public MissionConditionLogicTrue(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }

        public override bool OnReset()
        {
            SetCompleted();
            return true;
        }
    }
}
