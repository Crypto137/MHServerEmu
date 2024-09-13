using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateRegionPlayerAccess : MetaState
    {
	    private MetaStateRegionPlayerAccessPrototype _proto;
		
        public MetaStateRegionPlayerAccess(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateRegionPlayerAccessPrototype;
        }
    }
}
