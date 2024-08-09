using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemDonate : MissionPlayerCondition
    {
        protected MissionConditionItemDonatePrototype Proto => Prototype as MissionConditionItemDonatePrototype;
        protected override long RequiredCount => Proto.Count;

        public MissionConditionItemDonate(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
