using MHServerEmu.Core.Collisions;

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
        public static int RoundUpToInt(float v) => (int)MathF.Ceiling(v);

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

        public static void BitSet(ref ulong value, ulong bitMask)
        {
            value |= bitMask;
        }

        public static void EBitSet(ref ulong value, int bit)
        {
            BitSet(ref value, 1UL << bit);
        }

        public static bool EBitTest(ulong value, int bit)
        {
            return BitTest(value, 1UL << bit);
        }

        public static bool BitTest(ulong value, ulong bitMask)
        {
            return (value & bitMask) != 0;
        }

        public static bool BitTest(int value, int bitMask)
        {
            return (value & bitMask) != 0;
        }

        public static bool BitTestAll(int value, int bitMask)
        {
            return (value & bitMask) == bitMask;
        }

        public static int BitfieldGetLS1B(int value)
        {
            return value & ~(value - 1);
        }

        public static ulong SwizzleSignBit(long value)
        {
            ulong bits = (ulong)value;
            return (bits << 1) | (bits >> 63);
        }

        public static long UnswizzleSignBit(ulong bits)
        {
            return (long)((bits >> 1) | (bits << 63));
        }

        public static float Round(float value)
        {
            if (value < 0.0f)
                return (int)(value - 0.5f);
            else
                return (int)(value + 0.5f);
        }

        public static int RoundToInt(float value)
        {
            if (value < 0.0f)
                return (int)(value - 0.5f);
            else
                return (int)(value + 0.5f);
        }

        public static int RoundToInt(double value)
        {
            if (value < 0.0)
                return (int)(value - 0.5);
            else
                return (int)(value + 0.5);
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

        public static bool IsBelow(long value, long maxValue, float thresholdPct)
        {
            return value < (maxValue * thresholdPct);
        }

        public static bool IsAboveOrEqual(long value, long maxValue, float thresholdPct)
        {
            return value >= (maxValue * thresholdPct);
        }

        public static bool IsAbove(long value, long maxValue, float thresholdPct)
        {
            return value > (maxValue * thresholdPct);
        }

        public static bool IsNearZero(float value)
        {
            return Segment.IsNearZero(value);
        }

        public static float Ratio(long value, long maxValue)
        {
            // NOTE: We need to divide using double because ratio is often used for health, which can reach very high values
            return (float)((double)value / maxValue);
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

        /// <summary>
        /// Performs clamp without throwing when min > max.
        /// </summary>
        public static float ClampNoThrow(float value, float min, float max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        /// <summary>
        /// Performs clamp without throwing when min > max.
        /// </summary>
        public static int ClampNoThrow(int value, int min, int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}
