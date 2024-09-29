using System.Collections;

namespace MHServerEmu.Core.Collections
{
    /// <summary>
    /// A collection of <typeparamref name="T"/> with a fixed size that loops back and overwrites the oldest element when it reaches the end. 
    /// </summary>
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _data;
        private int _count;
        private int _position;

        public int Capacity { get => _data.Length; }
        public int Count { get => _count; }

        /// <summary>
        /// Constructs a new <see cref="CircularBuffer{T}"/> with the specified capacity.
        /// </summary>
        public CircularBuffer(int capacity)
        {
            if (capacity <= 0 ) throw new ArgumentException("Capacity must be > 0.");

            _data = new T[capacity];
        }

        public void Add(T value)
        {
            _data[_position] = value;
            _count = Math.Min(++_count, _data.Length);

            _position++;
            if (_position >= _data.Length)
                _position = 0;
        }

        public void Clear()
        {
            Array.Clear(_data);
            _count = 0;
            _position = 0;
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly CircularBuffer<T> _buffer;
            private int _index;

            public T Current { get; private set; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(CircularBuffer<T> buffer)
            {
                _buffer = buffer;
                _index = -1;

                Current = default;
            }

            public bool MoveNext()
            {
                Current = default;

                while (++_index < _buffer._count)
                {
                    Current = _buffer._data[_index];
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose()
            {
            }
        }

    }
}
