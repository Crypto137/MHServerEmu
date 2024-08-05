using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMetaStateComplete : MissionPlayerCondition
    {
        protected MissionConditionMetaStateCompletePrototype Proto => Prototype as MissionConditionMetaStateCompletePrototype;
        protected override long MaxCount => Proto.Count;

        public MissionConditionMetaStateComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
