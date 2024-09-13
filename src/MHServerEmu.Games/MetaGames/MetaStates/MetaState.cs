using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaState
    {
        protected MetaGame MetaGame { get; }
        public PrototypeId PrototypeDataRef { get; }
        public MetaStatePrototype Prototype {  get; } 

        public MetaState(MetaGame metaGame, PrototypeId stateRef)
        {
            MetaGame = metaGame;
            PrototypeDataRef = stateRef;
            Prototype = GameDatabase.GetPrototype<MetaStatePrototype>(stateRef);
        }

        public virtual void OnRemovedPlayer(Player player) { }
    }
}
