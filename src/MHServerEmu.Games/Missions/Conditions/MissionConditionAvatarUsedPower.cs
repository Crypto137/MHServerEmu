using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarUsedPower : MissionCondition
    {
        public MissionConditionAvatarUsedPower(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}