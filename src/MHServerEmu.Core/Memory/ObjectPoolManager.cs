using System.Text;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Provides thread-safe access to pools of <see cref="IPoolable"/> instances.
    /// </summary>
    public class ObjectPoolManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<Type, ObjectPool> _poolDict = new();

        public static ObjectPoolManager Instance { get; } = new();

        private ObjectPoolManager() { }

        /// <summary>
        /// Creates if needed and returns an instance of <typeparamref name="T"/>.
        /// </summary>
        public T Get<T>() where T: IPoolable, new()
        {
            lock (_poolDict)
            {
                ObjectPool pool = GetOrCreatePool<T>();
                return pool.Get<T>();
            }
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> to the pool for later reuse.
        /// </summary>
        public void Return<T>(T @object) where T: IPoolable, new()
        {
            lock (_poolDict)
            {
                ObjectPool pool = GetOrCreatePool<T>();
                pool.Return(@object);
            }
        }

        public string GenerateReport()
        {
            StringBuilder sb = new();

            sb.AppendLine("ObjectPoolManager Status");

            lock (_poolDict)
            {
                foreach (var kvp in _poolDict)
                    sb.AppendLine($"{kvp.Key.Name}: {kvp.Value.AvailableCount}/{kvp.Value.TotalCount}");
            }

            return sb.ToString();
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
