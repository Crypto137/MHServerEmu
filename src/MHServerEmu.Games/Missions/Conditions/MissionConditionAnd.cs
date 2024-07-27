using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAnd : MissionConditionList
    {
        public MissionConditionAnd(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }

        public override bool IsCompleted() 
        {
            foreach (var condition in Conditions)
                if (condition != null && condition.IsCompleted() == false)
                    return false;
            
            return true;
        }
    }
}
