using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameMode
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public MetaGame MetaGame { get; }
        public Game Game { get; }
        public Region Region { get; }
        public MetaGameModePrototype Prototype { get; }
        public PrototypeId PrototypeDataRef { get; }

        protected TimeSpan _startTime;
        private EventGroup _timedGroup = new();
        protected EventGroup _pendingEvents = new();
        private EventPointer<ActiveGoalRepeatEvent> _activeGoalRepeatEvent = new();
        private Event<EntityEnteredWorldGameEvent>.Action _entityEnteredWorldAction;
        private LocaleStringId _modeText;

        public MetaGameMode(MetaGame metaGame, MetaGameModePrototype proto)
        {
            MetaGame = metaGame;
            Game = metaGame.Game;
            Region = metaGame.Region;
            Prototype = proto;
            PrototypeDataRef = proto.DataRef;

            _entityEnteredWorldAction = OnEntityEnteredWorld;
        }

        public static MetaGameMode CreateGameMode(MetaGame metaGame, PrototypeId modeRef)
        {
            var gamemodeProto = GameDatabase.GetPrototype<MetaGameModePrototype>(modeRef);
            if (MetaGame.Debug) Logger.Debug($"CreateGameMode {GameDatabase.GetFormattedPrototypeName(modeRef)} {gamemodeProto.GetType().Name}");
            return gamemodeProto.AllocateGameMode(metaGame);
        }

        #region Virtual

        public virtual void OnDestroy()
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelAllEvents(_pendingEvents);
            scheduler.CancelAllEvents(_timedGroup);
        }

        public virtual void OnActivate()
        {
            var proto = Prototype;
            if (MetaGame.Debug) Logger.Debug($"OnActivate {GameDatabase.GetFormattedPrototypeName(proto.DataRef)} {proto.GetType().Name}");

            _startTime = Game.CurrentTime;
            Region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);

            SendAvatarOnKilledInfoOverride(proto.AvatarOnKilledInfoOverride);
            SendUINotification(proto.UINotificationOnActivate);
            ScheduleTimedBanners(proto.UITimedBannersOnActivate);

            if (proto.UINotificationActiveGoalRepeat != PrototypeId.Invalid && proto.ActiveGoalRepeatTimeMS > 0)
                ScheduleActiveGoalRepeat(proto.ActiveGoalRepeatTimeMS);

            if (proto.ShowScoreboard) SendMessage(NetMessageShowPvPScoreboard.DefaultInstance);

            SendMetaGameInfoNotifications(proto.PlayerEnterNotifications);
            SendPlayUISoundTheme(proto.PlayerEnterAudioTheme);

            MetaGame.RemoveGroups(proto.RemoveGroups);
            MetaGame.RemoveStates(proto.RemoveStates);
            MetaGame.ApplyStates(proto.ApplyStates);
        }

        public virtual void OnAddPlayer(Player player)
        {
            var proto = Prototype;
            SendAvatarOnKilledInfoOverride(proto.AvatarOnKilledInfoOverride, player);
            SendUINotification(proto.UINotificationOnActivate);
            SendMetaGameInfoNotifications(proto.PlayerEnterNotifications, player);
        }

        public virtual void OnDeactivate()
        {
            var proto = Prototype;

            SendAvatarOnKilledInfoOverride(PrototypeId.Invalid);
            SendUINotification(proto.UINotificationOnDeactivate);

            var scheduler = Game.GameEventScheduler;
            if (scheduler != null)
            {
                scheduler.CancelEvent(_activeGoalRepeatEvent);
                scheduler.CancelAllEvents(_pendingEvents);
                scheduler.CancelAllEvents(_timedGroup);
            }

            var region = Region;
            if (region == null) return;

            foreach (var player in new PlayerIterator(Region))
                player.OnScoringEvent(new(ScoringEventType.MetaGameModeComplete, Prototype));

            if (proto.PlayerEnterNotifications.HasValue())
                SendClearMetaGameInfoNotification();

            region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
        }
        public virtual PrototypeId GetStartTargetOverride(Player player) => PrototypeId.Invalid;
        public virtual bool OnResurrect(Player player) => false;
        public virtual void OnRemovePlayer(Player player) { }
        public virtual void OnRemoveState(PrototypeId removeStateRef) { }
        public virtual void OnUpdatePlayerNotification(Player player)
        {
            SendSetModeText(player);
        }
        public virtual TimeSpan GetDurationTime() => TimeSpan.Zero;

        #endregion

        private void OnEntityEnteredWorld(in EntityEnteredWorldGameEvent evt)
        {
            var proto = Prototype;
            if (proto.PlayerEnterAudioTheme == AssetId.Invalid) return;

            if (evt.Entity is not Avatar avatar) return;
            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;
            SendPlayUISoundTheme(proto.PlayerEnterAudioTheme, player);
        }

        public void TeleportPlayersToTarget(PrototypeId targetRef)
        {
            List<Player> players = ListPool<Player>.Instance.Get();
            foreach (Player player in MetaGame.Players)
                players.Add(player);

            foreach (var player in players)
            {
                using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
                teleporter.Initialize(player, TeleportContextEnum.TeleportContext_MetaGame);
                teleporter.TeleportToTarget(targetRef);
            }

            ListPool<Player>.Instance.Return(players);
        }

        public void ResetStates()
        {
            var applyStates = Prototype.ApplyStates;
            if (applyStates.IsNullOrEmpty()) return;

            var metaStates = MetaGame.MetaStates;
            foreach (var state in applyStates)
            {
                var metaState = metaStates.FirstOrDefault(ms => ms.PrototypeDataRef == state);
                metaState?.OnReset();
            }
        }

        public void SetModeText(LocaleStringId modeText)
        {
            if (_modeText == modeText) return;

            _modeText = modeText;
            SendSetModeText();
        }

        public void GetInterestedClients(List<PlayerConnection> interestedClients, Player player = null)
        {
            if (player != null)
            {
                interestedClients.Add(player.PlayerConnection);
            }
            else
            {
                foreach (var regionPlayer in new PlayerIterator(Region))
                    interestedClients.Add(regionPlayer.PlayerConnection);
            }
        }

        #region SendMessage

        private void SendMessage(IMessage message, Player player = null)
        {
            if (player == null)
                Game.NetworkManager.SendMessageToInterested(message, Region);
            else
                player.SendMessage(message);
        }

        public void SendPlayUISoundTheme(AssetId soundThemeAssetRef, Player player = null)
        {
            if (soundThemeAssetRef == AssetId.Invalid) return;
            var message = NetMessagePlayUISoundTheme.CreateBuilder().SetSoundThemeAssetId((ulong)soundThemeAssetRef).Build();
            SendMessage(message, player);
        }

        private void SendMetaGameInfoNotifications(MetaGameNotificationDataPrototype[] notifications, Player player = null)
        {
            if (notifications.IsNullOrEmpty()) return;

            var interestedClients = ListPool<PlayerConnection>.Instance.Get();
            GetInterestedClients(interestedClients, player);

            foreach (var notificationData in notifications)
            {
                var entityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(notificationData.WorldEntityPrototype);
                if (entityProto == null) continue;

                var message = NetMessageMetaGameInfoNotification.CreateBuilder()
                    .SetEntityPrototypeId((ulong)entityProto.DataRef)
                    .SetDialogTextStringId((ulong)notificationData.DialogText)
                    .SetIconPathOverrideId((ulong)entityProto.IconPath).Build();

                Game.NetworkManager.SendMessageToMultiple(interestedClients, message);
            }
            ListPool<PlayerConnection>.Instance.Return(interestedClients);
        }

        protected void SendMetaGameBanner(List<PlayerConnection> interestedClients, LocaleStringId bannerText, List<long> intArgs = null,
            string playerName1 = "", string playerName2 = "",
            LocaleStringId arg1 = LocaleStringId.Blank, LocaleStringId arg2 = LocaleStringId.Blank)
        {
            if (bannerText == LocaleStringId.Blank) return;

            var message = NetMessageMetaGameBanner.CreateBuilder();
            message.SetMessageStringId((ulong)bannerText);
            if (playerName1 != null) message.SetPlayerName1(playerName1);
            if (playerName2 != null) message.SetPlayerName2(playerName2);
            if (intArgs != null) message.AddRangeIntArgs(intArgs);
            if (arg1 != LocaleStringId.Blank)
            {
                message.AddArgStringIds((ulong)arg1);
                if (arg2 != LocaleStringId.Blank)
                    message.AddArgStringIds((ulong)arg2);
            }

            Game.NetworkManager.SendMessageToMultiple(interestedClients, message.Build());
        }

        public void SendUINotification(PrototypeId uiNotificationRef)
        {
            if (uiNotificationRef == PrototypeId.Invalid) return;
            SendMessage(NetMessageUINotificationMessage.CreateBuilder().SetUiNotificationRef((ulong)uiNotificationRef).Build());
        }

        public void SetUITrackedEntityId(ulong entityId, Player player)
        {           
            SendMessage(NetMessageSetUITrackedEntityId.CreateBuilder().SetEntityId(entityId).Build(), player);
        }

        private void SendAvatarOnKilledInfoOverride(PrototypeId avatarOnKilledInfoRef, Player player = null)
        {
            Region.SetAvatarOnKilledInfo(avatarOnKilledInfoRef);

            var message = NetMessageAvatarOnKilledInfoOverride.CreateBuilder()
                .SetRegionId(Region.Id)
                .SetAvatarOnKilledInfoProtoId((ulong)avatarOnKilledInfoRef)
                .Build();
            
            SendMessage(message, player);
        }

        private void SendClearMetaGameInfoNotification()
        {
            var interestedClients = ListPool<PlayerConnection>.Instance.Get();
            GetInterestedClients(interestedClients);
            var message = NetMessageClearMetaGameInfoNotification.DefaultInstance;
            Game.NetworkManager.SendMessageToMultiple(interestedClients, message);
            ListPool<PlayerConnection>.Instance.Return(interestedClients);
        }

        public void SendSetModeText(Player player = null)
        {
            if (_modeText == LocaleStringId.Blank) return;

            var message = NetMessageSetModeText.CreateBuilder()
                .SetMetaGameId(MetaGame.Id)
                .SetModeRef((ulong)PrototypeDataRef)
                .SetModeTextId((ulong)_modeText)
                .Build();

            SendMessage(message, player);
        }

        public void SendDifficultyChange(Player player)
        {
            var region = Region;
            if (region == null) return;

            int dificultyIndex = region.TuningTable.DifficultyIndex;
            if (player != null)
            {
                player.SendRegionDifficultyChange(dificultyIndex);
            }
            else
            {
                foreach (var regionPlayer in MetaGame.Players)
                    regionPlayer.SendRegionDifficultyChange(dificultyIndex);
            }
        }

        public void SendPvEInstanceDeathUpdate(int current)
        {
            var message = NetMessagePvEInstanceDeathUpdate.CreateBuilder()
                 .SetMetaGameId(MetaGame.Id)
                 .SetCurrentDeathCount((ulong)current)
                 .Build();

            SendMessage(message);
        }

        public void SendPvEInstanceRegionScoreUpdate(int score, Player player)
        {
            var message = NetMessagePvEInstanceRegionScoreUpdate.CreateBuilder()
                 .SetMetaGameId(MetaGame.Id)
                 .SetCurrentRegionScore((ulong)score)
                 .Build();

            SendMessage(message, player);
        }

        protected void SendStartPvPTimer(TimeSpan startTime, TimeSpan endTime, TimeSpan lowTime, TimeSpan criticalTime, 
            Player player = null, LocaleStringId labelOverride = LocaleStringId.Blank)
        {
            var message = NetMessageStartPvPTimer.CreateBuilder()
                .SetMetaGameId(MetaGame.Id)
                .SetStartTime((uint)startTime.TotalMilliseconds)
                .SetEndTime((uint)endTime.TotalMilliseconds)
                .SetLowTimeWarning((uint)lowTime.TotalMilliseconds)
                .SetCriticalTimeWarning((uint)criticalTime.TotalMilliseconds)
                .SetLabelOverrideTextId((ulong)labelOverride)
                .Build();

            SendMessage(message, player);
        }

        protected void SendStopPvPTimer(Player player = null)
        {
            var message = NetMessageStopPvPTimer.CreateBuilder().SetMetaGameId(MetaGame.Id).Build();
            SendMessage(message, player);
        }

        protected void SendPlayKismetSeq(PrototypeId kismetSeqRef)
        {
            if (kismetSeqRef == PrototypeId.Invalid) return;
            var message = NetMessagePlayKismetSeq.CreateBuilder().SetKismetSeqPrototypeId((ulong)kismetSeqRef).Build();
            SendMessage(message);
        }

        #endregion

        #region Schedule

        private void ScheduleActiveGoalRepeat(int timeMs)
        {
            if (timeMs == 0) return;
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            TimeSpan timeOffset = TimeSpan.FromMilliseconds(timeMs);
            if (_activeGoalRepeatEvent.IsValid) return;
            scheduler.ScheduleEvent(_activeGoalRepeatEvent, timeOffset, _pendingEvents);
            _activeGoalRepeatEvent.Get().Initialize(this);
        }

        private void ScheduledActiveGoalRepeat()
        {
            var proto = Prototype;
            SendUINotification(proto.UINotificationActiveGoalRepeat);
            ScheduleActiveGoalRepeat(proto.ActiveGoalRepeatTimeMS);
        }

        private void ScheduleTimedBanners(MetaGameBannerTimeDataPrototype[] uiTimedBanners)
        {
            if (uiTimedBanners.IsNullOrEmpty()) return;
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;

            foreach (var timeBannerProto in uiTimedBanners)
            {
                if (timeBannerProto == null) continue;
                int time = timeBannerProto.TimerValueMS;
                if (timeBannerProto.TimerModeType == MetaGameModeTimerBannerType.Interval && time <= 0) continue;
                EventPointer<BannerTimeEvent> bannerPointer = new();
                var timeOffset = TimeSpan.FromMilliseconds(time);
                scheduler.ScheduleEvent(bannerPointer, timeOffset, _timedGroup);
                bannerPointer.Get().Initialize(this, timeBannerProto);
            }
        }

        private void ScheduledBannerTime(MetaGameBannerTimeDataPrototype bannerProto)
        {
            var interestedClients = ListPool<PlayerConnection>.Instance.Get();
            GetInterestedClients(interestedClients);

            var intArgs = ListPool<long>.Instance.Get();
            var runTime = Game.CurrentTime - _startTime;
            TimeSpan durationTime = GetDurationTime();

            intArgs.Add((long)runTime.TotalSeconds);
            intArgs.Add((long)durationTime.TotalSeconds);

            SendMetaGameBanner(interestedClients, bannerProto.BannerText, intArgs);
            ListPool<long>.Instance.Return(intArgs);
            ListPool<PlayerConnection>.Instance.Return(interestedClients);

            if (bannerProto.TimerModeType == MetaGameModeTimerBannerType.Once) return;

            // Reschedule event
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            EventPointer<BannerTimeEvent> bannerPointer = new();
            var timeOffset = TimeSpan.FromMilliseconds(bannerProto.TimerValueMS);
            scheduler.ScheduleEvent(bannerPointer, timeOffset, _timedGroup);
            bannerPointer.Get().Initialize(this, bannerProto);
        }

        #endregion

        public class ActiveGoalRepeatEvent : CallMethodEvent<MetaGameMode>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.ScheduledActiveGoalRepeat();
        }

        public class BannerTimeEvent : CallMethodEventParam1<MetaGameMode, MetaGameBannerTimeDataPrototype>
        {
            protected override CallbackDelegate GetCallback() => (gameMode, bannerProto) => gameMode.ScheduledBannerTime(bannerProto);
        }
    }
}
