using System.Buffers;

namespace MHServerEmu.Core.Memory
{
    /// <summary>
    /// A wrapper around <see cref="Span{T}"/> for automatically slicing and disposing memory rented from <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <remarks>
    /// This is similar to SpanOwner from the .NET Community Toolkit.
    /// </remarks>
    public readonly ref struct PoolSpan<T>
    {
        private readonly ArrayPool<T> _pool;
        private readonly T[] _buffer;
        private readonly bool _clearOnDispose;

        public Span<T> Span { get; }

        public int Length { get => Span.Length; }
        public T this[int index] { get => Span[index]; set => Span[index] = value; }

        private PoolSpan(ArrayPool<T> pool, T[] buffer, int length, bool clearOnDispose)
        {
            _pool = pool;
            _buffer = buffer;
            Span = buffer.AsSpan(0, length);
            _clearOnDispose = clearOnDispose;
        }

        public static PoolSpan<T> Allocate(int length, bool clearOnDispose, ArrayPool<T> pool)
        {
            T[] buffer = pool.Rent(length);
            return new(pool, buffer, length, clearOnDispose);
        }

        public static PoolSpan<T> Allocate(int length, bool clearOnDispose = true)
        {
            return Allocate(length, clearOnDispose, ArrayPool<T>.Shared);
        }

        public Span<T>.Enumerator GetEnumerator()
        {
            return Span.GetEnumerator();
        }

        public void Clear()
        {
            Span.Clear();
        }

        public void Dispose()
        {
            _pool.Return(_buffer, _clearOnDispose);
        }
    }
}
