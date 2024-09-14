using MHServerEmu.Core.Extensions;
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

        public MetaState(MetaGame metaGame, MetaStatePrototype prototype)
        {
            MetaGame = metaGame;
            Prototype = prototype;
            PrototypeDataRef = prototype.DataRef;
        }

        public static MetaState CreateMetaState(MetaGame metaGame, PrototypeId stateRef)
        {
            var stateProto = GameDatabase.GetPrototype<MetaStatePrototype>(stateRef);
            return stateProto.AllocateState(metaGame);
        }

        public bool HasGroup(AssetId group)
        {
            if (group == AssetId.Invalid || Prototype.Groups.IsNullOrEmpty()) return false;
            return Prototype.Groups.Any(stateGroup => stateGroup == group);
        }

        public virtual void OnApply() { }
        public virtual void OnRemove() { }
        public virtual void OnRemovedState(PrototypeId removedStateRef) { }
        public virtual void OnRemovedPlayer(Player player) { }
    }
}
