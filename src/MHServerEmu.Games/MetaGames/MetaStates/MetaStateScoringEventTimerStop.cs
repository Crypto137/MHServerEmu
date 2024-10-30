using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateScoringEventTimerStop : MetaState
    {
        private MetaStateScoringEventTimerStopPrototype _proto;

        public MetaStateScoringEventTimerStop(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateScoringEventTimerStopPrototype;
        }

        public override void OnApply()
        {
            Region?.ScoringEventTimerStop(_proto.Timer);
        }
    }
}

