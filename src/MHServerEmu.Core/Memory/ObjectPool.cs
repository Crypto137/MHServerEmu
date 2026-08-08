using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    public enum ObjectPoolFlags
    {
        None            = 0,
        ThreadLocal     = 1 << 0,
    }

    public abstract class ObjectPool<T> where T: class
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<T> _instances = new();
#if DEBUG
        private readonly HashSet<T> _activeInstances = new();
#endif
        private readonly ObjectPoolFlags _flags;
        private readonly int _threadId;

        private int _totalAllocatedCount = 0;
        private T _lastReturnedInstance = null;

        public ObjectPool(ObjectPoolFlags flags = ObjectPoolFlags.None)
        {
            _flags = flags;
            _threadId = _flags.HasFlag(ObjectPoolFlags.ThreadLocal) ? Environment.CurrentManagedThreadId : -1;
        }

        public T Get()
        {
            T instance;

            if (_lastReturnedInstance != null)
            {
                instance = _lastReturnedInstance;
                _lastReturnedInstance = null;
            }
            else if (_instances.Count == 0)
            {
                instance = Allocate();
                _totalAllocatedCount++;
                Logger.Trace($"Get(): Created a new instance of {typeof(T)} (ThreadId={_threadId}, TotalCount={_totalAllocatedCount})");
            }
            else
            {
                int index = _instances.Count - 1;
                instance = _instances[index];
                _instances.RemoveAt(index);
            }

#if DEBUG
            if (_activeInstances.Add(instance) == false)
                throw new Exception($"Attempted to get an active instance of {typeof(T).Name} from the pool.");
#endif

            OnGet(instance);
            return instance;
        }

        public ObjectPoolHandle<T> Get(out T instance)
        {
            instance = Get();
            return new(this, instance);
        }

        public void Return(T instance)
        {
#if DEBUG
            if (_activeInstances.Remove(instance) == false)
                throw new Exception($"Attempted to return an inactive instance of {typeof(T).Name} to the pool.");
#endif

            OnReturn(instance);

            if (_lastReturnedInstance == null)
                _lastReturnedInstance = instance;
            else
                _instances.Add(instance);
        }

        protected abstract T Allocate();

        protected virtual void OnGet(T instance) { }

        protected virtual void OnReturn(T instance) { }
    }

    public readonly struct ObjectPoolHandle<T> : IDisposable where T: class
    {
        private readonly ObjectPool<T> _pool;
        private readonly T _instance;

        public ObjectPoolHandle(ObjectPool<T> pool, T instance)
        {
            _pool = pool;
            _instance = instance;
        }

        public void Dispose()
        {
            _pool.Return(_instance);
        }
    }
}
