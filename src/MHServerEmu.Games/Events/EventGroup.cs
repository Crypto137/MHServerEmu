using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// An intrusive collection of <see cref="ScheduledEvent"/>. Used primarily to cancel groups of related events.
    /// </summary>
    public class EventGroup
    {
        private readonly LinkedList<ScheduledEvent> _eventList = new();

        /// <summary>
        /// Returns the first <see cref="ScheduledEvent"/> instance in this <see cref="EventGroup"/>.
        /// Returns <see langword="null"/> if this <see cref="EventGroup"/> is empty.
        /// </summary>
        public ScheduledEvent Front { get => _eventList.First?.Value; }
        
        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="EventGroup"/> contains any <see cref="ScheduledEvent"/> instances.
        /// </summary>
        public bool IsEmpty { get => _eventList.First == null; }

        /// <summary>
        /// Adds the provided <see cref="ScheduledEvent"/> instance to this <see cref="EventGroup"/>.
        /// </summary>
        public void Add(ScheduledEvent @event)
        {
            @event.EventGroupNode.Remove();
            _eventList.AddLast(@event.EventGroupNode);
        }

        /// <summary>
        /// Removes the provided <see cref="ScheduledEvent"/> from this <see cref="EventGroup"/>.
        /// Returns <see langword="false"/> if the provided instance does not belong to this <see cref="EventGroup"/>.
        /// </summary>
        public bool Remove(ScheduledEvent @event)
        {
            LinkedListNode<ScheduledEvent> node = @event.EventGroupNode;

            if (node.List != _eventList)
                return false;

            node.Remove();
            return true;
        }

        /// <summary>
        /// Returns a enumerator for <see cref="ScheduledEvent"/> instances belonging to this <see cref="EventGroup"/>.
        /// </summary>
        public LinkedList<ScheduledEvent>.Enumerator GetEnumerator()
        {
            return _eventList.GetEnumerator();
        }
    }
}
