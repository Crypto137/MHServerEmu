namespace MHServerEmu.Games.Events.Templates
{
    /// <summary>
    /// An abstract template for a <see cref="ScheduledEvent"/> that operates on a <typeparamref name="T"/> instance.
    /// </summary>
    public abstract class TargetedScheduledEvent<T> : ScheduledEvent
    {
        protected T _eventTarget;

        public override void Clear()
        {
            _eventTarget = default;
        }
    }
}
