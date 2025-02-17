using System.Text;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// Specialized pool for managing reusable object instances for <see cref="EventScheduler"/>.
    /// </summary>
    public class ScheduledEventPool
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<Type, Node> _nodeDict = new();

        private readonly Stack<LinkedList<ScheduledEvent>> _listStack = new();
        private int _totalListCount = 0;

        /// <summary>
        /// The number of <see cref="ScheduledEvent"/> instances allocated by this <see cref="ScheduledEventPool"/> that are currently in use.
        /// </summary>
        /// <remarks>
        /// Used primarily as a fast lookup for metrics.
        /// </remarks>
        public int ActiveInstanceCount { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="ScheduledEventPool"/> instance.
        /// </summary>
        public ScheduledEventPool()
        {
            // Preallocate some linked lists to store window buckets, each stores 1 frame of events
            const int StartingListCount = 8192;
            for (int i = 0; i < StartingListCount; i++)
            {
                LinkedList<ScheduledEvent> list = new();
                _totalListCount++;
                _listStack.Push(list);
            }
        }

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

            T @event = node.Get<T>();
            ActiveInstanceCount++;
            return @event;
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
            ActiveInstanceCount--;
            return true;
        }

        /// <summary>
        /// Retrieves or creates a new <see cref="LinkedList{T}"/> instance.
        /// </summary>
        public LinkedList<ScheduledEvent> GetList()
        {
            LinkedList<ScheduledEvent> list;

            if (_listStack.Count == 0)
            {
                list = new();
                _totalListCount++;
            }
            else
            {
                list = _listStack.Pop();
            }

            return list;
        }

        /// <summary>
        /// Returns a <see cref="LinkedList{T}"/> instance to the pool.
        /// </summary>
        public bool ReturnList(LinkedList<ScheduledEvent> eventList)
        {
            // Here we accept LinkedList instances created elsewhere (e.g. when constructing WindowBuckets)
            if (eventList.Count != 0)
                return Logger.WarnReturn(false, "ReturnList(): Attemped to return non-empty LinkedList to the pool");

            _listStack.Push(eventList);
            return true;
        }

        /// <summary>
        /// Returns a <see cref="string"/> representing the current state of this <see cref="ScheduledEventPool"/> instance.
        /// </summary>
        public string GetReportString()
        {
            StringBuilder sb = new();

            sb.AppendLine("Name\tActive\tAvailable\tTotal");

            // Accuracy > efficiency here, so recalculate all counts using the data from actual nodes
            int availableSum = 0;
            int totalSum = 0;
            int activeSum = 0;

            foreach (var kvp in _nodeDict.OrderBy(kvp => kvp.Key.Name))
            {
                string name = kvp.Key.Name;
                int available = kvp.Value.AvailableCount;
                int total = kvp.Value.TotalCount;
                int active = total - available;

                availableSum += available;
                totalSum += total;
                activeSum += active;

                sb.AppendLine($"{name}\t{active}\t{available}\t{total}");
            }

            sb.AppendLine();
            sb.AppendLine($"TOTAL\t{activeSum}\t{availableSum}\t{totalSum}");

            int availableListCount = _listStack.Count;
            int activeListCount = _totalListCount - availableListCount;
            sb.AppendLine($"LinkedListCount\t{activeListCount}\t{availableListCount}\t{_totalListCount}");

            return sb.ToString();
        }

        /// <summary>
        /// Contains <see cref="ScheduledEvent"/> instances of a specific subtype.
        /// </summary>
        private class Node
        {
            private readonly Stack<ScheduledEvent> _stack = new();

            /// <summary>
            /// The total number of <see cref="ScheduledEvent"/> instances allocated by this <see cref="Node"/>.
            /// </summary>
            public int TotalCount { get; private set; }

            /// <summary>
            /// The number of <see cref="ScheduledEvent"/> instances that have been returned to this <see cref="Node"/> after use.
            /// </summary>
            public int AvailableCount { get => _stack.Count; }

            /// <summary>
            /// Constructs a new <see cref="Node"/> instance.
            /// </summary>
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
                }
                else
                {
                    @event = (T)_stack.Pop();
                }

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
