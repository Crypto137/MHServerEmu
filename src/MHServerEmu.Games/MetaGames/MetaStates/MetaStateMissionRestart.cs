using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionRestart : MetaState
    {
	    private MetaStateMissionRestartPrototype _proto;
		
        public MetaStateMissionRestart(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateMissionRestartPrototype;
        }
    }
}
