using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMetaStateDeathLimitHit : MissionPlayerCondition
    {
        private MissionConditionMetaStateDeathLimitHitPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionMetaStateDeathLimitHit(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionMetaStateDeathLimitHitPrototype;
        }
    }
}
