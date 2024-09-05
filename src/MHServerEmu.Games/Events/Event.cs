namespace MHServerEmu.Games.Events
{
    public class Event
    {        
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
            foreach (var action in _actionList.ToList())
                action?.Invoke();
        }

        public void UnregisterCallbacks() => _actionList.Clear();

    }

    public class Event<T>
    {
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
            foreach (var action in _actionList.ToList())
                action?.Invoke(eventType);
        }

        public void UnregisterCallbacks() => _actionList.Clear();
    }
}
