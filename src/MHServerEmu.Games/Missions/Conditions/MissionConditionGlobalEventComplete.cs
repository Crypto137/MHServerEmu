using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionGlobalEventComplete : MissionPlayerCondition
    {
        public MissionConditionGlobalEventComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH09Main1AFrostyReception NotInGame
        }
    }
}
