using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionStateListener : MetaState
    {
	    private MetaStateMissionStateListenerPrototype _proto;
		
        public MetaStateMissionStateListener(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateMissionStateListenerPrototype;
        }
    }
}
