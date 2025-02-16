using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// Represents an intrusive collection of <see cref="ScheduledEvent"/>.
    /// </summary>
    public class EventGroup
    {
        private readonly LinkedList<ScheduledEvent> _eventList = new();

        public ScheduledEvent Front { get => _eventList.First?.Value; }
        public bool IsEmpty { get => _eventList.First == null; }

        public void Add(ScheduledEvent @event)
        {
            @event.EventGroupNode.Remove();
            _eventList.AddLast(@event.EventGroupNode);
        }

        public bool Remove(ScheduledEvent @event)
        {
            LinkedListNode<ScheduledEvent> node = @event.EventGroupNode;

            if (node.List != _eventList)
                return false;

            node.Remove();
            return true;
        }

        public LinkedList<ScheduledEvent>.Enumerator GetEnumerator()
        {
            return _eventList.GetEnumerator();
        }
    }
}
