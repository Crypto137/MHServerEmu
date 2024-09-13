using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateShutdown : MetaState
    {
	    private MetaStateShutdownPrototype _proto;
		
        public MetaStateShutdown(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateShutdownPrototype;
        }

        public override void OnRemovedPlayer(Player player)
        {
            // TODO
        }
    }
}
