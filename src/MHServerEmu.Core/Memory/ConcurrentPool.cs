using System.Collections.Concurrent;
using System.Diagnostics;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// A thread-safe pool of arbitrary objects intended to be used for cases where retrieval and returns always happen on separate threads.
    /// </summary>
    public class ConcurrentPool<T>
    {
        private readonly ConcurrentStack<T> _items = new();
        private readonly int _maxCount;
        private readonly Func<T> _constructor;

        public ConcurrentPool(int maxCount, Func<T> constructor)
        {
            Debug.Assert(maxCount > 0);

            _maxCount = maxCount;
            _constructor = constructor;
        }

        public T Get()
        {
            if (_items.TryPop(out T item) == false)
                return _constructor();

            return item;
        }

        public void Return(T item)
        {
            if (_items.Count < _maxCount)
                _items.Push(item);
        }
    }
}
