using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionTimedAction : MissionAction
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private MissionActionTimedActionPrototype _proto;
        private MissionActionList _actionsToPerform;
        private EventGroup _pendingEvents = new();

        public MissionActionTimedAction(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // JuggernautEmergenceController
            _proto = prototype as MissionActionTimedActionPrototype;
        }

        public override void Destroy()
        {
            Mission.GameEventScheduler.CancelAllEvents(_pendingEvents);
            _actionsToPerform?.Destroy();
        }

        public override void Run()
        {
            if (MissionManager.Debug) Logger.Trace($"Run timer {Mission.PrototypeName} {_proto.DelayInSeconds} sec");
            var time = TimeSpan.FromSeconds(_proto.DelayInSeconds);
            EventPointer<TimedActionsEvent> timedActionsEvent = new();
            Mission.GameEventScheduler.ScheduleEvent(timedActionsEvent, time, _pendingEvents);
            timedActionsEvent.Get().Initialize(this);
        }

        private void OnTimedActions()
        {
            if (MissionManager.Debug) Logger.Trace($"OnTimedActions timer {Mission.PrototypeName} {_proto.DelayInSeconds} sec");
            MissionActionList.CreateActionList(ref _actionsToPerform, _proto.ActionsToPerform, Mission);
            if (_proto.Repeat) Run();
            else _actionsToPerform?.Destroy();
        }        
        
        public class TimedActionsEvent : CallMethodEvent<MissionActionTimedAction>
        {
            protected override CallbackDelegate GetCallback() => (action) => action.OnTimedActions();
        }
    }
}
