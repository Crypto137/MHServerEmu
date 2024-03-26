using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Core.Tests.System.Random
{
    public class RandMwcTests
    {
        [Fact]
        public void SetSeed_RandomSeed_ReturnsExpectedLeadingBytes()
        {
            RandMwc randMwc = new(0);
            randMwc.SetSeed(0);
            Assert.StartsWith("0x0000029A", randMwc.ToString());
        }

        [Theory]
        [InlineData(1, "0x0000029A00000001")]
        [InlineData(100, "0x0000029A00000064")]
        [InlineData(1234567890, "0x0000029A499602D2")]
        public void SetSeed_FixedSeed_ReturnsExpectedStringRepresentation(uint seed, string stringRepresentation)
        {
            RandMwc randMwc = new(0);
            randMwc.SetSeed(seed);
            Assert.Equal(stringRepresentation, randMwc.ToString());
        }

        [Theory]
        [InlineData(2, 1397538804)]
        [InlineData(128, 3543095578)]
        [InlineData(9999999, 1538938989)]
        public void Get_SingleIteration_ReturnsExpectedValue(uint seed, uint expectedValue)
        {
            RandMwc randMwc = new(seed);
            Assert.Equal(expectedValue, randMwc.Get());
        }

        [Theory]
        [InlineData(3, 10, 1309464540)]
        [InlineData(256, 27, 1757449084)]
        [InlineData(7777777, 521, 2388615634)]
        public void Get_MultipleIterations_ReturnsExpectedValue(uint seed, int numIterations, uint expectedValue)
        {
            RandMwc randMwc = new(seed);

            uint value = 0;
            for (int i = 0; i < numIterations; i++)
                value = randMwc.Get();

            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(4, 12004764058651742646)]
        [InlineData(384, 8758945035800783824)]
        [InlineData(5445555, 14117870662727427272)]
        public void Get64_SingleIteration_ReturnsExpectedValue(uint seed, ulong expectedValue)
        {
            RandMwc randMwc = new(seed);
            Assert.Equal(expectedValue, randMwc.Get64());
        }

        [Theory]
        [InlineData(5, 12, 17016995753819117394)]
        [InlineData(412, 25, 5825973590781123022)]
        [InlineData(6666666, 666, 16965763966401600410)]
        public void Get64_MultipleIterations_ReturnsExpectedValue(uint seed, int numIterations, ulong expectedValue)
        {
            RandMwc randMwc = new(seed);

            ulong value = 0;
            for (int i = 0; i < numIterations; i++)
                value = randMwc.Get64();

            Assert.Equal(expectedValue, value);
        }
    }
}
