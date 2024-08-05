using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionSpawnerDefeated : MissionPlayerCondition
    {
        protected MissionConditionSpawnerDefeatedPrototype Proto => Prototype as MissionConditionSpawnerDefeatedPrototype;
        protected override long MaxCount => Proto.Count;

        public MissionConditionSpawnerDefeated(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
