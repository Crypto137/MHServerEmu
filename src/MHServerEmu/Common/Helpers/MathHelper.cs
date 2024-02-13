namespace MHServerEmu.Common.Helpers
{
    /// <summary>
    /// Provides various math functionality.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Determines the index of the highest bit set in a <see cref="ulong"/> value.
        /// </summary>
        public static int HighestBitSet(ulong value)
        {
            int bit = 0;
            while ((value >>= 1) > 0)
                bit++;
            return bit;
        }
    }
}
