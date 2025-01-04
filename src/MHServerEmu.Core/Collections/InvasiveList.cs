using System.Collections;

namespace MHServerEmu.Core.Collections
{
    public class InvasiveList<T>
    {
        public int Id { get; private set; }
        public T Head { get; set; }
        public T Tail { get; private set; }
        public int Count { get; private set; }

        private Iterator[] _iterators;
        private int _numIterators;
        private int _maxIterators;

        public InvasiveList(int maxIterators)
        {
            _maxIterators = maxIterators;
            _iterators = new Iterator[_maxIterators];
        }

        public InvasiveList(int maxIterators, int id)
        {
            _maxIterators = maxIterators;
            _iterators = new Iterator[_maxIterators];
            Id = id;
        }

        public IEnumerable<T> Iterate()
        {
            var iterator = new Iterator(this);

            try
            {
                while (iterator.End() == false)
                {
                    var element = iterator.Current;
                    iterator.MoveNext();
                    yield return element;
                }
            }
            finally
            {
                UnregisterIterator(iterator);
            }
        }

        public bool IsEmpty() => Head == null;

        public void Remove(T element)
        {
            if (element == null || Contains(element) == false) return;

            var node = GetInvasiveListNode(element, Id);
            if (node == null) return;

            for (int i = 0; i < _numIterators; i++)
            {
                Iterator iterator = _iterators[i];
                if (element.Equals(iterator.Current))
                {
                    iterator.SkipNext = false;
                    iterator.MoveNext();
                    iterator.SkipNext = true;
                }
            }

            if (node.Next != null)
            {
                T nextElement = node.Next;
                var nextNode = GetInvasiveListNode(nextElement, Id);
                if (nextNode != null)
                    nextNode.Prev = node.Prev;
            }

            if (node.Prev != null)
            {
                T prevElement = node.Prev;
                var prevNode = GetInvasiveListNode(prevElement, Id);
                if (prevNode != null)
                    prevNode.Next = node.Next;
            }

            if (Head.Equals(element)) Head = node.Next;
            if (Tail.Equals(element)) Tail = node.Prev;
            node.Clear();

            if (Count > 0) Count--;
        }

        public void InsertBefore(T element, T oldElement)
        {
            if (oldElement == null || Contains(oldElement) == false) return;
            if (element == null || Contains(element)) return;

            var node = GetInvasiveListNode(element, Id);
            if (node == null) return;

            var oldNode = GetInvasiveListNode(oldElement, Id);
            if (oldNode == null) return;

            var oldPrev = oldNode.Prev;
            oldNode.Prev = element;
            node.Next = oldElement;
            node.Prev = oldPrev;

            if (oldPrev != null)
            {
                var oldPrevNode = GetInvasiveListNode(oldPrev, Id);
                if (oldPrevNode == null) return;
                oldPrevNode.Next = element;
            }
            else
                Head = element;

            Count++;
        }

        public void AddBack(T element)
        {
            if (element == null || Contains(element)) return;

            var node = GetInvasiveListNode(element, Id);
            if (node == null) return;

            node.Prev = Tail;
            if (Tail != null)
            {
                var tailNode = GetInvasiveListNode(Tail, Id);
                if (tailNode == null) return;
                tailNode.Next = element;
            }
            else
                Head = element;

            Tail = element;
            Count++;
        }

        public virtual InvasiveListNode<T> GetInvasiveListNode(T element, int listId) => null;

        public bool Contains(T element)
        {
            if (element == null) return false;
            var node = GetInvasiveListNode(element, Id);
            if (node == null) return false;
            return node.Next != null || node.Prev != null || element.Equals(Head);
        }

        private void RegisterIterator(Iterator iterator)
        {
            if (_numIterators >= _maxIterators)
                throw new InvalidOperationException($"Too many iterators '{_maxIterators}' for invasive list");

            _iterators[_numIterators++] = iterator;
        }

        private void UnregisterIterator(Iterator iterator)
        {
            for (int i = 0; i < _numIterators; i++)
                if (_iterators[i] == iterator)
                {
                    if (_numIterators > 1 && i != _numIterators - 1)
                        _iterators[i] = _iterators[_numIterators - 1];

                    _iterators[_numIterators - 1] = null;
                    _numIterators--;
                    return;
                }

            throw new InvalidOperationException("Iterator not found in iterator collection of invasive list!");
        }

        public class Iterator : IEnumerator<T>
        {
            private InvasiveList<T> _list;            
            public bool SkipNext { get; set; }

            public Iterator(InvasiveList<T> invasiveList)
            {
                _list = invasiveList;
                Current = _list.Head;
                SkipNext = false;
                _list.RegisterIterator(this);
            }

            public T Current { get; private set; }
            object IEnumerator.Current => Current;
            public void Dispose() { }
            public void Reset() { }

            public bool MoveNext()
            {
                if (SkipNext) SkipNext = false; 
                else if (Current != null)
                    Current = _list.GetInvasiveListNode(Current, _list.Id).Next;

                return true;
            }

            public bool End() => Current == null;
        }

    }

    public class InvasiveListNode<T>
    {
        public T Next { get; set; }
        public T Prev { get; set; }

        public void Clear() => Next = Prev = default;
    }

    public class InvasiveListNodeCollection<T>
    {
        private readonly InvasiveListNode<T>[] _nodes;
        private int _numLists;

        public InvasiveListNodeCollection(int numLists)
        {
            _numLists = numLists;
            _nodes = new InvasiveListNode<T>[_numLists];
            for (int i = 0; i < _numLists; i++)
                _nodes[i] = new();
        }

        public InvasiveListNode<T> GetInvasiveListNode(int listIndex)
        {
            if (listIndex >= 0 && listIndex < _numLists)
                return _nodes[listIndex];
            else
                return null;
        }
    }
}
