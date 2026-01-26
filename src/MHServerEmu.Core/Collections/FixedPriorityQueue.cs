namespace MHServerEmu.Core.Collections
{
    public readonly struct FixedPriorityQueue<T> where T : IComparable<T>
    {
        // Our implementation is just a List<T> wrapper with no data of its own, so we can get away with making it a readonly struct.
        // If this ever needs to stop being the case, turn it into a fully featured ICollection<T> implementation and pool it.

        private readonly List<T> _items;

        public bool Empty => _items.Count == 0;
        public int Count => _items.Count;
        public T Top => _items[0];

        public FixedPriorityQueue(int capacity) 
        { 
            _items = new(capacity); 
        }

        public FixedPriorityQueue(List<T> items)
        {
            _items = items;
        }

        public void Push(T value)
        {
            _items.Add(value);
            HeapFunctions.PushHeap(_items);
        }

        public void Pop()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            HeapFunctions.PopHeap(_items);
            _items.RemoveAt(_items.Count - 1);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public void Heapify()
        {
            HeapFunctions.MakeHeap(_items);
        }
    }

    public static class HeapFunctions
    {
        public static void PushHeap<T>(List<T> list) where T : IComparable<T>
        {
            int last = list.Count - 1;
            int holeIndex = last;
            T value = list[last];
            PushHeapIndex(list, holeIndex, 0, value);
        }

        private static void PushHeapIndex<T>(List<T> list, int holeIndex, int topIndex, T value) where T : IComparable<T>
        {
            int parent = (holeIndex - 1) / 2;
            while (holeIndex > topIndex && list[parent].CompareTo(value) < 0)
            {
                list[holeIndex] = list[parent];
                holeIndex = parent;
                parent = (holeIndex - 1) / 2;
            }
            list[holeIndex] = value;
        }

        public static void PopHeap<T>(List<T> list) where T : IComparable<T>
        {
            if (list.Count > 1)
            {
                int last = list.Count - 1;
                T value = list[last];
                list[last] = list[0];
                AdjustHeap(list, 0, last, value);
            }
        }

        public static void MakeHeap<T>(List<T> list) where T : IComparable<T>
        {
            if (list.Count < 2) return;

            int len = list.Count;
            int parent = (len - 2) / 2;
            while (true)
            {
                T value = list[parent];
                AdjustHeap(list, parent, len, value);
                if (parent == 0) return;
                parent--;
            }
        }

        private static void AdjustHeap<T>(List<T> list, int holeIndex, int len, T value) where T : IComparable<T>
        {
            int topIndex = holeIndex;
            int secondChild = 2 * (holeIndex + 1);
            while (secondChild < len)
            {
                if (list[secondChild].CompareTo(list[secondChild - 1]) < 0)
                    secondChild--;
                list[holeIndex] = list[secondChild];
                holeIndex = secondChild;
                secondChild = 2 * (secondChild + 1);
            }
            if (secondChild == len)
            {              
                list[holeIndex] = list[secondChild - 1];
                holeIndex = secondChild - 1;
            }

            PushHeapIndex(list, holeIndex, topIndex, value);
        }
    }
}
