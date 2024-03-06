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

        public const float PI2 = 2 * MathF.PI;

        /// <summary>
        /// Angle is simplified into [0;2π] interval
        /// </summary>
        public static float WrapAngleRadians(float angleInRadian)
        {
            int wrap = (int)(angleInRadian / PI2);
            if (wrap > 0) return angleInRadian - wrap * PI2;
            if (angleInRadian < 0.0f) return angleInRadian - (wrap - 1) * PI2;
            return angleInRadian;
        }

        public static float ToRadians(float v) => v * 0.017453292f;

        public static float SquareRoot(float f) => f > 0.0f ? MathF.Sqrt(f) : 0.0f;
    }
}
