using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemCraft : MissionPlayerCondition
    {
        private MissionConditionItemCraftPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionItemCraft(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionItemCraftPrototype;
        }
    }
}
