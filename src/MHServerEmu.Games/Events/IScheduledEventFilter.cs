namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// Filters <see cref="ScheduledEvent"/> instances.
    /// </summary>
    public interface IScheduledEventFilter
    {
        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="ScheduledEvent"/> passes the filter.
        /// </summary>
        public bool Filter(ScheduledEvent @event);
    }
}
