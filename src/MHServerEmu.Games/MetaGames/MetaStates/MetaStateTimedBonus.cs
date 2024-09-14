using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateTimedBonus : MetaState
    {
	    private MetaStateTimedBonusPrototype _proto;
		
        public MetaStateTimedBonus(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateTimedBonusPrototype;
        }
    }
}
