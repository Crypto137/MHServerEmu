using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateScoringEventTimerEnd : MetaState
    {
        private MetaStateScoringEventTimerEndPrototype _proto;

        public MetaStateScoringEventTimerEnd(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateScoringEventTimerEndPrototype;
        }
    }
}

