namespace MHServerEmu.Games.Common
{
    public class FixedPriorityQueue<T> where T : IComparable<T>
    {
        private List<T> _items;

        public FixedPriorityQueue()
        {
            _items = new List<T>();
        }

        public int Count => _items.Count;

        public void Push(T item)
        {
            _items.Add(item);
            _items.Sort();
        }

        public T Top()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            return _items[0];
        }

        public T Pop()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            T topItem = _items[0];
            _items.RemoveAt(0);
            return topItem;
        }

        public void Heapify()
        {
            _items.Sort();
        }
    }
}
