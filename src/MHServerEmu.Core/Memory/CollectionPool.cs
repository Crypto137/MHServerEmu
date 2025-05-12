using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Contains settings for <see cref="CollectionPool{TCollection, TValue}"/> instances.
    /// </summary>
    public static class CollectionPoolSettings
    {
        // NOTE: We use a separate class to have shared settings for various CollectionPool types.

        // For game threads we want to have dedicated pools, in other cases we'll use shared pools with locks
        [ThreadStatic]
        public static bool UseThreadLocalStorage;
    }

    /// <summary>
    /// Provides a pool of reusable <typeparamref name="TCollection"/> instances, similar to ArrayPool.
    /// </summary>
    public abstract class CollectionPool<TCollection, TValue> where TCollection: ICollection<TValue>, new()
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [ThreadStatic]
        private static Node _threadLocalNode;

        private readonly Node _sharedNode = new(false);

        /// <summary>
        /// Retrieves a <typeparamref name="TCollection"/> from the pool or allocates a new one if the pool is empty.
        /// </summary>
        public TCollection Get()
        {
            if (CollectionPoolSettings.UseThreadLocalStorage)
            {
                _threadLocalNode ??= new(true);
                return _threadLocalNode.Get();
            }
            else
            {
                lock (_sharedNode)
                    return _sharedNode.Get();
            }
        }

        /// <summary>
        /// Clears the provided <typeparamref name="TCollection"/> and returns it to the pool.
        /// </summary>
        public void Return(TCollection collection)
        {
            if (CollectionPoolSettings.UseThreadLocalStorage)
            {
                // Thread-static node should have already been allocated in Get()
                _threadLocalNode.Return(collection);
            }
            else
            {
                lock (_sharedNode)
                    _sharedNode.Return(collection);
            }
        }

        /// <summary>
        /// Represents a storage unit of a pool or a particular type.
        /// </summary>
        private class Node
        {
            private readonly Stack<TCollection> _collectionStack = new();
            private readonly int _threadId = -1;

            private int _totalCount = 0;

            public Node(bool isThreadLocal)
            {
                if (isThreadLocal)
                    _threadId = Environment.CurrentManagedThreadId;
            }

            /// <summary>
            /// Retrieves a <typeparamref name="TCollection"/> from the node or allocates a new one if the node is empty.
            /// </summary>
            public TCollection Get()
            {
                if (_collectionStack.Count == 0)
                {
                    Logger.Trace($"Get(): Created a new instance of {typeof(TCollection).Name}<{typeof(TValue).Name}> (ThreadId={_threadId}, TotalCount={++_totalCount})");
                    return new();
                }

                return _collectionStack.Pop();
            }

            /// <summary>
            /// Clears the provided <typeparamref name="TCollection"/> and returns it to the node.
            /// </summary>
            public void Return(TCollection collection)
            {
                collection.Clear();
                _collectionStack.Push(collection);
            }
        }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="List{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class ListPool<T> : CollectionPool<List<T>, T>
    {
        public static ListPool<T> Instance { get; } = new();

        private ListPool() { }

        /// <summary>
        /// Retrieves a <typeparamref name="TCollection"/> from the pool or allocates a new one if the pool is empty and ensures it has the specified capacity.
        /// </summary>
        public List<T> Get(int capacity)
        {
            List<T> list = Get();
            list.EnsureCapacity(capacity);
            return list;
        }

        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool or allocates a new one if the pool is empty and copies all elements from collection.
        /// </summary>
        public List<T> Get(IEnumerable<T> collection)
        {
            List<T> list = Get();
            list.AddRange(collection);
            return list;
        }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="Dictionary{TKey, TValue}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class DictionaryPool<TKey, TValue> : CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
    {
        public static DictionaryPool<TKey, TValue> Instance { get; } = new();

        private DictionaryPool() { }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="HashSet{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class HashSetPool<T> : CollectionPool<HashSet<T>, T>
    {
        public static HashSetPool<T> Instance { get; } = new();

        private HashSetPool() { }
    }
}
