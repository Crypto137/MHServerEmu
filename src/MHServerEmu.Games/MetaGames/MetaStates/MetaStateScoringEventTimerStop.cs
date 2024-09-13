using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateScoringEventTimerStop : MetaState
    {
        private MetaStateScoringEventTimerStopPrototype _proto;

        public MetaStateScoringEventTimerStop(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateScoringEventTimerStopPrototype;
        }
    }
}

