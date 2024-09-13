using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStatePopulationMaintain : MetaState
    {
	    private MetaStatePopulationMaintainPrototype _proto;
		
        public MetaStatePopulationMaintain(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStatePopulationMaintainPrototype;
        }
    }
}
