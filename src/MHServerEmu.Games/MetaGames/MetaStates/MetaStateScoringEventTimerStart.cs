using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateScoringEventTimerStart : MetaState
    {
	    private MetaStateScoringEventTimerStartPrototype _proto;
		
        public MetaStateScoringEventTimerStart(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateScoringEventTimerStartPrototype;
        }
    }
}
