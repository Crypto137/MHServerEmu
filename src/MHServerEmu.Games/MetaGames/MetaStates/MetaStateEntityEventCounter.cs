using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateEntityEventCounter : MetaState
    {
	    private MetaStateEntityEventCounterPrototype _proto;
		
        public MetaStateEntityEventCounter(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateEntityEventCounterPrototype;
        }
    }
}
