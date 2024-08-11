using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemDonate : MissionPlayerCondition
    {
        private MissionConditionItemDonatePrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionItemDonate(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionItemDonatePrototype;
        }
    }
}
