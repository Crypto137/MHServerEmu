using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionPowerPointsRemaining : MissionPlayerCondition
    {
        public MissionConditionPowerPointsRemaining(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // NotInGame TimesPowerPointController
        }
    }
}
