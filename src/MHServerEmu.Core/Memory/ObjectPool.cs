using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Stores <see cref="IPoolable"/> instances for later reuse.
    /// </summary>
    public class ObjectPool
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Stack<IPoolable> _objects = new();

        public int TotalCount { get; private set; }
        public int AvailableCount { get => _objects.Count; }

        /// <summary>
        /// Creates if needed and returns an instance of <typeparamref name="T"/>.
        /// </summary>
        public T Get<T>() where T: IPoolable, new()
        {
            T @object;

            if (AvailableCount == 0)
            {
                @object = new();

                TotalCount++;
                Logger.Trace($"Get<T>(): Created a new instance of {typeof(T).Name} (TotalCount={TotalCount})");
            }
            else
            {
                @object = (T)_objects.Pop();
                @object.IsInPool = false;
            }

            return @object;
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> to the pool for later reuse.
        /// </summary>
        public bool Return<T>(T @object) where T: IPoolable, new()
        {
            if (@object.IsInPool)
                return Logger.WarnReturn(false, $"Return<T>(): Attempted to return an instance of {typeof(T).Name} that is already in a pool!");

            @object.ResetForPool();
            @object.IsInPool = true;
            _objects.Push(@object);
            return true;
        }
    }
}
