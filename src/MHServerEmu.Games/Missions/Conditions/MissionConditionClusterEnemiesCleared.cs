using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionClusterEnemiesCleared : MissionPlayerCondition
    {
        private MissionConditionClusterEnemiesClearedPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionClusterEnemiesCleared(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionClusterEnemiesClearedPrototype;
        }
    }
}
