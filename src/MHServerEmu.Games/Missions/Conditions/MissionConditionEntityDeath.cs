using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEntityDeath : MissionPlayerCondition
    {
        protected MissionConditionEntityDeathPrototype Proto => Prototype as MissionConditionEntityDeathPrototype;
        protected override long MaxCount => Proto.Count;

        public MissionConditionEntityDeath(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
