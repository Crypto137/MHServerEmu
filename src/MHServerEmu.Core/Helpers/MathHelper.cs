namespace MHServerEmu.Core.Helpers
{
    /// <summary>
    /// Provides various math functionality.
    /// </summary>
    public static class MathHelper
    {
        public const float Pi = 3.1415927f;
        public const float TwoPi = 6.2831855f;
        public const float PiOver2 = 1.5707964f;
        public const float PiOver4 = 0.78539819f;

        public const float PiOverHalfCircleDegrees = 0.017453292f;
        public const float HalfCircleDegreesOverPi = 57.295776f;

        public static float ToRadians(float v) => v * PiOverHalfCircleDegrees;
        public static float SquareRoot(float f) => f > 0.0f ? MathF.Sqrt(f) : 0.0f;

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
