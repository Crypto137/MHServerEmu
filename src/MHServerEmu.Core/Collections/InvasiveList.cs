using System.Collections;
using System.Runtime.CompilerServices;

namespace MHServerEmu.Core.Collections
{
    public class InvasiveList<T>
    {
        private readonly Iterator[] _iterators;

        private readonly Stack<Iterator> _iteratorPool;
        private Iterator _reusableIterator;

        private int _numIterators;

        public int Id { get; private set; }
        public T Head { get; private set; }
        public T Tail { get; private set; }
        public int Count { get; private set; }

        public bool IsEmpty { get => Head == null; }

        public InvasiveList(int maxIterators, int id = 0)
        {
            _iterators = new Iterator[maxIterators];

            if (maxIterators > 1)
                _iteratorPool = new();

            Id = id;
        }

        public IEnumerator<T> GetEnumerator()
        {
            Iterator iterator;

            if (_iteratorPool != null)
            {
                if (_iteratorPool.TryPop(out iterator) == false)
                    iterator = new(this);
            }
            else
            {
                _reusableIterator ??= new(this);
                iterator = _reusableIterator;
            }

            iterator.Initialize();
            return iterator;
        }

        public void Remove(T element)
        {
            if (element == null || Contains(element) == false) return;

            ref var node = ref GetInvasiveListNode(element, Id);
            if (Unsafe.IsNullRef(ref node)) return;

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
                ref var nextNode = ref GetInvasiveListNode(nextElement, Id);
                if (Unsafe.IsNullRef(ref nextNode) == false)
                    nextNode.Prev = node.Prev;
            }

            if (node.Prev != null)
            {
                T prevElement = node.Prev;
                ref var prevNode = ref GetInvasiveListNode(prevElement, Id);
                if (Unsafe.IsNullRef(ref prevNode) == false)
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

            ref var node = ref GetInvasiveListNode(element, Id);
            if (Unsafe.IsNullRef(ref node)) return;

            ref var oldNode = ref GetInvasiveListNode(oldElement, Id);
            if (Unsafe.IsNullRef(ref oldNode)) return;

            var oldPrev = oldNode.Prev;
            oldNode.Prev = element;
            node.Next = oldElement;
            node.Prev = oldPrev;

            if (oldPrev != null)
            {
                ref var oldPrevNode = ref GetInvasiveListNode(oldPrev, Id);
                if (Unsafe.IsNullRef(ref oldPrevNode)) return;
                oldPrevNode.Next = element;
            }
            else
                Head = element;

            Count++;
        }

        public void AddBack(T element)
        {
            if (element == null || Contains(element)) return;

            ref var node = ref GetInvasiveListNode(element, Id);
            if (Unsafe.IsNullRef(ref node)) return;

            node.Prev = Tail;
            if (Tail != null)
            {
                ref var tailNode = ref GetInvasiveListNode(Tail, Id);
                if (Unsafe.IsNullRef(ref tailNode)) return;
                tailNode.Next = element;
            }
            else
                Head = element;

            Tail = element;
            Count++;
        }

        public virtual ref InvasiveListNode<T> GetInvasiveListNode(T element, int listId)
        {
            return ref Unsafe.NullRef<InvasiveListNode<T>>();
        }

        public bool Contains(T element)
        {
            if (element == null) return false;
            ref var node = ref GetInvasiveListNode(element, Id);
            if (Unsafe.IsNullRef(ref node)) return false;
            return node.Next != null || node.Prev != null || element.Equals(Head);
        }

        private void RegisterIterator(Iterator iterator)
        {
            if (_numIterators >= _iterators.Length)
                throw new InvalidOperationException($"Too many iterators '{_iterators.Length}' for invasive list");

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

                    // pool iterator instance for reuse
                    iterator.Reset();
                    _iteratorPool?.Push(iterator);

                    return;
                }

            throw new InvalidOperationException("Iterator not found in iterator collection of invasive list!");
        }

        public sealed class Iterator : IEnumerator<T>
        {
            private readonly InvasiveList<T> _list;

            private bool _start = true;

            public T Current { get; private set; }
            object IEnumerator.Current { get => Current; }

            public bool SkipNext { get; set; } = false;

            public Iterator(InvasiveList<T> invasiveList)
            {
                _list = invasiveList;
            }

            public void Initialize()
            {
                _list.RegisterIterator(this);
            }

            public void Dispose()
            {
                _list.UnregisterIterator(this);
            }

            public void Reset()
            {
                _start = true;
                Current = default;
                SkipNext = false;
            }

            public bool MoveNext()
            {
                if (_start)
                {
                    Current = _list.Head;
                    _start = false;
                }
                else
                {
                    if (SkipNext)
                        SkipNext = false;
                    else if (Current != null)
                        Current = _list.GetInvasiveListNode(Current, _list.Id).Next;
                }

                return Current != null;
            }
        }
    }

    public struct InvasiveListNode<T>
    {
        public T Next;
        public T Prev;

        public void Clear() => Next = Prev = default;
    }
}
