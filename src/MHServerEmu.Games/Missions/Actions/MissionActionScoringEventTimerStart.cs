using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionScoringEventTimerStart : MissionAction
    {
        private MissionActionScoringEventTimerStartPrototype _proto;
        public MissionActionScoringEventTimerStart(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // DRMissionChallengeCosmicDoopPhase01
            _proto = prototype as MissionActionScoringEventTimerStartPrototype;
        }

        public override void Run()
        {
            Region?.ScoringEventTimerStart(_proto.Timer);
        }
    }
}
