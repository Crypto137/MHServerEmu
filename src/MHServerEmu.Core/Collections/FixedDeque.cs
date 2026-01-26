namespace MHServerEmu.Core.Collections
{
    public class FixedDeque<T>
    {
        private readonly T[] _items;
        private int _first;
        private int _last;
        private readonly int _maxSize;

        public FixedDeque(int size)
        {
            _maxSize = size + 1;
            _items = new T[_maxSize];
            _first = 0;
            _last = 0;
        }

        public int Capacity => _maxSize - 1;
        public int Size => (_last - _first + _maxSize) % _maxSize;
        public bool Empty => _first == _last;
        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
            _first = _last = 0;
        }

        public T this[int index]
        {
            get
            {
                if (index >= Size)
                    throw new IndexOutOfRangeException();
                return _items[(_first + index) % _maxSize];
            }
            set
            {
                if (index >= Size)
                    throw new IndexOutOfRangeException();
                _items[(_first + index) % _maxSize] = value;
            }
        }

        public T Front => _items[_first];
        public T Back => _items[(_last + _maxSize - 1) % _maxSize];

        public void PushBack(T value)
        {
            _items[_last] = value;
            _last = (_last + 1) % _maxSize;
        }

        public void PushFront(T value)
        {
            _first = (_first + _maxSize - 1) % _maxSize;
            _items[_first] = value;
        }

        public T PopBack()
        {
            _last = (_last + _maxSize - 1) % _maxSize;
            T result = _items[_last];
            _items[_last] = default;
            return result;
        }

        public bool TryPopBack(out T value)
        {
            if (Empty)
            {
                value = default;
                return false;
            }
            _last = (_last + _maxSize - 1) % _maxSize;
            value = _items[_last];
            _items[_last] = default;
            return true;
        }

        public T PopFront()
        {
            T result = _items[_first];
            _items[_first] = default;
            _first = (_first + 1) % _maxSize;
            return result;
        }

        public bool TryPopFront(out T value)
        {
            if (Empty)
            {
                value = default;
                return false;
            }
            value = _items[_first];
            _items[_first] = default;
            _first = (_first + 1) % _maxSize;
            return true;
        }
    }
}
