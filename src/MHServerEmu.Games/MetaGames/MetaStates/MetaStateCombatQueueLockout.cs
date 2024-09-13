using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateCombatQueueLockout : MetaState
    {
	    private MetaStateCombatQueueLockoutPrototype _proto;
		
        public MetaStateCombatQueueLockout(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateCombatQueueLockoutPrototype;
        }
    }
}
