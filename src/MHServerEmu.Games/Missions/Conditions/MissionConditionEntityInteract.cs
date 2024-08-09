using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityInteract : MissionPlayerCondition
    {
        protected MissionConditionEntityInteractPrototype Proto => Prototype as MissionConditionEntityInteractPrototype;
        protected override long RequiredCount => Proto.Count;

        public MissionConditionEntityInteract(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
