using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionClusterEnemiesCleared : MissionPlayerCondition
    {
        protected MissionConditionClusterEnemiesClearedPrototype Proto => Prototype as MissionConditionClusterEnemiesClearedPrototype;
        protected override long RequiredCount => Proto.Count;

        public MissionConditionClusterEnemiesCleared(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
    }
}
