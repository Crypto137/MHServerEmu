using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Stores <see cref="IPoolable"/> for later reuse.
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
            if (AvailableCount == 0)
            {
                T @object = new();

                TotalCount++;
                Logger.Trace($"Get<T>(): Created a new instance of {typeof(T).Name} (TotalCount={TotalCount})");

                return @object;
            }

            return (T)_objects.Pop();
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> to the pool for later reuse.
        /// </summary>
        public void Return<T>(T @object) where T: IPoolable, new()
        {
            @object.Clear();
            _objects.Push(@object);
        }
    }
}
