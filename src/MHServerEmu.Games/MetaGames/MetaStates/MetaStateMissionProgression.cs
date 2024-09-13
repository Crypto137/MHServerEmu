using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionProgression : MetaState
    {
	    private MetaStateMissionProgressionPrototype _proto;
		
        public MetaStateMissionProgression(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateMissionProgressionPrototype;
        }
    }
}
