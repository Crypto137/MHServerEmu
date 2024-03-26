using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Core.Tests.System.Random
{
    public class RandTests
    {
        #region uint

        [Theory]
        [InlineData(1, 100, 35)]
        [InlineData(2000, 333, 242)]
        [InlineData(5555555, 500, 401)]
        public void GetUIntMax_SingleIteration_ReturnsExpectedValue(uint seed, uint max, uint expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(2, 34, 10, 17)]
        [InlineData(1250, 120, 33, 101)]
        [InlineData(4554555, 3000, 343, 2984)]
        public void GetUIntMax_MultipleIterations_ReturnsExpectedValue(uint seed, uint max, int numIterations, uint expectedValue)
        {
            Rand rand = new(seed);

            uint value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetUIntMax_Zero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0u, rand.Get(0u));
        }

        [Theory]
        [InlineData(2, 10, 33, 22)]
        [InlineData(3434, 100, 500, 450)]
        [InlineData(68346726, 2300, 5666, 4629)]
        public void GetUIntMinMax_SingleIteration_ReturnsExpectedValue(uint seed, uint min, uint max, uint expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(3, 12, 44, 4, 12)]
        [InlineData(4325, 78, 345, 17, 298)]
        [InlineData(34687892, 12432, 25734, 77, 13129)]
        public void GetUIntMinMax_MultipleIterations_ReturnsExpectedValue(uint seed, uint min, uint max, int numIterations, uint expectedValue)
        {
            Rand rand = new(seed);

            uint value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(min, max);

            Assert.Equal(expectedValue, value);
        }

        #endregion

        #region int

        [Theory]
        [InlineData(4, 123, 16)]
        [InlineData(4378, 2000, 1996)]
        [InlineData(3523407, 90000, 89709)]
        public void GetIntMax_SingleIteration_ReturnsExpectedValue(uint seed, int max, int expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(5, 66, 2, 57)]
        [InlineData(7237, 500, 28, 67)]
        [InlineData(54352189, 10000, 92, 6019)]
        public void GetIntMax_MultipleIterations_ReturnsExpectedValue(uint seed, int max, int numIterations, int expectedValue)
        {
            Rand rand = new(seed);

            int value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetIntMax_Zero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0, rand.Get(0));
        }

        [Fact]
        public void GetIntMax_Negative_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0, rand.Get(-1));
        }

        [Theory]
        [InlineData(6, 1, 5, 1)]
        [InlineData(3468, 324, 600, 332)]
        [InlineData(456867234, 7777, 9999, 8848)]
        public void GetIntMinMax_SingleIteration_ReturnsExpectedValue(uint seed, int min, int max, int expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(9, 42, 58, 6, 50)]
        [InlineData(7777, 111, 222, 33, 204)]
        [InlineData(5761034, 6000, 8000, 144, 7180)]
        public void GetIntMinMax_MultipleIterations_ReturnsExpectedValue(uint seed, int min, int max, int numIterations, int expectedValue)
        {
            Rand rand = new(seed);

            int value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(min, max);

            Assert.Equal(expectedValue, value);
        }

        #endregion

        #region ulong

        [Theory]
        [InlineData(12, 124, 71)]
        [InlineData(223000, 32333, 11951)]
        [InlineData(345634, 34632432, 16973996)]
        public void GetULongMax_SingleIteration_ReturnsExpectedValue(uint seed, ulong max, ulong expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(62, 10, 6, 7)]
        [InlineData(346, 1001, 51, 544)]
        [InlineData(954328, 3465856782, 77, 696198760)]
        public void GetULongMax_MultipleIterations_ReturnsExpectedValue(uint seed, ulong max, int numIterations, ulong expectedValue)
        {
            Rand rand = new(seed);

            ulong value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetULongMax_Zero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0ul, rand.Get(0ul));
        }

        [Theory]
        [InlineData(6, 1, 15, 11)]
        [InlineData(6987, 144, 320, 307)]
        [InlineData(34534634, 45675467, 346357888, 74843548)]
        public void GetULongMinMax_SingleIteration_ReturnsExpectedValue(uint seed, ulong min, ulong max, ulong expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(12, 300, 600, 24, 387)]
        [InlineData(9999, 1, 5000, 12, 4077)]
        [InlineData(7466345, 456456, 789789789, 36, 238535500)]
        public void GetULongMinMax_MultipleIterations_ReturnsExpectedValue(uint seed, ulong min, ulong max, int numIterations, ulong expectedValue)
        {
            Rand rand = new(seed);

            ulong value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(min, max);

            Assert.Equal(expectedValue, value);
        }

        #endregion

        #region long

        [Theory]
        [InlineData(76, 3, 0)]
        [InlineData(1200, 5090, 4987)]
        [InlineData(8531569, 12000, 11157)]
        [InlineData(2342566, -100, 83)]
        public void GetLongMax_SingleIteration_ReturnsExpectedValue(uint seed, long max, long expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(50, 235, 7, 166)]
        [InlineData(1241, 436, 24, 401)]
        [InlineData(123567788, 200, 566, 128)]
        [InlineData(564234523, -500, 436, 112)]
        public void GetLongMax_MultipleIterations_ReturnsExpectedValue(uint seed, long max, int numIterations, long expectedValue)
        {
            Rand rand = new(seed);

            long value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetLongMax_Zero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0L, rand.Get(0L));
        }

        [Theory]
        [InlineData(15, 63, 375, 211)]
        [InlineData(7892, 100, 200, 120)]
        [InlineData(358022368, 4732, 7976, 6995)]
        public void GetLongMinMax_SingleIteration_ReturnsExpectedValue(uint seed, ulong min, long max, long expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(125, 40, 80, 12, 63)]
        [InlineData(47990, 1000, 2000, 96, 1687)]
        [InlineData(567923789, 799, 5589, 3, 1419)]
        public void GetLongMinMax_MultipleIterations_ReturnsExpectedValue(uint seed, ulong min, long max, int numIterations, long expectedValue)
        {
            Rand rand = new(seed);

            long value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(min, max);

            Assert.Equal(expectedValue, value);
        }

        #endregion

        #region float

        // TODO

        #endregion

        #region double

        // TODO

        #endregion
    }
}
