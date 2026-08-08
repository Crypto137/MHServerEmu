namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Contains settings for <see cref="GenericPool{T}"/> instances.
    /// </summary>
    public static class GenericPoolSettings
    {
        [ThreadStatic]
        public static bool UseThreadLocalStorage;
    }

    /// <summary>
    /// Pools instances of arbitrary types that implement <see cref="IPoolable"/>.
    /// </summary>
    /// <remarks>
    /// This is intended to be used for objects with short lifetimes (typically within a scope).
    /// If you need to pool objects with long lifetimes, create a specialized implementation of <see cref="ObjectPool{T}"/>.
    /// </remarks>
    public class GenericPool<T> where T: class, IPoolable, new()
    {
        private static readonly GenericPoolImpl _sharedPool = new(ObjectPoolFlags.None);

        [ThreadStatic]
        private static GenericPoolImpl _threadLocalPool;

        public static T Get()
        {
            if (GenericPoolSettings.UseThreadLocalStorage)
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

        public static ObjectPoolHandle<T> Get(out T instance)
        {
            if (GenericPoolSettings.UseThreadLocalStorage)
            {
                _threadLocalPool ??= new(ObjectPoolFlags.ThreadLocal);
                return _threadLocalPool.Get(out instance);
            }
            else
            {
                lock (_sharedPool)
                    return _sharedPool.Get(out instance);
            }
        }

        public static void Return(T instance)
        {
            if (GenericPoolSettings.UseThreadLocalStorage)
            {
                _threadLocalPool.Return(instance);
            }
            else
            {
                lock (_sharedPool)
                    _sharedPool.Return(instance);
            }
        }

        private sealed class GenericPoolImpl : ObjectPool<T>
        {
            public GenericPoolImpl(ObjectPoolFlags flags) : base(flags) { }

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
