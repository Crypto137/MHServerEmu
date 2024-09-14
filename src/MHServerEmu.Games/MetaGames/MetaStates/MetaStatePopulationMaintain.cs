using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStatePopulationMaintain : MetaState
    {
	    private MetaStatePopulationMaintainPrototype _proto;
		
        public MetaStatePopulationMaintain(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStatePopulationMaintainPrototype;
        }
    }
}
