namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Interface for objects that can be stored in an <see cref="ObjectPool"/>.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// A flag to guard against returning the same <see cref="IPoolable"/> instance to the pool multiple times.
        /// </summary>
        public bool IsInPool { get; set; }

        /// <summary>
        /// Resets an <see cref="IPoolable"/> instance before it returns to the pool.
        /// </summary>
        public void ResetForPool();
    }
}
