namespace MHServerEmu.Core.Collections
{
    /// <summary>
    /// A modification of <see cref="Stack{T}"/> that implements <see cref="ICollection{T}"/> for compatibility with our CollectionPool implementation.
    /// </summary>
    public sealed class PoolableStack<T> : Stack<T>, ICollection<T>
    {
        public bool IsReadOnly { get => false; }

        [Obsolete("This method is not supported.")]
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        [Obsolete("This method is not supported.")]
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }
    }
}
