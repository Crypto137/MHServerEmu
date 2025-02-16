using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// Manages reusable <see cref="ScheduledEvent"/> instances of various subtypes.
    /// </summary>
    public class ScheduledEventPool
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<Type, Node> _nodeDict = new();

        public ScheduledEventPool() { }

        /// <summary>
        /// Retrieves or creates a new <see cref="ScheduledEvent"/> instance of subtype <typeparamref name="T"/>.
        /// </summary>
        public T Get<T>() where T: ScheduledEvent, new()
        {
            Type type = typeof(T);
            if (_nodeDict.TryGetValue(type, out Node node) == false)
            {
                node = new();
                _nodeDict.Add(type, node);
            }

            return node.Get<T>();
        }

        /// <summary>
        /// Returns a <see cref="ScheduledEvent"/> instance to the pool.
        /// </summary>
        public bool Return(ScheduledEvent @event)
        {
            // All events returned to the pool need to be created by the pool. If we don't have a node for this type, this event have been created somewhere else.
            Type type = @event.GetType();
            if (_nodeDict.TryGetValue(type, out Node node) == false)
                return Logger.WarnReturn(false, $"Return(): Failed to get a node for a scheduled event instance of type {type.Name}");

            node.Return(@event);
            return true;
        }

        /// <summary>
        /// Contains <see cref="ScheduledEvent"/> instances of a specific subtype.
        /// </summary>
        private class Node
        {
            private readonly Stack<ScheduledEvent> _stack = new();

            public int TotalCount { get; private set; }
            public int AvailableCount { get => _stack.Count; }

            public Node() { }

            /// <summary>
            /// Retrieves or creates a new <see cref="ScheduledEvent"/> instance of subtype <typeparamref name="T"/>.
            /// </summary>
            public T Get<T>() where T: ScheduledEvent, new()
            {
                T @event;

                if (_stack.Count == 0)
                {
                    @event = new();

                    TotalCount++;
                    //Logger.Trace($"Get<T>(): Created a new instance of {typeof(T).Name} (TotalCount={TotalCount})");
                }
                else
                {
                    @event = (T)_stack.Pop();
                }

                @event.ResetSortOrder();    // REMOVEME
                return @event;
            }

            /// <summary>
            /// Returns a <see cref="ScheduledEvent"/> instance to the pool node.
            /// </summary>
            public void Return(ScheduledEvent @event)
            {
                // Clear the event before pushing it to the stack to allow GC to collect the things it references
                @event.Clear();
                _stack.Push(@event);
            }
        }
    }
}
