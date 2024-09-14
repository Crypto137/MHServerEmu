using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateRegionPlayerAccess : MetaState
    {
	    private MetaStateRegionPlayerAccessPrototype _proto;
		
        public MetaStateRegionPlayerAccess(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateRegionPlayerAccessPrototype;
        }
    }
}
