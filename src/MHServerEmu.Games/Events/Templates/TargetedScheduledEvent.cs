namespace MHServerEmu.Games.Events.Templates
{
    public abstract class TargetedScheduledEvent<T> : ScheduledEvent
    {
        protected T _eventTarget;
    }
}
