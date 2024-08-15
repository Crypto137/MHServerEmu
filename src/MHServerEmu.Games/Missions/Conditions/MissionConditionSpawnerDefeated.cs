using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionSpawnerDefeated : MissionPlayerCondition
    {
        private MissionConditionSpawnerDefeatedPrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionSpawnerDefeated(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CivilWarDailyCapOM04SaveDumDum
            _proto = prototype as MissionConditionSpawnerDefeatedPrototype;
        }
    }
}
