using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaState
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
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
            if (MetaGame.Debug) Logger.Debug($"CreateMetaState {GameDatabase.GetFormattedPrototypeName(stateRef)} {stateProto.GetType().Name}");
            return stateProto.AllocateState(metaGame);
        }

        public bool HasGroup(AssetId group)
        {
            if (group == AssetId.Invalid || Prototype.Groups.IsNullOrEmpty()) return false;
            return Prototype.Groups.Any(stateGroup => stateGroup == group);
        }

        protected void ActivateMission(PrototypeId missionRef)
        {
            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
            if (MetaGame.Debug) Logger.Debug($"ActivateMission {GameDatabase.GetFormattedPrototypeName(missionRef)} {Prototype.GetType().Name}");
            if (missionProto is not OpenMissionPrototype) return;

            var manager = Region?.MissionManager;
            if (manager?.ShouldCreateMission(missionProto) == true)
                manager.ActivateMission(missionRef);
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
        public virtual void OnRemovePlayer(Player player) { }
        public virtual void OnUpdatePlayerNotification(Player player) { }
    }
}
