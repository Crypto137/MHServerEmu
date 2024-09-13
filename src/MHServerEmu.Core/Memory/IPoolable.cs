namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// Interface for objects that can be stored in an <see cref="ObjectPool"/>.
    /// </summary>
    public interface IPoolable
    {
        public void Clear();
    }
}
