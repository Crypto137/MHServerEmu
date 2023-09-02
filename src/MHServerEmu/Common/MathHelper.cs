namespace MHServerEmu.Common
{
    public static class MathHelper
    {
        public static int HighestBitSet(ulong value)
        {
            int bit = 0;
            while (value > 0)
            {
                value >>= 1;
                bit++;
            }

            return bit;
        }
    }
}
