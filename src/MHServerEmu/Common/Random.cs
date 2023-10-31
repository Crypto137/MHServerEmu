namespace MHServerEmu.Common
{
    public class Random
    {
        public const int RandMax = 0x7fffffff;

        private int _seed;
        private readonly Rand _rand;

        public Random() 
        {
            _seed = 0;
            _rand = new Rand(0);
        }

        public Random(int seed)
        {
            _seed = seed;
            _rand = new Rand((uint)seed);
        }

        public void Seed(int seed)
        {
            _seed = seed;
            _rand.SetSeed((uint)seed);
        }

        public int GetSeed()
        {
            return _seed;
        }

        public int Next()
        {
            return Next(RandMax);
        }

        public int Next(int max)
        {
            return Next(0, max);
        }

        public int Next(int min, int max)
        {
            if (min == max)
                return min;
            // Verify: Min is greater than max
            int range = max - min;
            return (int)(_rand.Get() & RandMax) % range + min;
        }

        public float NextFloat()
        {
            return _rand.GetFloat();
        }

        public float NextFloat(float max)
        {
            return _rand.Get(0.0f, max);
        }

        public float NextFloat(float min, float max)
        {
            return _rand.Get(min, max);
        }

        public double NextDouble()
        {
            return _rand.GetDouble();
        }

        public double NextDouble(double max)
        {
            return _rand.Get(0.0, max);
        }

        public double NextDouble(double min, double max)
        {
            return _rand.Get(min, max);
        }

        public bool NextPct(int pct)
        {
            return (Next(0, 100) < pct);
        }

    }

    public class Rand : RandMWC
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
            return (range == 0 ? Get() : Get(range) + min);
        }

        public int Get(int max)
        {
            return (max <= 0 ? 0 : (int)(Get() & 0x7fffffff) % max);
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
            return (max == 0 ? 0 : Get64() % max);
        }

        public ulong Get(ulong min, ulong max)
        {
            if (max < min)
                return max;
            ulong range = max - min + 1;
            return (range == 0 ? Get64() : Get(range) + min);
        }

        public long Get(long max)
        {
            return (max == 0 ? 0 : (long)(Get64() & 0x7fffffffffffffffUL) % max);
        }

        public long Get(ulong min, long max)
        {
            if (max < (long)min)
                return max;
            ulong range = (ulong)max - (ulong)min + 1;
            return (long)(range == 0 ? Get() : Get(range) + min);
        }

        public float GetFloat()
        {
            uint value = (Get() & 0x7fffff) | 0x3f800000;
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0) - 1.0f;
        }

        public float Get(float max)
        {
            return (max <= 0.0f ? 0.0f : GetFloat() * max);
        }

        public float Get(float min, float max)
        {
            return (max < min ? max : GetFloat() * (max - min) + min);
        }

        public double GetDouble()
        {
            ulong value = (Get64() & 0xfffffffffffff) | 0x3ff0000000000000;
            return BitConverter.ToDouble(BitConverter.GetBytes(value), 0) - 1.0;
        }

        public double Get(double max)
        {
            return (max <= 0.0f ? 0.0f : GetDouble() * max);
        }

        public double Get(double min, double max)
        {
            return (max < min ? max : GetDouble() * (max - min) + min);
        }
    }

    public class RandMWC
    {
        private ulong _seed;
        public const uint RandMax = 0xffffffff;

        public RandMWC(uint seed)
        {
            SetSeed(seed == 0 ? (uint)DateTime.Now.Ticks : seed);
        }

        public void SetSeed(uint seed)
        {
            _seed = ((ulong)666 << 32) | seed;
        }

        public uint Get()
        {
            _seed = 698769069UL * (_seed & 0xffffffff) + (_seed >> 32);
            return (uint)_seed;
        }

        public ulong Get64()
        {
            return ((ulong)Get() << 32) | Get();
        }
    }
}
