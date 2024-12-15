using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Provides a pool of reusable <see cref="List{T}"/> instances, similar to ArrayPool.
    /// </summary>
    public class ListPool<T>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Stack<List<T>> _listStack = new();
        private int _totalCount = 0;

        public static ListPool<T> Instance { get; } = new();

        /// <summary>
        /// Gets a <see cref="List{T}"/> from the pool.
        /// </summary>
        public List<T> Get()
        {
            lock (_listStack)
            {
                if (_listStack.Count == 0)
                {
                    Logger.Trace($"Get(): Created a new instance of List<{typeof(T).Name}> (TotalCount={++_totalCount})");
                    return new();
                }

                return _listStack.Pop();
            }
        }

        /// <summary>
        /// Clears the provided <see cref="List{T}"/> and return it to the pool.
        /// </summary>
        public void Return(List<T> list)
        {
            list.Clear();

            lock (_listStack)
            {
                _listStack.Push(list);
            }
        }
    }
}
