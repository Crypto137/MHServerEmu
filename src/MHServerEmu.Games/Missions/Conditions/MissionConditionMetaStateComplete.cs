using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMetaStateComplete : MissionPlayerCondition
    {
        private MissionConditionMetaStateCompletePrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionMetaStateComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionMetaStateCompletePrototype;
        }
    }
}
