using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionScoringEventTimerEnd : MissionAction
    {
        private MissionActionScoringEventTimerEndPrototype _proto;
        public MissionActionScoringEventTimerEnd(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // Not Used
            _proto = prototype as MissionActionScoringEventTimerEndPrototype;
        }

        public override void Run()
        {
            Region?.ScoringEventTimerEnd(_proto.Timer);
        }
    }
}
