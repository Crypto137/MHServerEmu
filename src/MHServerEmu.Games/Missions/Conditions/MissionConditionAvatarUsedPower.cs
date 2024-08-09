using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarUsedPower : MissionPlayerCondition
    {
        protected MissionConditionAvatarUsedPowerPrototype Proto => Prototype as MissionConditionAvatarUsedPowerPrototype;
        protected override long RequiredCount => Proto.Count;

        public MissionConditionAvatarUsedPower(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
