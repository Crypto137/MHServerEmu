
namespace MHServerEmu.Games.Events
{

    public class GameEventScheduler : EventScheduler
    {
    }

    public class EventScheduler
    {
        internal void CancelEvent(ScheduledEvent time)
        {
            throw new NotImplementedException();
        }

        internal void RescheduleEvent(ScheduledEvent evt, TimeSpan time)
        {
            throw new NotImplementedException();
        }

        internal void ScheduleEvent(ScheduledEvent evt, TimeSpan time, EventGroup pendingEvents)
        {
            throw new NotImplementedException();
        }
    }

    public class EventGroup
    {
        public List<ScheduledEvent> EventList = new();
    }

    public class ScheduledEvent
    {
        private object linkEvent;
        public TimeSpan FireTime { get; private set; }
        public bool IsValid() { return linkEvent != null; }
        public virtual void OnTriggered() { }
        public virtual void OnCancelled() { }
    }
}
