namespace MHServerEmu.Core.System.Random
{
    // More info on MWC random: https://en.wikipedia.org/wiki/Multiply-with-carry_pseudorandom_number_generator
    public class RandMwc
    {
        private ulong _seed;
        public const uint RandMax = 0xffffffff;

        public RandMwc(uint seed)
        {
            SetSeed(seed == 0 ? (uint)DateTime.Now.Ticks : seed);
        }

        public void SetSeed(uint seed)
        {
            _seed = (ulong)666 << 32 | seed;
        }

        public uint Get()
        {
            _seed = 698769069UL * (_seed & 0xffffffff) + (_seed >> 32);
            return (uint)_seed;
        }

        public ulong Get64()
        {
            return (ulong)Get() << 32 | Get();
        }

        public override string ToString()
        {
            return $"0x{_seed:X16}";
        }
    }
}
