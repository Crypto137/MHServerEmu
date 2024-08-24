using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionScoringEventTimerStop : MissionAction
    {
        private MissionActionScoringEventTimerStopPrototype _proto;
        public MissionActionScoringEventTimerStop(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // DRMissionChallengeCosmicDoopPhase01
            _proto = prototype as MissionActionScoringEventTimerStopPrototype;
        }

        public override void Run()
        {
            Region?.ScoringEventTimerStop(_proto.Timer);
        }
    }
}
