using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemBuy : MissionPlayerCondition
    {
        protected MissionConditionItemBuyPrototype Proto => Prototype as MissionConditionItemBuyPrototype;
        protected override long MaxCount => Proto.Count;

        public MissionConditionItemBuy(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
