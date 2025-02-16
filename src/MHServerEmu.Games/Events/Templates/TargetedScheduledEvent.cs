namespace MHServerEmu.Games.Events.Templates
{
    public abstract class TargetedScheduledEvent<T> : ScheduledEvent
    {
        protected T _eventTarget;

        public override void Clear()
        {
            _eventTarget = default;
        }
    }
}
