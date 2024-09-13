using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateStartTargetOverride : MetaState
    {
	    private MetaStateStartTargetOverridePrototype _proto;
		
        public MetaStateStartTargetOverride(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateStartTargetOverridePrototype;
        }
    }
}
