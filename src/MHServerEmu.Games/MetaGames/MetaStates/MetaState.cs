using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
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

        protected void SetMissionFailedState(PrototypeId missionRef)
        {
            if (missionRef == PrototypeId.Invalid) return;
            var manager = Region?.MissionManager;
            if (manager == null || manager.IsInitialized == false) return;

            var mission = manager.FindMissionByDataRef(missionRef);
            if (mission == null) return;

            if (mission.State == MissionState.Active)
                mission.SetState(MissionState.Failed);
        }

        protected void PlayerMetaStateComplete()
        {
            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            var region = Region;
            if (region == null) return;

            var affixes = region.Settings.Affixes;
            var rarityRef = region.Settings.ItemRarity;
            int difficulty = region.Properties[PropertyEnum.RegionAffixDifficulty];
            int waveCount = MetaGame.Properties[PropertyEnum.MetaGameWaveCount];

            Prototype rarityProto = GameDatabase.GetPrototype<Prototype>(rarityRef);

            foreach (var player in new PlayerIterator(region))
            {
                player.OnScoringEvent(new(ScoringEventType.MetaGameStateComplete, Prototype, rarityProto));

                foreach (var affix in affixes)
                    if (affix != PrototypeId.Invalid)
                    {
                        Prototype affixProto = GameDatabase.GetPrototype<Prototype>(affix);
                        player.OnScoringEvent(new(ScoringEventType.MetaGameStateCompleteAffix, Prototype, affixProto, rarityProto));
                    }

                if (difficulty > 0)
                    player.OnScoringEvent(new(ScoringEventType.MetaGameStateCompleteDifficulty, Prototype, rarityProto, difficulty));

                if (waveCount > 0)
                    player.OnScoringEvent(new(ScoringEventType.MetaGameWaveComplete, mode.Prototype, waveCount));

                // region.PlayerMetaStateCompleteEvent.Invoke() ?
            }
        }

        public virtual void OnApply() { }

        public virtual void OnRemove() 
        { 
            GameEventScheduler?.CancelAllEvents(_pendingEvents); 
        }
        public virtual void OnReset() { }
        public virtual void OnRemovedState(PrototypeId removedStateRef) { }
        public virtual void OnAddPlayer(Player player) { }
        public virtual void OnRemovePlayer(Player player) { }
        public virtual void OnUpdatePlayerNotification(Player player) { }
    }
}
