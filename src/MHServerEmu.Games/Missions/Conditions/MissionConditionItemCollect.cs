using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionItemCollect : MissionPlayerCondition
    {
        protected MissionConditionItemCollectPrototype Proto => Prototype as MissionConditionItemCollectPrototype;
        protected override long RequiredCount => Proto.Count;

        public MissionConditionItemCollect(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
