using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityInteract : MissionPlayerCondition
    {
        private MissionConditionEntityInteractPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionEntityInteract(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionEntityInteractPrototype;
        }
    }
}
