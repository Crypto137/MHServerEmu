using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarUsedPower : MissionPlayerCondition
    {
        private MissionConditionAvatarUsedPowerPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionAvatarUsedPower(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionAvatarUsedPowerPrototype;
        }
    }
}
