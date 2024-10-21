using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateScoringEventTimerEnd : MetaState
    {
        private MetaStateScoringEventTimerEndPrototype _proto;

        public MetaStateScoringEventTimerEnd(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateScoringEventTimerEndPrototype;
        }
    }
}

