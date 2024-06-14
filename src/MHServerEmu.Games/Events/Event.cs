namespace MHServerEmu.Games.Events
{
    public class Event
    {        
        // TODO write worked Event and replace Action
        private readonly List<Action> _actionList = new();
        public void AddActionFront(Action handler) => _actionList.Insert(0, handler);
        public void AddActionBack(Action handler) => _actionList.Add(handler);
        public void RemoveAction(Action handler) => _actionList.Remove(handler);

        public void Invoke()
        {
            foreach(var action in _actionList)
                action();
        }

        public void UnregisterCallbacks() => _actionList.Clear();

    }

    public class Event<T>
    {
        private readonly List<Action<T>> _actionList = new();
        public void AddActionFront(Action<T> handler) => _actionList.Insert(0, handler);
        public void AddActionBack(Action<T> handler) => _actionList.Add(handler);
        public void RemoveAction(Action<T> handler) => _actionList.Remove(handler);
        public void Invoke(T eventType)
        {
            foreach (var action in _actionList)
                action(eventType);
        }
        public void UnregisterCallbacks() => _actionList.Clear();
    }
}
