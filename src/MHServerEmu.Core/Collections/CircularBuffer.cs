using System.Collections;

namespace MHServerEmu.Core.Collections
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _data;
        private int _count;
        private int _position;

        public int Capacity { get => _data.Length; }
        public int Count { get => _count; }

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

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _data[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
