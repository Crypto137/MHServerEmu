using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Tests.Helpers
{
    public class MathHelperTests
    {
        [Theory]
        [InlineData(100f, 1.74532926f)]
        [InlineData(-256f, -4.46804285f)]
        [InlineData(0f, 0f)]
        public void ToRadians_Float_ReturnsExpectedValue(float v, float expectedValue)
        {
            Assert.Equal(expectedValue, MathHelper.ToRadians(v));
        }

        [Theory]
        [InlineData(144f, 12f)]
        [InlineData(12.512f, 3.53723049f)]
        [InlineData(-16f, 0f)]
        [InlineData(0f, 0f)]
        public void SquareRoot_Float_ReturnsExpectedValue(float f, float expectedValue)
        {
            Assert.Equal(expectedValue, MathHelper.SquareRoot(f));
        }

        [Theory]
        [InlineData(0x0000000000000000, 0)]
        [InlineData(0x0000000000000001, 0)]
        [InlineData(0x0000000000000002, 1)]
        [InlineData(0x00000000B65F6806, 31)]
        [InlineData(0x1D6868A33FF0E850, 60)]
        public void HighestBitSet_ULong_ReturnsExpectedValue(ulong value, int expectedValue)
        {
            Assert.Equal(expectedValue, MathHelper.HighestBitSet(value));
        }
    }
}
