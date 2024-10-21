using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public class Event
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const int InfiniteLoopCheckLimit = 5000; // in client 100000
        private readonly LinkedList<Action> _actionList = new();
        private readonly List<ActionIterator<Action>> _iteratorList = new();
        private int _infiniteLoopCheckCount = 0;

        public bool AddActionFront(Action action)
        {
            if (action == null) return Logger.ErrorReturn(false, "AddActionFront action == null");
            if (_infiniteLoopCheckCount >= InfiniteLoopCheckLimit) return Logger.ErrorReturn(false, $"AddActionFront {_infiniteLoopCheckCount} >= InfiniteLoopCheckLimit");

            _actionList.AddFirst(action);
            return _infiniteLoopCheckCount < InfiniteLoopCheckLimit;
        }

        public bool AddActionBack(Action action)
        {
            if (action == null) return Logger.ErrorReturn(false, "AddActionBack action == null");
            if (_infiniteLoopCheckCount >= InfiniteLoopCheckLimit) return Logger.ErrorReturn(false, $"AddActionBack {_infiniteLoopCheckCount} >= InfiniteLoopCheckLimit");

            var newNode = _actionList.AddLast(action);
            foreach (var iterator in _iteratorList)
                iterator.CurrentNode ??= newNode;

            return _infiniteLoopCheckCount < InfiniteLoopCheckLimit;
        }

        public void RemoveAction(Action action)
        {
            if (action == null) return;
            var nodeToRemove = _actionList.Find(action);
            if (nodeToRemove == null) return;

            foreach (var iterator in _iteratorList)
                if (iterator.CurrentNode == nodeToRemove)
                    iterator.MoveNext();

            _actionList.Remove(nodeToRemove);
        }

        public void Invoke()
        {
            if (_actionList.Count == 0) return;

            ActionIterator<Action> iterator = new(_actionList);
            _iteratorList.Add(iterator);

            while (iterator.CurrentNode != null)
            {
                var action = iterator.Current;
                iterator.MoveNext();
                action();
                _infiniteLoopCheckCount++;
            }

            _iteratorList.Remove(iterator);
            if (_iteratorList.Count == 0) _infiniteLoopCheckCount = 0;
        }

        public void UnregisterCallbacks() => _actionList.Clear();
    }

    public class ActionIterator<T>
    {
        private readonly LinkedList<T> _list;
        public LinkedListNode<T> CurrentNode { get; set; }
        public T Current => CurrentNode.Value;

        public ActionIterator(LinkedList<T> list)
        {
            _list = list;
            CurrentNode = _list.First;
        }

        public void MoveNext()
        {
            if (CurrentNode != null) CurrentNode = CurrentNode.Next;
        }
    }

    public class Event<T>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly LinkedList<Action<T>> _actionList = new();
        private readonly List<ActionIterator<Action<T>>> _iteratorList = new();
        private int _infiniteLoopCheckCount = 0;

        public bool AddActionFront(Action<T> action)
        {
            if (action == null) return Logger.ErrorReturn(false, $"AddActionFront [{typeof(T).Name}]  action == null") ;
            if (_infiniteLoopCheckCount >= Event.InfiniteLoopCheckLimit) return Logger.ErrorReturn(false, $"AddActionFront [{typeof(T).Name}] {_infiniteLoopCheckCount} >= InfiniteLoopCheckLimit");

            _actionList.AddFirst(action);
            return _infiniteLoopCheckCount < Event.InfiniteLoopCheckLimit;
        }

        public bool AddActionBack(Action<T> action)
        {
            if (action == null) return Logger.ErrorReturn(false, $"AddActionBack [{typeof(T).Name}]  action == null");
            if (_infiniteLoopCheckCount >= Event.InfiniteLoopCheckLimit) return Logger.ErrorReturn(false, $"AddActionBack [{typeof(T).Name}] {_infiniteLoopCheckCount} >= InfiniteLoopCheckLimit");

            var newNode = _actionList.AddLast(action);
            foreach (var iterator in _iteratorList)
                iterator.CurrentNode ??= newNode;

            return _infiniteLoopCheckCount < Event.InfiniteLoopCheckLimit;
        }

        public void RemoveAction(Action<T> action)
        {
            if (action == null) return;
            var nodeToRemove = _actionList.Find(action);
            if (nodeToRemove == null) return;

            foreach (var iterator in _iteratorList)
                if (iterator.CurrentNode == nodeToRemove)
                    iterator.MoveNext();

            _actionList.Remove(nodeToRemove);
        }

        public void Invoke(T eventData)
        {
            if (_actionList.Count == 0) return;

            ActionIterator<Action<T>> iterator = new(_actionList);
            _iteratorList.Add(iterator);

            while (iterator.CurrentNode != null)
            {
                var action = iterator.Current;
                iterator.MoveNext();
                action(eventData);
                _infiniteLoopCheckCount++;
            }

            _iteratorList.Remove(iterator);
            if (_iteratorList.Count == 0) _infiniteLoopCheckCount = 0;
        }

        public void UnregisterCallbacks() => _actionList.Clear();
    }
}
