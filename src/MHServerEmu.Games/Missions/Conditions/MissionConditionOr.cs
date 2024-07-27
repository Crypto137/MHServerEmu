using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionOr : MissionConditionList
    {
        public MissionConditionOr(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }

        public override bool IsCompleted()
        {
            if (Conditions.Count == 0) return false;
            foreach (var condition in Conditions)
                if (condition != null && condition.IsCompleted())
                    return true;

            return false;
        }

        public override void SetCompleted()
        {
            foreach (var condition in Conditions)
                condition?.SetCompleted();
        }
    }
}
