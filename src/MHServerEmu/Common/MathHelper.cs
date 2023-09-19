namespace MHServerEmu.Common
{
    public static class MathHelper
    {
        public static int HighestBitSet(ulong value)
        {
            int bit = 0;
            while ((value >>= 1) > 0)
                bit++;
            return bit;
        }

        public static int HighestBitSet(int value) => HighestBitSet((ulong)value);
    }
}
