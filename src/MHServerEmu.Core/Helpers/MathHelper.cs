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

        public static float PositionSqTolerance => 2 * Square(0.125f);

        public static float ToRadians(float v) => v * PiOverHalfCircleDegrees;
        public static float ToDegrees(float v) => v * HalfCircleDegreesOverPi;
        public static float SquareRoot(float f) => f > 0.0f ? MathF.Sqrt(f) : 0.0f;
        public static float Square(float v) => v * v;
        public static int RoundDownToInt(float v) => (int)MathF.Floor(v);

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

        public static void BitSet(ref int value, int bitMask)
        {
            value |= bitMask;
        }

        public static bool BitTest(int value, int bitMask)
        {
            return (value & bitMask) != 0;
        }

        public static int RoundToInt(float value) // TODO check where it used
        {
            if (value < 0.0f)
                return (int)(value - 0.5f);
            else
                return (int)(value + 0.5f);
        }

        public static long RoundToInt64(float value)
        {
            if (value < 0.0f)
                return (long)(value - 0.5f);
            else
                return (long)(value + 0.5f);
        }

        public static bool IsBelowOrEqual(long value, long maxValue, float thresholdPct)
        {
            return value <= (maxValue * thresholdPct);
        }

        public static float Ratio(long value, long maxValue)
        {
            return value / (float)maxValue;
        }

        public static long Modulus(long v1, long v2)
        {
            if (v1 < 0) v1 += v2;
            return v1 % v2;
        }

        public static float FloatModulus(float v1, float v2)
        {
            return MathF.IEEERemainder(v1, v2);
        }
    }
}
