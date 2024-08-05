using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMetaStateDeathLimitHit : MissionPlayerCondition
    {
        protected MissionConditionMetaStateDeathLimitHitPrototype Proto => Prototype as MissionConditionMetaStateDeathLimitHitPrototype;
        protected override long MaxCount => Proto.Count;

        public MissionConditionMetaStateDeathLimitHit(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
