using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionStateListener : MetaState
    {
	    private MetaStateMissionStateListenerPrototype _proto;
		
        public MetaStateMissionStateListener(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionStateListenerPrototype;
        }
    }
}
