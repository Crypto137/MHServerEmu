using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateShutdown : MetaState
    {
	    private MetaStateShutdownPrototype _proto;
		
        public MetaStateShutdown(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateShutdownPrototype;
        }

        public override void OnRemovedPlayer(Player player)
        {
            // TODO
        }
    }
}
