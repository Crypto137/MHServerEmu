using MHServerEmu.Common.Helpers;

namespace MHServerEmu.Common.Extensions
{
    /// <summary>
    /// Extends numeric types.
    /// </summary>
    public static class NumberExtensions
    {
        /// <summary>
        /// Checks if an <see cref="int"/> is within the specified range.
        /// </summary>
        public static bool IsWithin(this int value, int min, int max)
        {
            if (value < min) return false;
            if (value > max) return false;
            return true;
        }

        /// <summary>
        /// Determines the index of the highest bit set in a <see cref="ulong"/> value.
        /// </summary>
        public static int HighestBitSet(this ulong value) => MathHelper.HighestBitSet(value);

        /// <summary>
        /// Determines the index of the highest bit set in a <see cref="uint"/> value.
        /// </summary>
        public static int HighestBitSet(this uint value) => MathHelper.HighestBitSet(value);

        // NOTE: We need to keep the bit count in mind and cast long -> ulong and int -> uint to avoid distortions

        /// <summary>
        /// Determines the index of the highest bit set in a <see cref="long"/> value.
        /// </summary>
        public static int HighestBitSet(this long value) => MathHelper.HighestBitSet((ulong)value);

        /// <summary>
        /// Determines the index of the highest bit set in an <see cref="int"/> value.
        /// </summary>
        public static int HighestBitSet(this int value) => MathHelper.HighestBitSet((uint)value);
    }
}
