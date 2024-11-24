using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameModeShutdown : MetaGameMode
    {
        private MetaGameModeShutdownPrototype _proto;
        private EventPointer<ShutdownEvent> _shutdownEvent;

        public MetaGameModeShutdown(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as MetaGameModeShutdownPrototype;
            _shutdownEvent = new();
        }

        public override void OnActivate()
        {
            if (_proto.ShutdownTarget != PrototypeId.Invalid)
                TeleportPlayersToTarget(_proto.ShutdownTarget);

            if (_proto.Behavior == MetaGameModeShutdownBehaviorType.Delay)
                ScheduleShutdown(TimeSpan.FromMinutes(1));
            else
                Shutdown();
        }

        public override void OnDeactivate()
        {
            Game.GameEventScheduler?.CancelEvent(_shutdownEvent);
            base.OnDeactivate();
        }

        private void ScheduleShutdown(TimeSpan timeOffset)
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            if (timeOffset <= TimeSpan.Zero) return;
            if (_shutdownEvent.IsValid) return;
            scheduler.ScheduleEvent(_shutdownEvent, timeOffset, _pendingEvents);
            _shutdownEvent.Get().Initialize(this);
        }

        private void Shutdown()
        {
            TeleportPlayersToTarget(GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion);

            // TODO Shutdown Region event
            Region.RequestShutdown();
        }

        public class ShutdownEvent : CallMethodEvent<MetaGameModeShutdown>
        {
            protected override CallbackDelegate GetCallback() => gameMode => gameMode.Shutdown();
        }
    }
}

