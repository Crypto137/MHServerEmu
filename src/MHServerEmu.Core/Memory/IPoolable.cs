namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Interface for objects that can be stored in an <see cref="ObjectPool"/>.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Resets an <see cref="IPoolable"/> instance before it returns to the pool.
        /// </summary>
        public void ResetForPool();
    }
}
