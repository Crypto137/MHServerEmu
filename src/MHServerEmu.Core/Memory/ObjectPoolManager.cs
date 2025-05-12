using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Provides a pool of reusable instances that implement <see cref="IPoolable"/> and <see cref="IDisposable"/>.
    /// </summary>
    public class ObjectPoolManager
    {
        // NOTE: Looking back, ObjectPoolManager was a poor name for this,
        // because this is actually a pool of things that implement both
        // IPoolable and IDisposable. At this point it would require renaming
        // too much stuff across the entire codebase, so we're sticking with for now.

        // If we ever decide to rename it, it should be called DisposablePool or something.

        private static readonly Logger Logger = LogManager.CreateLogger();

        [ThreadStatic]
        private static Dictionary<Type, ObjectPool> _threadLocalPoolDict;

        [ThreadStatic]
        public static bool UseThreadLocalStorage;

        private readonly Dictionary<Type, ObjectPool> _sharedPoolDict = new();

        public static ObjectPoolManager Instance { get; } = new();

        private ObjectPoolManager() { }

        /// <summary>
        /// Creates if needed and returns an instance of <typeparamref name="T"/>.
        /// </summary>
        public T Get<T>() where T: IPoolable, IDisposable, new()
        {
            if (UseThreadLocalStorage)
            {
                _threadLocalPoolDict ??= new();
                ObjectPool pool = GetOrCreatePool<T>(_threadLocalPoolDict);
                return pool.Get<T>();
            }
            else
            {
                lock (_sharedPoolDict)
                {
                    ObjectPool pool = GetOrCreatePool<T>(_sharedPoolDict);
                    return pool.Get<T>();
                }
            }
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> to the pool for later reuse.
        /// </summary>
        public void Return<T>(T @object) where T: IPoolable, IDisposable, new()
        {
            if (UseThreadLocalStorage)
            {
                // Thread local dict should have already been initialized in a Get() call
                ObjectPool pool = GetOrCreatePool<T>(_threadLocalPoolDict);
                pool.Return(@object);
            }
            else
            {
                lock (_sharedPoolDict)
                {
                    ObjectPool pool = GetOrCreatePool<T>(_sharedPoolDict);
                    pool.Return(@object);
                }
            }
        }

        /// <summary>
        /// Create if needed and returns an <see cref="ObjectPool"/> for <typeparamref name="T"/>.
        /// </summary>
        private static ObjectPool GetOrCreatePool<T>(Dictionary<Type, ObjectPool> poolDict) where T: IPoolable, IDisposable, new()
        {
            Type type = typeof(T);
            
            if (poolDict.TryGetValue(type, out ObjectPool pool) == false)
            {
                pool = new(UseThreadLocalStorage);
                poolDict.Add(type, pool);
            }

            return pool;
        }
    }
}
