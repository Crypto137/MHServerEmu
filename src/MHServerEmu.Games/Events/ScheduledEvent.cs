namespace MHServerEmu.Games.Events
{
    public abstract class ScheduledEvent
    {
        // TODO: Remove sort order workaround when we implement buckets for the scheduler
        private static ulong NextSortOrder = 0;

        private readonly HashSet<IEventPointer> _pointers = new();

        public ulong SortOrder { get; }    // REMOVEME
        public LinkedListNode<ScheduledEvent> EventGroupNode { get; set; }
        public TimeSpan FireTime { get; set; }

        public ScheduledEvent()
        {
            SortOrder = ++NextSortOrder;    // REMOVEME
        }

        public bool IsValid { get => _pointers.Count > 0; }

        public bool Link(IEventPointer pointer)
        {
            return _pointers.Add(pointer);
        }

        public bool Unlink(IEventPointer pointer)
        {
            return _pointers.Remove(pointer);
        }

        public void InvalidatePointers()
        {
            foreach (IEventPointer pointer in _pointers)
                pointer.Set(null);

            _pointers.Clear();
        }

        public override string ToString()
        {
            return $"{nameof(FireTime)}: {FireTime.TotalMilliseconds} ms";
        }

        public abstract bool OnTriggered();
        public virtual bool OnCancelled() { return true; }
    }
}
