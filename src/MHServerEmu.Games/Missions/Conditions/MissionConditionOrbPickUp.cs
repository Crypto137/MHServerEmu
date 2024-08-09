using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionOrbPickUp : MissionPlayerCondition
    {
        protected MissionConditionOrbPickUpPrototype Proto => Prototype as MissionConditionOrbPickUpPrototype;
        protected override long RequiredCount => Proto.Count;

        public MissionConditionOrbPickUp(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
