using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionContains : MissionPlayerCondition
    {
        public MissionConditionContains(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) : base(mission, owner, prototype)
        {
        }
    }
}
