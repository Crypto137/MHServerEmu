namespace MHServerEmu.Common.Helpers
{
    /// <summary>
    /// Provides various math functionality.
    /// </summary>
    public static class MathHelper
    {
        public const float Pi = 3.1415926f;

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

        /// <summary>
        /// Angle is simplified into [0;2π] interval
        /// </summary>
        public static float WrapAngleRadians(float angleInRadian)
        {
            const float Pi2 = 2 * Pi;
            int wrap = (int)(angleInRadian / Pi2);
            if (wrap > 0) return angleInRadian - wrap * Pi2;
            if (angleInRadian < 0.0f) return angleInRadian - (wrap - 1) * Pi2;
            return angleInRadian;
        }
    }
}
