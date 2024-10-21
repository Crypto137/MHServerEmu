using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionPublicEventIsActive : MissionPlayerCondition
    {
        public MissionConditionPublicEventIsActive(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // Not Used
        }
    }
}
