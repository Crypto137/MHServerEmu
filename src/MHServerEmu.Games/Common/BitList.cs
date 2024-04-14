
namespace MHServerEmu.Games.Common
{
    public class BitList
    {
        protected List<bool> _bits;

        public BitList()
        {
            _bits = new();
        }

        public BitList(List<bool> bits)
        {
            _bits = new (bits);
        }

        public void Set(int index, bool value)
        {
            Expand(index);
            _bits[index] = value;
        }

        public void Set(int bitIndex)
        {
            Set(bitIndex, true);
        }

        private void Expand(int index)
        {
            while (index >= _bits.Count)
                _bits.Add(false);
        }

        public bool Get(int index)
        {
            if (index < _bits.Count)
                return _bits[index];
            return false; 
        }

        public bool Any()
        {
            return _bits.Any(bit => bit);
        }

        public void Clear()
        {
            for (int i = 0; i < _bits.Count; i++)
                _bits[i] = false;
        }

        public T Copy<T>() where T : BitList, new()
        {
            T result = new ();
            result._bits.AddRange(_bits);
            return result;
        }

        public void Resize(int newSize)
        {
            Reserve(newSize);
            _bits.RemoveRange(newSize, _bits.Count - newSize);
        }

        public void Reserve(int newSize)
        {
            while (_bits.Count < newSize)
                _bits.Add(false);
        }

        public int Size => _bits.Count;

        public bool this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        public static T And<T>(T left, T right) where T: BitList, new()
        {
            T result = left.Copy<T>();
            result.Reserve(right.Size);

            for (int i = 0; i < right.Size; i++)
                result._bits[i] &= right._bits[i];

            return result;
        }

        public static BitList operator &(BitList left, BitList right)
        {
            return And(left, right);
        }

        public static T Or<T>(T left, T right) where T : BitList, new()
        {
            T result = left.Copy<T>();
            result.Reserve(right.Size);

            for (int i = 0; i < right.Size; i++)
                result._bits[i] |= right._bits[i];

            return result;
        }

        public static BitList operator |(BitList left, BitList right)
        {
            return Or(left, right);
        }

        public static T Xor<T>(T left, T right) where T : BitList, new()
        {
            T result = left.Copy<T>();
            result.Reserve(right.Size);

            for (int i = 0; i < right.Size; i++)
                result._bits[i] ^= right._bits[i];

            return result;
        }

        public static BitList operator ^(BitList left, BitList right)
        {
            return Xor(left, right);
        }
    }
}
