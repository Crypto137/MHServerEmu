using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCohort : MissionPlayerCondition
    {
        protected MissionConditionCohortPrototype Proto => Prototype as MissionConditionCohortPrototype;
        public MissionConditionCohort(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // DevelopmentOnly
        }
    }
}
