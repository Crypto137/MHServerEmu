using MHServerEmu.Core.Collections;

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

    public abstract class CollectionPool<TCollection, TValue> where TCollection: class, ICollection<TValue>, new()
    {
        private static readonly CollectionPoolImpl _sharedPool = new(ObjectPoolFlags.None);

        [ThreadStatic]
        private static CollectionPoolImpl _threadLocalPool;

        public static TCollection Get()
        {
            if (CollectionPoolSettings.UseThreadLocalStorage)
            {
                _threadLocalPool ??= new(ObjectPoolFlags.ThreadLocal);
                return _threadLocalPool.Get();
            }
            else
            {
                lock (_sharedPool)
                    return _sharedPool.Get();
            }
        }

        public static ObjectPoolHandle<TCollection> Get(out TCollection collection)
        {
            if (CollectionPoolSettings.UseThreadLocalStorage)
            {
                _threadLocalPool ??= new(ObjectPoolFlags.ThreadLocal);
                return _threadLocalPool.Get(out collection);
            }
            else
            {
                lock (_sharedPool)
                    return _sharedPool.Get(out collection);
            }
        }

        public static void Return (TCollection collection)
        {
            if (CollectionPoolSettings.UseThreadLocalStorage)
            {
                _threadLocalPool.Return(collection);
            }
            else
            {
                lock (_sharedPool)
                    _sharedPool.Return(collection);
            }
        }

        private sealed class CollectionPoolImpl : ObjectPool<TCollection>
        {
            public CollectionPoolImpl(ObjectPoolFlags flags) : base(flags) { }

            protected override TCollection Allocate()
            {
                return new();
            }

            protected override void OnReturn(TCollection instance)
            {
                instance.Clear();
            }
        }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="List{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public class ListPool<T> : CollectionPool<List<T>, T>
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
        public static ObjectPoolHandle<List<T>> Get(int capacity, out List<T> list)
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
        public static ObjectPoolHandle<List<T>> Get(IEnumerable<T> collection, out List<T> list)
        {
            var handle = Get(out list);
            list.AddRange(collection);
            return handle;
        }
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="Dictionary{TKey, TValue}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class DictionaryPool<TKey, TValue> : CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
    {
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="HashSet{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class HashSetPool<T> : CollectionPool<HashSet<T>, T>
    {
    }

    /// <summary>
    /// Provides a pool of reusable <see cref="PoolableStack{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public sealed class StackPool<T> : CollectionPool<PoolableStack<T>, T>
    {
    }
}
