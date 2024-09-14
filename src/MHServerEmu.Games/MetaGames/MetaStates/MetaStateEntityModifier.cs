using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateEntityModifier : MetaState
    {
	    private MetaStateEntityModifierPrototype _proto;
		
        public MetaStateEntityModifier(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateEntityModifierPrototype;
        }
    }
}
