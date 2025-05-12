using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// Base class for events managed by <see cref="EventScheduler"/>.
    /// </summary>
    public abstract class ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // We don't need a collection of pointers for this like the client because
        // in practice there is never more than one pointer to an event.
        private IEventPointer _pointer;

        /// <summary>
        /// Intrusive linked list node for a frame or window bucket.
        /// </summary>
        public LinkedListNode<ScheduledEvent> ProcessListNode { get; }

        /// <summary>
        /// Intrusive linked list node for an <see cref="EventGroup"/>.
        /// </summary>
        public LinkedListNode<ScheduledEvent> EventGroupNode { get; }

        /// <summary>
        /// The time this event is supposed to be fired at.
        /// </summary>
        public TimeSpan FireTime { get; set; }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="ScheduledEvent"/> instance is linked to an <see cref="IEventPointer"/>.
        /// </summary>
        public bool IsValid { get => _pointer != null; }

        /// <summary>
        /// Constructs a new <see cref="ScheduledEvent"/> instance.
        /// </summary>
        public ScheduledEvent()
        {
            ProcessListNode = new(this);
            EventGroupNode = new(this);
        }

        public override string ToString()
        {
            return $"{nameof(FireTime)}: {FireTime.TotalMilliseconds} ms";
        }

        /// <summary>
        /// Links this <see cref="ScheduledEvent"/> to the provided <see cref="IEventPointer"/> instance.
        /// Returns <see langword="false"/> if already linked to another instance.
        /// </summary>
        public bool Link(IEventPointer pointer)
        {
            if (_pointer != null)
                return Logger.WarnReturn(false, $"Link(): {GetType().Name} is already linked to a pointer");

            _pointer = pointer;
            return true;
        }

        /// <summary>
        /// Unlinks this <see cref="ScheduledEvent"/> from the provided <see cref="IEventPointer"/> instance.
        /// Returns <see langword="false"/> if not linked to the provided instance.
        /// </summary>
        public bool Unlink(IEventPointer pointer)
        {
            if (pointer != _pointer)
                return Logger.WarnReturn(false, $"Unlink(): {GetType().Name} is not linked to the provided pointer");

            _pointer = null;
            return true;
        }

        /// <summary>
        /// Invalidates the <see cref="IEventPointer"/> instance this <see cref="ScheduledEvent"/> is linked to (if it is).
        /// </summary>
        /// <remarks>
        /// The name of this method uses plural "pointers" to match the client's API.
        /// </remarks>
        public void InvalidatePointers()
        {
            _pointer?.Set(null);
        }

        /// <summary>
        /// The callback that runs when this <see cref="ScheduledEvent"/> is triggered by an <see cref="EventScheduler"/>.
        /// </summary>
        public abstract bool OnTriggered();

        /// <summary>
        /// The callback that runs when this <see cref="ScheduledEvent"/> is cancelled.
        /// </summary>
        public virtual bool OnCancelled()
        {
            return true;
        }

        /// <summary>
        /// Called when this <see cref="ScheduledEvent"/> is returned to its <see cref="ScheduledEventPool"/>.
        /// </summary>
        public abstract void Clear();
    }
}
