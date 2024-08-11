using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemBuy : MissionPlayerCondition
    {
        private MissionConditionItemBuyPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionItemBuy(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionItemBuyPrototype;
        }
    }
}
