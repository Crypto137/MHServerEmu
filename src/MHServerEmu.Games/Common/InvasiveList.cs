
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Common
{
    public class InvasiveList<T>
    {
        private int _maxIterators;

        public InvasiveList(int maxIterators) 
        {
            _maxIterators = maxIterators;
        }

        public IEnumerable<T> Iterate()
        {
            throw new NotImplementedException();
        }

        public T Head()
        {
            throw new NotImplementedException();
        }

        public void Remove(T element)
        {
            throw new NotImplementedException();
        }

        internal void AddBack(T element)
        {
            throw new NotImplementedException();
        }
    }
}
