using System.Collections;

namespace MHServerEmu.Core.Collections
{
    /// <summary>
    /// A wrapper around <see cref="List{T}"/> that keeps a sorted collection of unique items.
    /// </summary>
    public class SortedVector<T> : IList<T>
    {
        private readonly List<T> _list;

        public T this[int index] { get => _list[index]; set => Insert(index, value); }

        public int Count { get => _list.Count; }

        public bool IsReadOnly { get => false; }

        /// <summary>
        /// Constructs a new <see cref="SortedVector{T}"/> instance with the default initial capacity.
        /// </summary>
        public SortedVector()
        {
            _list = new();
        }

        /// <summary>
        /// Constructs a new <see cref="SortedVector{T}"/> instance with the specified initial capacity.
        /// </summary>
        public SortedVector(int capacity)
        {
            _list = new(capacity);
        }

        public override string ToString()
        {
            return $"Count = {Count}";
        }

        /// <summary>
        /// Inserts the provided <typeparamref name="T"/> instance into this sorted vector.
        /// Returns <see langword="false"/> if an equivalent instance was already present.
        /// </summary>
        public bool SortedInsert(T item)
        {
            int index = _list.BinarySearch(item);
            if (index >= 0)
            {
                _list[index] = item;
                return false;
            }

            _list.Insert(~index, item);
            return true;
        }

        #region IList Implementation

        public void Add(T item)
        {
            SortedInsert(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.BinarySearch(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int IndexOf(T item)
        {
            int index = _list.BinarySearch(item);
            return index >= 0 ? index : -1;
        }

        public void Insert(int index, T item)
        {
            throw new InvalidOperationException("SortedVector cannot insert items at arbitrary indexes. Use SortedInsert() instead.");
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion
    }
}
