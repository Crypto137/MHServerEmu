using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateLimitPlayerDeaths : MetaState
    {
	    private MetaStateLimitPlayerDeathsPrototype _proto;
		
        public MetaStateLimitPlayerDeaths(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateLimitPlayerDeathsPrototype;
        }

        public override void OnRemovedPlayer(Player player)
        {
            // TODO _proto.FailOnAllPlayersDead
        }
    }
}
