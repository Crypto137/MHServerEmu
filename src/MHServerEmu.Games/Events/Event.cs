using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public class Event
    {
        public const int InfiniteLoopCheckLimit = 5000; // in client 100000

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly LinkedList<Action> _actionList = new();
        private readonly Stack<ActionIterator<Action>> _iteratorStack = new();
        private readonly List<ActionIterator<Action>> _activeIteratorList = new();
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

            LinkedListNode<Action> newNode = _actionList.AddLast(action);
            foreach (ActionIterator<Action> iterator in _activeIteratorList)
                iterator.CurrentNode ??= newNode;

            return _infiniteLoopCheckCount < InfiniteLoopCheckLimit;
        }

        public void RemoveAction(Action action)
        {
            if (action == null)
                return;

            LinkedListNode<Action> nodeToRemove = _actionList.Find(action);
            if (nodeToRemove == null)
                return;

            foreach (ActionIterator<Action> iterator in _activeIteratorList)
            {
                if (iterator.CurrentNode == nodeToRemove)
                    iterator.MoveNext();
            }

            _actionList.Remove(nodeToRemove);
        }

        public void Invoke()
        {
            if (_actionList.Count == 0)
                return;

            ActionIterator<Action> iterator = GetIterator();

            while (iterator.CurrentNode != null)
            {
                Action action = iterator.Current;
                iterator.MoveNext();
                action();
                _infiniteLoopCheckCount++;
            }

            ReturnIterator(iterator);

            if (_activeIteratorList.Count == 0)
                _infiniteLoopCheckCount = 0;
        }

        public void UnregisterCallbacks()
        {
            _actionList.Clear();
        }

        private ActionIterator<Action> GetIterator()
        {
            ActionIterator<Action> iterator;

            if (_iteratorStack.Count == 0)
            {
                iterator = new(_actionList);
            }
            else
            {
                iterator = _iteratorStack.Pop();
                iterator.Reset();
            }

            _activeIteratorList.Add(iterator);
            return iterator;
        }

        private void ReturnIterator(ActionIterator<Action> iterator)
        {
            _activeIteratorList.Remove(iterator);
            _iteratorStack.Push(iterator);
        }
    }

    public class Event<T> where T: struct, IGameEventData
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly LinkedList<Action> _actionList = new();
        private readonly Stack<ActionIterator<Action>> _iteratorStack = new();
        private readonly List<ActionIterator<Action>> _activeIteratorList = new();
        private int _infiniteLoopCheckCount = 0;

        public delegate void Action(in T eventData);

        public bool AddActionFront(Action action)
        {
            if (action == null) return Logger.ErrorReturn(false, $"AddActionFront [{typeof(T).Name}]  action == null") ;
            if (_infiniteLoopCheckCount >= Event.InfiniteLoopCheckLimit) return Logger.ErrorReturn(false, $"AddActionFront [{typeof(T).Name}] {_infiniteLoopCheckCount} >= InfiniteLoopCheckLimit");

            _actionList.AddFirst(action);
            return _infiniteLoopCheckCount < Event.InfiniteLoopCheckLimit;
        }

        public bool AddActionBack(Action action)
        {
            if (action == null) return Logger.ErrorReturn(false, $"AddActionBack [{typeof(T).Name}]  action == null");
            if (_infiniteLoopCheckCount >= Event.InfiniteLoopCheckLimit) return Logger.ErrorReturn(false, $"AddActionBack [{typeof(T).Name}] {_infiniteLoopCheckCount} >= InfiniteLoopCheckLimit");

            LinkedListNode<Action> newNode = _actionList.AddLast(action);
            foreach (ActionIterator<Action> iterator in _activeIteratorList)
                iterator.CurrentNode ??= newNode;

            return _infiniteLoopCheckCount < Event.InfiniteLoopCheckLimit;
        }

        public void RemoveAction(Action action)
        {
            if (action == null)
                return;

            LinkedListNode<Action> nodeToRemove = _actionList.Find(action);
            if (nodeToRemove == null)
                return;

            foreach (ActionIterator<Action> iterator in _activeIteratorList)
            {
                if (iterator.CurrentNode == nodeToRemove)
                    iterator.MoveNext();
            }

            _actionList.Remove(nodeToRemove);
        }

        public void Invoke(T eventData)
        {
            if (_actionList.Count == 0)
                return;

            ActionIterator<Action> iterator = GetIterator();

            while (iterator.CurrentNode != null)
            {
                Action action = iterator.Current;
                iterator.MoveNext();
                action(eventData);
                _infiniteLoopCheckCount++;
            }

            ReturnIterator(iterator);

            if (_activeIteratorList.Count == 0)
                _infiniteLoopCheckCount = 0;
        }

        public void UnregisterCallbacks()
        {
            _actionList.Clear();
        }

        private ActionIterator<Action> GetIterator()
        {
            ActionIterator<Action> iterator;

            if (_iteratorStack.Count == 0)
            {
                iterator = new(_actionList);
            }
            else
            {
                iterator = _iteratorStack.Pop();
                iterator.Reset();
            }

            _activeIteratorList.Add(iterator);
            return iterator;
        }

        private void ReturnIterator(ActionIterator<Action> iterator)
        {
            _activeIteratorList.Remove(iterator);
            _iteratorStack.Push(iterator);
        }
    }

    /// <summary>
    /// Marker inteface for structs that are used as <see cref="Event{T}"/> arguments.
    /// </summary>
    public interface IGameEventData
    {
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
            if (CurrentNode != null)
                CurrentNode = CurrentNode.Next;
        }

        public void Reset()
        {
            CurrentNode = _list.First;
        }
    }
}
