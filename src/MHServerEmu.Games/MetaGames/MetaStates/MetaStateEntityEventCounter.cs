using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateEntityEventCounter : MetaState
    {
	    private MetaStateEntityEventCounterPrototype _proto;
		
        public MetaStateEntityEventCounter(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateEntityEventCounterPrototype;
        }
    }
}
