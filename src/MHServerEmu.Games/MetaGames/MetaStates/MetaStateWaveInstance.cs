using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateWaveInstance : MetaState
    {
	    private MetaStateWaveInstancePrototype _proto;
		
        public MetaStateWaveInstance(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateWaveInstancePrototype;
        }
    }
}
