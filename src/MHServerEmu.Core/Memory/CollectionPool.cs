using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Provides a pool of reusable <typeparamref name="TCollection"/> instances, similar to ArrayPool.
    /// </summary>
    public abstract class CollectionPool<TCollection, TValue> where TCollection: ICollection<TValue>, new()
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Stack<TCollection> _collectionStack = new();
        private int _totalCount = 0;

        /// <summary>
        /// Retrieves a <typeparamref name="TCollection"/> from the pool or allocates a new one if the pool is empty.
        /// </summary>
        public TCollection Get()
        {
            lock (_collectionStack)
            {
                if (_collectionStack.Count == 0)
                {
                    Logger.Trace($"Get(): Created a new instance of {typeof(TCollection).Name}<{typeof(TValue).Name}> (TotalCount={++_totalCount})");
                    return new();
                }

                return _collectionStack.Pop();
            }
        }

        /// <summary>
        /// Clears the provided <typeparamref name="TCollection"/> and returns it to the pool.
        /// </summary>
        public void Return(TCollection collection)
        {
            collection.Clear();

            lock (_collectionStack)
            {
                _collectionStack.Push(collection);
            }
        }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="List{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public class ListPool<T> : CollectionPool<List<T>, T>
    {
        public static ListPool<T> Instance { get; } = new();
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="Dictionary{TKey, TValue}"/> instances, similar to ArrayPool.
    /// </summary>
    public class DictionaryPool<TKey, TValue> : CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
    {
        public static DictionaryPool<TKey, TValue> Instance { get; } = new();
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="HashSet{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public class HashSetPool<T> : CollectionPool<HashSet<T>, T>
    {
        public static HashSetPool<T> Instance { get; } = new();
    }
}
