using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Contains settings for <see cref="CollectionPool{TCollection, TValue}"/> instances.
    /// </summary>
    public static class CollectionPoolSettings
    {
        // NOTE: We use a separate class to have shared settings for various CollectionPool types.

        // For game threads we want to have dedicated pools, in other cases we use shared pools with locks
        [ThreadStatic]
        public static bool UseThreadLocalStorage;
    }

    /// <summary>
    /// Provides a pool of reusable <typeparamref name="TCollection"/> instances, similar to ArrayPool.
    /// </summary>
    public class CollectionPool<TCollection, TValue> where TCollection: ICollection<TValue>, new()
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
        /// Retrieves a <typeparamref name="TCollection"/> from the pool or allocates a new one if the pool is empty.
        /// Returns a <see cref="CollectionHandle"/> that can automatically return the <typeparamref name="TCollection"/> instance to the pool when it goes out of scope.
        /// </summary>
        public CollectionHandle Get(out TCollection collection)
        {
            collection = Get();
            return new(this, collection);
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
        /// A handle that implements <see cref="IDisposable"/> that can automatically return a <typeparamref name="TCollection"/> instance to the pool when it goes out of scope.
        /// </summary>
        public readonly struct CollectionHandle : IDisposable
        {
            private readonly CollectionPool<TCollection, TValue> _pool;
            private readonly TCollection _collection;

            public CollectionHandle(CollectionPool<TCollection, TValue> pool, TCollection collection)
            {
                _pool = pool;
                _collection = collection;
            }

            public void Dispose()
            {
                _pool.Return(_collection);
            }
        }

        /// <summary>
        /// Represents a storage unit of a pool of a particular type.
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

    // REMOVEME - get rid of this once unified pools are implemented
    public abstract class CollectionPoolImpl<TCollection, TValue> where TCollection: ICollection<TValue>, new()
    {
        private static readonly CollectionPool<TCollection, TValue> _pool = new();

        public static TCollection Get()
        {
            return _pool.Get();
        }

        public static CollectionPool<TCollection, TValue>.CollectionHandle Get(out TCollection instance)
        {
            return _pool.Get(out instance);
        }

        public static void Return(TCollection instance)
        {
            _pool.Return(instance);
        }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="List{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public class ListPool<T> : CollectionPoolImpl<List<T>, T>
    {
        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool or allocates a new one if the pool is empty and ensures it has the specified capacity.
        /// </summary>
        public static List<T> Get(int capacity)
        {
            List<T> list = Get();
            list.EnsureCapacity(capacity);
            return list;
        }

        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool or allocates a new one if the pool is empty and ensures it has the specified capacity.
        /// Returns a <see cref="CollectionPool{TCollection, TValue}.CollectionHandle"/> that can automatically return the <see cref="List{T}"/>
        /// instance to the pool when it goes out of scope.
        /// </summary>
        public static CollectionPool<List<T>, T>.CollectionHandle Get(int capacity, out List<T> list)
        {
            var handle = Get(out list);
            list.EnsureCapacity(capacity);
            return handle;
        }

        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool or allocates a new one if the pool is empty and copies all elements from the provided <see cref="IEnumerable{T}"/> collection.
        /// </summary>
        public static List<T> Get(IEnumerable<T> collection)
        {
            List<T> list = Get();
            list.AddRange(collection);
            return list;
        }

        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool or allocates a new one if the pool is empty and copies all elements from the provided <see cref="IEnumerable{T}"/> collection.
        /// Returns a <see cref="CollectionPool{TCollection, TValue}.CollectionHandle"/> that can automatically return the <see cref="List{T}"/> instance to the pool when it goes out of scope.
        /// </summary>
        public static CollectionPool<List<T>, T>.CollectionHandle Get(IEnumerable<T> collection, out List<T> list)
        {
            var handle = Get(out list);
            list.AddRange(collection);
            return handle;
        }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="Dictionary{TKey, TValue}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class DictionaryPool<TKey, TValue> : CollectionPoolImpl<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
    {
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="HashSet{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class HashSetPool<T> : CollectionPoolImpl<HashSet<T>, T>
    {
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="PoolableStack{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class StackPool<T> : CollectionPoolImpl<PoolableStack<T>, T>
    {
    }
}
