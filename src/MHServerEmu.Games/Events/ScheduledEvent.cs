using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public abstract class ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // We don't need a collection of pointers for this like the client because
        // in practice there is never more than one pointer to an event.
        private IEventPointer _pointer;

        public LinkedListNode<ScheduledEvent> ProcessListNode { get; }
        public LinkedListNode<ScheduledEvent> EventGroupNode { get; }
        public TimeSpan FireTime { get; set; }

        public bool IsValid { get => _pointer != null; }

        public ScheduledEvent()
        {
            ProcessListNode = new(this);
            EventGroupNode = new(this);
        }

        public bool Link(IEventPointer pointer)
        {
            if (_pointer != null)
                return Logger.WarnReturn(false, $"Link(): {GetType().Name} is already linked to a pointer");

            _pointer = pointer;
            return true;
        }

        public bool Unlink(IEventPointer pointer)
        {
            if (pointer != _pointer)
                return Logger.WarnReturn(false, $"Unlink(): {GetType().Name} is not linked to the provided pointer");

            _pointer = null;
            return true;
        }

        public void InvalidatePointers()
        {
            _pointer?.Set(null);
        }

        public override string ToString()
        {
            return $"{nameof(FireTime)}: {FireTime.TotalMilliseconds} ms";
        }

        public abstract bool OnTriggered();
        public virtual bool OnCancelled() { return true; }

        public abstract void Clear();
    }
}
