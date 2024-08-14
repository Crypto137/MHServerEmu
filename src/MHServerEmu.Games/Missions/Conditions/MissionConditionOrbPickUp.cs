using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionOrbPickUp : MissionPlayerCondition
    {
        private MissionConditionOrbPickUpPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionOrbPickUp(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // TimesBehaviorController
            _proto = prototype as MissionConditionOrbPickUpPrototype;
        }
    }
}
