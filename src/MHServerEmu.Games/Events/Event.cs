namespace MHServerEmu.Games.Events
{
    public class Event
    {        
        // TODO write worked Event and replace Action
        private readonly List<Action> _actionList = new();

        public void AddActionBack(Action handler)
        {
            _actionList.Add(handler);
        }

        public void Invoke()
        {
            foreach(var action in _actionList)
                action();
        }

        public void UnregisterCallbacks()
        {
            _actionList.Clear();
        }

    }
}
