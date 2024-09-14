using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionRestart : MetaState
    {
	    private MetaStateMissionRestartPrototype _proto;
		
        public MetaStateMissionRestart(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionRestartPrototype;
        }
    }
}
