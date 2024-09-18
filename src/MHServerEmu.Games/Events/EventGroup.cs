using System.Collections;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// Represents an intrusive collection of <see cref="ScheduledEvent"/>.
    /// </summary>
    public class EventGroup : IEnumerable<ScheduledEvent>
    {
        private readonly LinkedList<ScheduledEvent> _eventList = new();

        public ScheduledEvent Front { get => _eventList.First?.Value; }
        public bool IsEmpty { get => _eventList.First == null; }

        public void Add(ScheduledEvent @event)
        {
            @event.EventGroupNode?.Remove();
            @event.EventGroupNode = _eventList.AddLast(@event);
        }

        public bool Remove(ScheduledEvent @event)
        {
            if (@event.EventGroupNode == null) return false;
            if (@event.EventGroupNode.List != _eventList) return false;
            @event.EventGroupNode.Remove();
            return true;
        }

        public IEnumerator<ScheduledEvent> GetEnumerator() => _eventList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
