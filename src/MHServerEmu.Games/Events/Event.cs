using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public class Event
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly LinkedList<Action> _actionList = new();
        public void AddActionFront(Action handler) => _actionList.AddFirst(handler);
        public void AddActionBack(Action handler) => _actionList.AddLast(handler);

        public void RemoveAction(Action handler)
        {
            var node = _actionList.Find(handler);
            if (node != null)
                _actionList.Remove(node);
        }

        public void Invoke()
        {
            var node = _actionList.First;
            var validNode = node;
            var prevNode = node?.Previous;
            int index = 0;
            while (node != null)
            {
                node.Value.Invoke();

                // Get valid node
                if (node.List == _actionList)
                {
                    validNode = node;
                    prevNode = node.Previous;
                }

                // Get valid next node
                if (node.Next != null)
                    node = node.Next;
                else if (validNode?.List == _actionList)
                    node = validNode?.Next;
                else
                    node = prevNode?.Next;

                index++;
            }
            if (index < _actionList.Count) Logger.Error($"Invoke is broken after [{index}] in Event");
        }

        public void UnregisterCallbacks() => _actionList.Clear();

    }

    public class Event<T>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly LinkedList<Action<T>> _actionList = new();
        public void AddActionFront(Action<T> handler) => _actionList.AddFirst(handler);
        public void AddActionBack(Action<T> handler) => _actionList.AddLast(handler);

        public void RemoveAction(Action<T> handler)
        {
            var node = _actionList.Find(handler);
            if (node != null)
                _actionList.Remove(node);
        }

        public void Invoke(T eventType)
        {
            var node = _actionList.First;
            var validNode = node;
            var prevNode = node?.Previous;
            int index = 0;
            while (node != null)
            {
                node.Value.Invoke(eventType);

                // Get valid node
                if (node.List == _actionList)
                {
                    validNode = node;
                    prevNode = node.Previous;
                }

                // Get valid next node
                if (node.Next != null)
                    node = node.Next;
                else if (validNode?.List == _actionList)
                    node = validNode?.Next;
                else
                    node = prevNode?.Next;

                index++;
            }
            if (index < _actionList.Count) Logger.Error($"Invoke is broken after [{index}] in Event<{typeof(T).Name}>");
        }

        public void UnregisterCallbacks() => _actionList.Clear();
    }
}
