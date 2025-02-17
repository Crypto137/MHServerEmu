namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// An interface for linking a generic <see cref="EventPointer{T}"/> to an abstract <see cref="EventScheduler"/>.
    /// </summary>
    public interface IEventPointer
    {
        public void Set(ScheduledEvent @event);
    }

    /// <summary>
    /// A reference to a <typeparamref name="T"/> instance managed by an <see cref="EventScheduler"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="EventPointer{T}"/> instances are invalidated by the <see cref="EventScheduler"/> when
    /// their linked <typeparamref name="T"/> is triggered or cancelled.
    /// </remarks>
    public class EventPointer<T> : IEventPointer where T: ScheduledEvent
    {
        private T _event;

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="EventPointer{T}"/> is linked to a <typeparamref name="T"/> instance.
        /// </summary>
        public bool IsValid { get => _event != null; }

        /// <summary>
        /// Constructs a new <see cref="EventPointer{T}"/> instance.
        /// </summary>
        public EventPointer() { }

        public override string ToString()
        {
            return _event != null ? _event.ToString() : "NULL";
        }

        public static explicit operator T(EventPointer<T> pointer) => pointer._event;

        /// <summary>
        /// Returns the <typeparamref name="T"/> instance linked to this <see cref="EventPointer{T}"/>.
        /// </summary>
        public T Get()
        {
            return _event;
        }

        /// <summary>
        /// Links this <see cref="EventPointer{T}"/> to the provided <see cref="ScheduledEvent"/> instance.
        /// </summary>
        public void Set(ScheduledEvent value)
        {
            if (value != null && value is not T) return;
            Set((T)value);
        }

        /// <summary>
        /// Links this <see cref="EventPointer{T}"/> to the provided <typeparamref name="T"/> instance.
        /// </summary>
        public void Set(T value)
        {
            _event?.Unlink(this);   // Unlink an existing event if valid
            _event = value;         // Assign the new event reference
            _event?.Link(this);     // Link the new reference to this pointer
        }
    }
}
