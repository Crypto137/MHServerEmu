namespace MHServerEmu.Core.System.Random
{
    public class Rand : RandMwc
    {
        public Rand(uint seed) : base(seed) { }

        public uint Get(uint max)
        {
            if (max == 0)
                return 0;
            return Get() % max;
        }

        public uint Get(uint min, uint max)
        {
            if (max < min)
                return max;
            uint range = max - min + 1;
            return range == 0 ? Get() : Get(range) + min;
        }

        public int Get(int max)
        {
            return max <= 0 ? 0 : (int)(Get() & 0x7fffffff) % max;
        }

        public int Get(int min, int max)
        {
            if (max < min)
                return max;
            uint range = (uint)max - (uint)min + 1;
            return (int)(range == 0 ? Get() : Get(range) + min);
        }

        public ulong Get(ulong max)
        {
            return max == 0 ? 0 : Get64() % max;
        }

        public ulong Get(ulong min, ulong max)
        {
            if (max < min)
                return max;
            ulong range = max - min + 1;
            return range == 0 ? Get64() : Get(range) + min;
        }

        public long Get(long max)
        {
            return max == 0 ? 0 : (long)(Get64() & 0x7fffffffffffffffUL) % max;
        }

        public long Get(ulong min, long max)    // ulong min <-- same as in the client
        {
            if (max < (long)min)
                return max;
            ulong range = (ulong)max - min + 1;
            return (long)(range == 0 ? Get() : Get(range) + min);
        }

        public float GetFloat()
        {
            uint value = Get() & 0x7fffff | 0x3f800000;
            return BitConverter.UInt32BitsToSingle(value) - 1.0f;
        }

        public float Get(float max)
        {
            return max <= 0.0f ? 0.0f : GetFloat() * max;
        }

        public float Get(float min, float max)
        {
            return max < min ? max : GetFloat() * (max - min) + min;
        }

        public double GetDouble()
        {
            ulong value = Get64() & 0xfffffffffffff | 0x3ff0000000000000;
            return BitConverter.UInt64BitsToDouble(value) - 1.0;
        }

        public double Get(double max)
        {
            return max <= 0.0f ? 0.0f : GetDouble() * max;
        }

        public double Get(double min, double max)
        {
            return max < min ? max : GetDouble() * (max - min) + min;
        }
    }
}
