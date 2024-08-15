using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCohort : MissionPlayerCondition
    {
        private MissionConditionCohortPrototype _proto;

        public MissionConditionCohort(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // DevelopmentOnly testCohorts
            _proto = prototype as MissionConditionCohortPrototype;
        }
    }
}
