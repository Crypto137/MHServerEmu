using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Manages pools of <see cref="IPoolable"/> instances.
    /// </summary>
    public class ObjectPoolManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<Type, ObjectPool> _poolDict = new();

        /// <summary>
        /// Creates if needed and returns an instance of <typeparamref name="T"/>.
        /// </summary>
        public T Get<T>() where T: IPoolable, new()
        {
            ObjectPool pool = GetOrCreatePool<T>();
            return pool.Get<T>();
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> to the pool for later reuse.
        /// </summary>
        public void Return<T>(T @object) where T: IPoolable, new()
        {
            ObjectPool pool = GetOrCreatePool<T>();
            pool.Return(@object);
        }

        /// <summary>
        /// Create if needed and returns an <see cref="ObjectPool"/> for <typeparamref name="T"/>.
        /// </summary>
        private ObjectPool GetOrCreatePool<T>() where T: IPoolable, new()
        {
            Type type = typeof(T);
            
            if (_poolDict.TryGetValue(type, out ObjectPool pool) == false)
            {
                pool = new();
                _poolDict.Add(type, pool);
            }

            return pool;
        }
    }
}
