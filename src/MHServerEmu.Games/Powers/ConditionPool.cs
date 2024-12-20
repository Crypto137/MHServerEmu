using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Powers
{
    public sealed class ConditionPool
    {
        private const int MaxCapacity = 2048;

        private static readonly Logger Logger = LogManager.CreateLogger();

        [ThreadStatic]
        private static ConditionPool ThreadInstance;
        public static ConditionPool Instance { get { ThreadInstance ??= new(); return ThreadInstance; } }

        private readonly Stack<Condition> _conditionStack = new(MaxCapacity);

        private int _activeCount = 0;

        private ConditionPool()
        {
            // Pre-allocate conditions for our capacity
            for (int i = 0; i < MaxCapacity; i++)
                _conditionStack.Push(new());
        }

        public Condition Get()
        {
            _activeCount++;

            Logger.Debug($"Get(): activeCount={_activeCount}");

            if (_conditionStack.Count == 0)
            {
                Logger.Warn($"Get(): Created a new condition instance (ActiveCount={_activeCount})");
                return new();
            }

            return _conditionStack.Pop();
        }

        public void Return(Condition condition)
        {
            _activeCount--;

            if (_conditionStack.Count >= MaxCapacity)
                return;

            condition.Clear();
            _conditionStack.Push(condition);
        }
    }
}
