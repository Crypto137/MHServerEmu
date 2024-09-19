using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaState
    {
        protected MetaGame MetaGame { get; }
        protected Game Game { get; }
        public Region Region { get; }
        protected EventScheduler GameEventScheduler { get => Game.GameEventScheduler; }        
        public PrototypeId PrototypeDataRef { get; }
        public MetaStatePrototype Prototype {  get; }

        protected EventGroup _pendingEvents = new();

        public MetaState(MetaGame metaGame, MetaStatePrototype prototype)
        {
            MetaGame = metaGame;
            Game = metaGame.Game;
            Region = metaGame.Region;
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

        protected void PlayerMetaStateComplete()
        {
            // TODO achievement
        }

        public virtual void OnApply() { }

        public virtual void OnRemove() 
        { 
            GameEventScheduler?.CancelAllEvents(_pendingEvents); 
        }

        public virtual void OnRemovedState(PrototypeId removedStateRef) { }
        public virtual void OnAddPlayer(Player player) { }
        public virtual void OnRemovedPlayer(Player player) { }
    }
}
