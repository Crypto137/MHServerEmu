using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateWaveInstance : MetaState
    {
	    private MetaStateWaveInstancePrototype _proto;
		
        public MetaStateWaveInstance(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateWaveInstancePrototype;
        }
    }
}
