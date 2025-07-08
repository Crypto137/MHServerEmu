namespace MHServerEmu.Core.Collections
{
    /// <summary>
    /// Represents a high-performance concurrent FIFO collection of <typeparamref name="T"/> implemented using the double buffer pattern. 
    /// </summary>
    /// <remarks>
    /// Items are enqueued to a "pending" queue, and dequeued from a "current" queue. These two queues can be swapped to access pending elements.
    /// Enqueueing and swapping is thread-safe, dequeueing is not. The current queue is not cleared when swapping, it's up to you to ensure it's empty.
    /// </remarks>
    public class DoubleBufferQueue<T>
    {
        private Queue<T> _pendingQueue = new();
        private Queue<T> _currentQueue = new();

        private SpinLock _lock = new(false);

        /// <summary>
        /// Gets the number of items in the pending queue.
        /// </summary>
        public int PendingCount { get => _pendingQueue.Count; }

        /// <summary>
        /// Gets the number of items in the current queue.
        /// </summary>
        public int CurrentCount { get => _currentQueue.Count; }

        /// <summary>
        /// Enqueues an item to the pending queue.
        /// </summary>
        public void Enqueue(T item)
        {
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                _pendingQueue.Enqueue(item);
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit(false);
            }
        }

        /// <summary>
        /// Dequeues an item from the current queue.
        /// </summary>
        public T Dequeue()
        {
            return _currentQueue.Dequeue();
        }

        /// <summary>
        /// Swap pending and current queues to allow access to pending items via <see cref="Dequeue"/>.
        /// </summary>
        public void Swap()
        {
            // We intentionally do not check if the current queue is empty here for simplicity and performance.

            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                (_pendingQueue, _currentQueue) = (_currentQueue, _pendingQueue);
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit(false);
            }
        }
    }
}
