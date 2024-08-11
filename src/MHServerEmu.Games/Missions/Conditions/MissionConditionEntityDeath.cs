using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityDeath : MissionPlayerCondition
    {
        private MissionConditionEntityDeathPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionEntityDeath(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionEntityDeathPrototype;
        }
    }
}
