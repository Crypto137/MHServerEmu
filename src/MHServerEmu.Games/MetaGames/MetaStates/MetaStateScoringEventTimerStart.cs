using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateScoringEventTimerStart : MetaState
    {
	    private MetaStateScoringEventTimerStartPrototype _proto;
		
        public MetaStateScoringEventTimerStart(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateScoringEventTimerStartPrototype;
        }
    }
}
