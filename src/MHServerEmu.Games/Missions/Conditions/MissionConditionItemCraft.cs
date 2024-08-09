using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemCraft : MissionPlayerCondition
    {
        protected MissionConditionItemCraftPrototype Proto => Prototype as MissionConditionItemCraftPrototype;
        protected override long RequiredCount => Proto.Count;

        public MissionConditionItemCraft(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
