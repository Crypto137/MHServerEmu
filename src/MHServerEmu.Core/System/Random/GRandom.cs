namespace MHServerEmu.Core.System.Random
{
    public class GRandom
    {
        public const int RandMax = 0x7fffffff;

        private int _seed;
        private readonly Rand _rand;

        public GRandom()
        {
            _seed = 0;
            _rand = new Rand(0);
        }

        public GRandom(int seed)
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
            return Next(0, 100) < pct;
        }
        public override string ToString()
        {
            return _rand.ToString();
        }

        public void ShuffleList<T>(List<T> list)
        {
            if (list.Count > 1)
                for (int i = 0; i < list.Count; i++)
                {
                    int j = Next(i, list.Count);
                    (list[j], list[i]) = (list[i], list[j]);
                }
        }
    }
}
