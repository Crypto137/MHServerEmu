using static MHServerEmu.Games.Powers.PowerPrototypes;

namespace MHServerEmu.Games.Common
{
    public class FixedPriorityQueue<T> where T : IComparable<T>
    {
        private readonly List<T> _items = new();
        public bool Empty => _items.Count == 0;
        public int Count => _items.Count;
        public T Top => _items[0];

        public void Push(T value)
        {
            _items.Add(value);
            int index = _items.Count - 1;
            HeapifyUp(index);
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_items[index].CompareTo(_items[parentIndex]) > 0)
                    Swap(index, parentIndex);
                else break;
                index = parentIndex;
            }
        }

        private void HeapifyDown(int index)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int maxIndex = index;

            if (left < _items.Count && _items[left].CompareTo(_items[maxIndex]) > 0)
                maxIndex = left;
            if (right < _items.Count && _items[right].CompareTo(_items[maxIndex]) > 0)
                maxIndex = right;

            if (maxIndex != index)
            {
                Swap(index, maxIndex);
                HeapifyDown(maxIndex);
            }
        }

        public void Pop()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            _items[0] = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            HeapifyDown(0);
        }

        public void Clear() => _items.Clear();

        public void Heapify()
        {
            for (int i = _items.Count / 2 - 1; i >= 0; i--)
                HeapifyDown(i);
        }

        private void Swap(int index1, int index2)
        {
            (_items[index2], _items[index1]) = (_items[index1], _items[index2]);
        }
    }
}
