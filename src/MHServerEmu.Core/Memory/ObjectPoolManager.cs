namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Provides a pool of reusable instances that implement <see cref="IPoolable"/> and <see cref="IDisposable"/>.
    /// </summary>
    public class ObjectPoolManager
    {
        // TODO: Rename this to GenericPool and change the API to match CollectionPool.

        [ThreadStatic]
        private static Dictionary<Type, object> _threadLocalPools;

        [ThreadStatic]
        public static bool UseThreadLocalStorage;

        private readonly Dictionary<Type, object> _sharedPools = new();

        public static ObjectPoolManager Instance { get; } = new();

        private ObjectPoolManager() { }

        /// <summary>
        /// Creates if needed and returns an instance of <typeparamref name="T"/>.
        /// </summary>
        public T Get<T>() where T: class, IPoolable, IDisposable, new()
        {
            if (UseThreadLocalStorage)
            {
                _threadLocalPools ??= new();
                GenericPoolImpl<T> pool = GetOrCreatePool<T>(_threadLocalPools);
                return pool.Get();
            }
            else
            {
                lock (_sharedPools)
                {
                    GenericPoolImpl<T> pool = GetOrCreatePool<T>(_sharedPools);
                    return pool.Get();
                }
            }
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> to the pool for later reuse.
        /// </summary>
        public void Return<T>(T @object) where T: class, IPoolable, IDisposable, new()
        {
            if (UseThreadLocalStorage)
            {
                // Thread local dict should have already been initialized in a Get() call
                GenericPoolImpl<T> pool = GetOrCreatePool<T>(_threadLocalPools);
                pool.Return(@object);
            }
            else
            {
                lock (_sharedPools)
                {
                    GenericPoolImpl<T> pool = GetOrCreatePool<T>(_sharedPools);
                    pool.Return(@object);
                }
            }
        }

        /// <summary>
        /// Create if needed and returns an <see cref="OldObjectPool"/> for <typeparamref name="T"/>.
        /// </summary>
        private static GenericPoolImpl<T> GetOrCreatePool<T>(Dictionary<Type, object> pools) where T: class, IPoolable, IDisposable, new()
        {
            Type type = typeof(T);
            
            if (pools.TryGetValue(type, out object pool) == false)
            {
                ObjectPoolFlags flags = UseThreadLocalStorage ? ObjectPoolFlags.ThreadLocal : ObjectPoolFlags.None;
                pool = new GenericPoolImpl<T>(flags);
                pools.Add(type, pool);
            }

            return (GenericPoolImpl<T>)pool;
        }

        private sealed class GenericPoolImpl<T> : ObjectPool<T> where T : class, IPoolable, new()
        {
            public GenericPoolImpl(ObjectPoolFlags flags) : base(flags)
            {
            }

            protected override T Allocate()
            {
                return new();
            }

            protected override void OnReturn(T instance)
            {
                instance.ResetForPool();
            }
        }
    }
}
