using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public interface IEventPointer
    {
        public void Set(ScheduledEvent @event);
    }

    public class EventPointer<T> : IEventPointer, IEquatable<EventPointer<T>> where T: ScheduledEvent
    {
        private T _event;

        public bool IsValid { get => _event != null; }

        public T Get()
        {
            return _event;
        }

        public void Set(ScheduledEvent value)
        {
            if (value != null && value is not T) return;
            Set((T)value);
        }

        public void Set(T value)
        {
            _event?.Unlink(this);   // Unlink an existing event if valid
            _event = value;         // Assign the new event reference
            _event?.Link(this);      // Link the new reference to this pointer
        }

        public override string ToString() => _event.ToString();
        public override int GetHashCode() => _event.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is not EventPointer<T> other) return false;
            return Equals(other);
        }

        public bool Equals(EventPointer<T> other)
        {
            return _event.Equals(other._event);
        }

        public static explicit operator T(EventPointer<T> pointer) => pointer._event;
    }
}
