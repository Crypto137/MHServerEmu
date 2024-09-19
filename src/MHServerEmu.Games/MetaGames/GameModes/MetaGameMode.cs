using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames.MetaStates;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameMode
    {
        public MetaGame MetaGame { get; }
        public Game Game { get; }
        public Region Region { get; }
        public MetaGameModePrototype Prototype { get; }
        public PrototypeId PrototypeDataRef { get; }

        private TimeSpan _startTime;
        private EventGroup _timedGroup = new();
        protected EventGroup _pendingEvents = new();
        private EventPointer<ActiveGoalRepeatEvent> _activeGoalRepeatEvent = new();
        private Action<EntityEnteredWorldGameEvent> _entityEnteredWorldAction;

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
            return gamemodeProto.AllocateGameMode(metaGame);
        }

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

        private void OnEntityEnteredWorld(EntityEnteredWorldGameEvent evt)
        {
            var proto = Prototype;
            if (proto.PlayerEnterAudioTheme == AssetId.Invalid) return;

            if (evt.Entity is not Avatar avatar) return;
            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;
            SendPlayUISoundTheme(proto.PlayerEnterAudioTheme, player);
        }

        private void SendPlayUISoundTheme(AssetId soundThemeAssetRef, Player player = null)
        {
            if (soundThemeAssetRef == AssetId.Invalid) return;
            var message = NetMessagePlayUISoundTheme.CreateBuilder().SetSoundThemeAssetId((ulong)soundThemeAssetRef).Build();
            SendMessage(message, player);
        }

        public List<PlayerConnection> GetInterestedClients(Player player = null)
        {
            List<PlayerConnection> interestedClients = new();
            if (player != null)
            {
                interestedClients.Add(player.PlayerConnection);
            }
            else
            {
                foreach (var regionPlayer in new PlayerIterator(Region))
                    interestedClients.Add(regionPlayer.PlayerConnection);
            }
            return interestedClients;
        }

        private void SendMetaGameInfoNotifications(MetaGameNotificationDataPrototype[] notifications, Player player = null)
        {
            if (notifications.IsNullOrEmpty()) return;

            var interestedClients = GetInterestedClients(player);

            foreach(var notificationData in notifications)
            {
                var entityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(notificationData.WorldEntityPrototype);
                if (entityProto == null) continue;

                var message = NetMessageMetaGameInfoNotification.CreateBuilder()
                    .SetEntityPrototypeId((ulong)entityProto.DataRef)
                    .SetDialogTextStringId((ulong)notificationData.DialogText)
                    .SetIconPathOverrideId((ulong)entityProto.IconPath).Build();

                Game.NetworkManager.SendMessageToMultiple(interestedClients, message);
            }
        }

        private void ScheduleActiveGoalRepeat(int timeMs)
        {
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

            foreach(var timeBannerProto in uiTimedBanners)
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
            var interestedClients = GetInterestedClients();

            List<long> intArgs = new();
            var runTime = Game.CurrentTime - _startTime;
            TimeSpan durationTime = GetDurationTime();

            intArgs.Add((long)runTime.TotalSeconds);
            intArgs.Add((long)durationTime.TotalSeconds);

            SendMetaGameBanner(interestedClients, bannerProto.BannerText, intArgs);

            if (bannerProto.TimerModeType == MetaGameModeTimerBannerType.Once) return;

            // Reschedule event
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            EventPointer<BannerTimeEvent> bannerPointer = new();
            var timeOffset = TimeSpan.FromMilliseconds(bannerProto.TimerValueMS);
            scheduler.ScheduleEvent(bannerPointer, timeOffset, _timedGroup);
            bannerPointer.Get().Initialize(this, bannerProto);
        }

        public virtual TimeSpan GetDurationTime() => TimeSpan.Zero;

        private void SendMetaGameBanner(List<PlayerConnection> interestedClients, LocaleStringId bannerText, List<long> intArgs = null,
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

        private void SendUINotification(PrototypeId uiNotificationRef)
        {
            if (uiNotificationRef == PrototypeId.Invalid) return;
            SendMessage(NetMessageUINotificationMessage.CreateBuilder().SetUiNotificationRef((ulong)uiNotificationRef).Build());
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

        private void SendMessage(IMessage message, Player player = null)
        {
            if (player == null)
                Game.NetworkManager.BroadcastMessage(message);
            else
                player.SendMessage(message);
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

            // TODO achievement
            if (proto.PlayerEnterNotifications.HasValue())
                SendClearMetaGameInfoNotification();

            region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
        }

        private void SendClearMetaGameInfoNotification()
        {
            var interestedClients = GetInterestedClients();
            var message = NetMessageClearMetaGameInfoNotification.DefaultInstance;
            Game.NetworkManager.SendMessageToMultiple(interestedClients, message);
        }

        public virtual void OnRemoveState(MetaState state) { }

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
