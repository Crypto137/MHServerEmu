using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public abstract class ScheduledEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // TODO: Remove sort order workaround when we implement buckets for the scheduler
        private static ulong NextSortOrder = 0;

        // We don't need a collection of pointers for this like the client because
        // in practice there is never more than one pointer to an event.
        private IEventPointer _pointer;

        public ulong SortOrder { get; }    // REMOVEME
        // TODO: public LinkedListNode<ScheduledEvent> ProcessListNode { get; }
        public LinkedListNode<ScheduledEvent> EventGroupNode { get; }
        public TimeSpan FireTime { get; set; }

        public ScheduledEvent()
        {
            SortOrder = ++NextSortOrder;    // REMOVEME
            EventGroupNode = new(this);
        }

        public bool IsValid { get => _pointer != null; }

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
    }
}
