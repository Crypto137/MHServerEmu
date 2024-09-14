using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateCombatQueueLockout : MetaState
    {
	    private MetaStateCombatQueueLockoutPrototype _proto;
		
        public MetaStateCombatQueueLockout(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateCombatQueueLockoutPrototype;
        }
    }
}
