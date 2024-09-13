using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateTimedBonus : MetaState
    {
	    private MetaStateTimedBonusPrototype _proto;
		
        public MetaStateTimedBonus(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateTimedBonusPrototype;
        }
    }
}
