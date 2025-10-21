using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameModeIdle : MetaGameMode
    {
        private MetaGameModeIdlePrototype _proto;
        private Event<EntityEnteredWorldGameEvent>.Action _entityEnteredWorldAction;
        private EventPointer<NextModeEvent> _nextModeEvent;
        private TimeSpan _endTime;
        private int _playerCount;

        public MetaGameModeIdle(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as MetaGameModeIdlePrototype;
            _entityEnteredWorldAction = OnEntityEnteredWorld;
            _nextModeEvent = new();
            _endTime = TimeSpan.Zero;
            _playerCount = 0;
        }

        public override void OnActivate()
        {
            var region = Region;
            if (region == null) return;

            base.OnActivate();

            region.EntityEnteredWorldEvent.AddActionBack(_entityEnteredWorldAction);

            SetModeText(_proto.Name);

            var duration = TimeSpan.FromMilliseconds(_proto.DurationMS);
            if (_proto.ShowTimer)
            {
                SendStartPvPTimer(duration, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
                _endTime = Game.CurrentTime + duration;
            }

            foreach(var player in MetaGame.Players)
                ActivatePlayer(player);

            SendPlayKismetSeq(_proto.KismetSequenceOnActivate);
            ScheduleNextMode(duration);
        }

        public override void OnDeactivate()
        {
            var region = Region;
            if (region == null) return;

            base.OnDeactivate();

            region.EntityEnteredWorldEvent.RemoveAction(_entityEnteredWorldAction);
            Game.GameEventScheduler.CancelEvent(_nextModeEvent);

            foreach (var player in MetaGame.Players)
                DeactivatePlayer(player);

            if (_proto.ShowTimer)
                SendStopPvPTimer();
        }

        public override void OnAddPlayer(Player player)
        {
            if (player == null) return;
            base.OnAddPlayer(player);

            ActivatePlayer(player);
            _playerCount++;

            if (_proto.PlayerCountToAdvance > 0 && _playerCount <= _proto.PlayerCountToAdvance) NextMode();

            if (_proto.ShowTimer)
            {
                var duration = GetDurationTime();
                SendStartPvPTimer(duration, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, player);
            }
        }

        public override void OnRemovePlayer(Player player)
        {
            if (player == null) return;
            DeactivatePlayer(player);
            _playerCount--;
        }

        public override bool OnResurrect(Player player)
        {
            // _proto.DeathRegionTarget PvEScale only!!!
            return false;
        }

        private void ScheduleNextMode(TimeSpan timeOffset)
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            if (timeOffset <= TimeSpan.Zero) return;
            if (_nextModeEvent.IsValid) return;
            scheduler.ScheduleEvent(_nextModeEvent, timeOffset, _pendingEvents);
            _nextModeEvent.Get().Initialize(this);
        }

        private void ActivatePlayer(Player player)
        {
            var avatar = player.CurrentAvatar;
            if (avatar == null) return;

            // _proto.TeleportPlayersToStartOnActivate // Not used

            if (_proto.PlayersCanMove == false)
            {
                avatar.Properties[PropertyEnum.SystemImmobilized] = true;
                avatar.Properties[PropertyEnum.PowerLock] = true;
            }
        }

        private void DeactivatePlayer(Player player)
        {
            var avatar = player.CurrentAvatar;
            if (avatar == null) return;

            if (_proto.PlayersCanMove == false)
            {
                avatar.Properties.RemoveProperty(PropertyEnum.SystemImmobilized);
                avatar.Properties.RemoveProperty(PropertyEnum.PowerLock);
                if (_proto.PlayerLockVisualsPower != PrototypeId.Invalid)
                    avatar.UnassignPower(_proto.PlayerLockVisualsPower);
            }
        }

        private void OnEntityEnteredWorld(in EntityEnteredWorldGameEvent evt)
        {
            if (evt.Entity is not Avatar avatar) return;
            if (_proto.PlayerLockVisualsPower != PrototypeId.Invalid)
            {
                PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
                avatar.AssignPower(_proto.PlayerLockVisualsPower, indexProps);
                var position = avatar.RegionLocation.Position;
                var powerSettings = new PowerActivationSettings(avatar.Id, Vector3.Zero, position)
                { Flags = PowerActivationSettingsFlags.NotifyOwner };
                avatar.ActivatePower(_proto.PlayerLockVisualsPower, ref powerSettings);
            }
        }

        public override TimeSpan GetDurationTime()
        {
            return _endTime - Game.CurrentTime;
        }

        public void NextMode()
        {
            MetaGame.ScheduleActivateGameMode(_proto.NextMode);
        }

        public class NextModeEvent : CallMethodEvent<MetaGameModeIdle>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.NextMode();
        }
    }
}
