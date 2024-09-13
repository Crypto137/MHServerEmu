using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateEntityModifier : MetaState
    {
	    private MetaStateEntityModifierPrototype _proto;
		
        public MetaStateEntityModifier(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateEntityModifierPrototype;
        }
    }
}
