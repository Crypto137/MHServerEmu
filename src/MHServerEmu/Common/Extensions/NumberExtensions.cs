namespace MHServerEmu.Common.Extensions
{
    public static class NumberExtensions
    {
        /// <summary>
        /// Determines the index of the highest bit set in a ulong value.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <returns>Index of the highest bit set.</returns>
        public static int HighestBitSet(this ulong value)
        {
            int bit = 0;
            while ((value >>= 1) > 0)
                bit++;
            return bit;
        }

        /// <summary>
        /// Casts an int to ulong and determines its highest bit set.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <returns>Index of the highest bit set.</returns>
        public static int HighestBitSet(this int value) => ((ulong)value).HighestBitSet();

        /// <summary>
        /// Checks if an integer is within the specified range.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <param name="min">Minimum acceptable value.</param>
        /// <param name="max">Maximum acceptable value.</param>
        /// <returns>Is within range.</returns>
        public static bool IsWithin(this int value, int min, int max)
        {
            if (value < min) return false;
            if (value > max) return false;
            return true;
        }
    }
}
