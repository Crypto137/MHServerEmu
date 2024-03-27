using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Core.Tests.System.Random
{
    public class GRandomTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(123123123)]
        [InlineData(-1)]
        public void Seed_SetFixed_ReturnsSameValue(int seed)
        {
            GRandom random = new();
            random.Seed(seed);
            Assert.Equal(seed, random.GetSeed());
        }

        [Theory]
        [InlineData(2, 1397538804)]
        [InlineData(456, 811116226)]
        [InlineData(456456, 177857282)]
        public void NextInt_NoRangeSingleIteration_ReturnsExpectedValue(int seed, int expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.Next());
        }

        [Theory]
        [InlineData(3, 100, 73)]
        [InlineData(345, 1000, 895)]
        [InlineData(345345, 10000, 5063)]
        public void NextInt_MaxSingleIteration_ReturnsExpectedValue(int seed, int max, int expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.Next(max));
        }

        [Theory]
        [InlineData(4, 20, 40, 34)]
        [InlineData(456, 200, 400, 226)]
        [InlineData(456456, 2000, 4000, 3282)]
        public void NextInt_MinMaxSingleIteration_ReturnsExpectedValue(int seed, int min, int max, int expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.Next(min, max));
        }

        [Theory]
        [InlineData(5, 0.498900771f)]
        [InlineData(567, 0.966423631f)]
        [InlineData(567567, 0.310660958f)]
        public void NextFloat_NoRangeSingleIteration_ReturnsExpectedValue(int seed, float expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.NextFloat());
        }

        [Theory]
        [InlineData(6, 1f, 0.798665047f)]
        [InlineData(678, 10f, 2.40258217f)]
        [InlineData(678678, 0.5f, 0.209540844f)]
        public void NextFloat_MaxSingleIteration_ReturnsExpectedValue(int seed, float max, float expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.NextFloat(max));
        }

        [Theory]
        [InlineData(7, 1f, 20f, 2.87015724f)]
        [InlineData(789, 2.2f, 3.8f, 3.02254844f)]
        [InlineData(789789, 0.1f, 0.33f, 0.221325576f)]
        public void NextFloat_MinMaxSingleIteration_ReturnsExpectedValue(int seed, float min, float max, float expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.NextFloat(min, max));
        }

        [Theory]
        [InlineData(8, 0.18554880397763074)]
        [InlineData(898, 0.50719149347423587)]
        [InlineData(898898, 0.062748448565058457)]
        public void NextDouble_NoRangeSingleIteration_ReturnsExpectedValue(int seed, double expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.NextDouble());
        }

        [Theory]
        [InlineData(9, 1.0, 0.58366332642380403)]
        [InlineData(987, 256.0, 240.47497752426744)]
        [InlineData(987987, 0.42, 0.27682409653927897)]
        public void NextDouble_MaxSingleIteration_ReturnsExpectedValue(int seed, double max, double expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.NextDouble(max));
        }

        [Theory]
        [InlineData(10, 1.0, 10.0, 9.8360006398297966)]
        [InlineData(1010, 3.14, 6.28, 3.4413827509599026)]
        [InlineData(101010, 0.128, 0.256, 0.19410205444819545)]
        public void NextDouble_MinMaxSingleIteration_ReturnsExpectedValue(int seed, double min, double max, double expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.NextDouble(min, max));
        }

        [Theory]
        [InlineData(11, 33, false)]
        [InlineData(1111, 66, false)]
        [InlineData(111111, 99, true)]
        public void NextPct_SingleIteration_ReturnsExpectedValue(int seed, int pct, bool expectedValue)
        {
            GRandom random = new(seed);
            Assert.Equal(expectedValue, random.NextPct(pct));
        }
    }
}
