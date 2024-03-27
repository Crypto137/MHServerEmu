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
        public void GetUInt_MaxSingleIteration_ReturnsExpectedValue(uint seed, uint max, uint expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(2, 34, 10, 17)]
        [InlineData(1250, 120, 33, 101)]
        [InlineData(4554555, 3000, 343, 2984)]
        public void GetUInt_MaxMultipleIterations_ReturnsExpectedValue(uint seed, uint max, int numIterations, uint expectedValue)
        {
            Rand rand = new(seed);

            uint value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetUInt_MaxZero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0u, rand.Get(0u));
        }

        [Theory]
        [InlineData(2, 10, 33, 22)]
        [InlineData(3434, 100, 500, 450)]
        [InlineData(68346726, 2300, 5666, 4629)]
        public void GetUInt_MinMaxSingleIteration_ReturnsExpectedValue(uint seed, uint min, uint max, uint expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(3, 12, 44, 4, 12)]
        [InlineData(4325, 78, 345, 17, 298)]
        [InlineData(34687892, 12432, 25734, 77, 13129)]
        public void GetUInt_MinMaxMultipleIterations_ReturnsExpectedValue(uint seed, uint min, uint max, int numIterations, uint expectedValue)
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
        public void GetInt_MaxSingleIteration_ReturnsExpectedValue(uint seed, int max, int expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(5, 66, 2, 57)]
        [InlineData(7237, 500, 28, 67)]
        [InlineData(54352189, 10000, 92, 6019)]
        public void GetInt_MaxMultipleIterations_ReturnsExpectedValue(uint seed, int max, int numIterations, int expectedValue)
        {
            Rand rand = new(seed);

            int value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetInt_MaxZero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0, rand.Get(0));
        }

        [Fact]
        public void GetInt_MaxNegative_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0, rand.Get(-1));
        }

        [Theory]
        [InlineData(6, 1, 5, 1)]
        [InlineData(3468, 324, 600, 332)]
        [InlineData(456867234, 7777, 9999, 8848)]
        public void GetInt_MinMaxSingleIteration_ReturnsExpectedValue(uint seed, int min, int max, int expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(9, 42, 58, 6, 50)]
        [InlineData(7777, 111, 222, 33, 204)]
        [InlineData(5761034, 6000, 8000, 144, 7180)]
        public void GetInt_MinMaxMultipleIterations_ReturnsExpectedValue(uint seed, int min, int max, int numIterations, int expectedValue)
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
        public void GetULong_MaxSingleIteration_ReturnsExpectedValue(uint seed, ulong max, ulong expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(62, 10, 6, 7)]
        [InlineData(346, 1001, 51, 544)]
        [InlineData(954328, 3465856782, 77, 696198760)]
        public void GetULong_MaxMultipleIterations_ReturnsExpectedValue(uint seed, ulong max, int numIterations, ulong expectedValue)
        {
            Rand rand = new(seed);

            ulong value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetULong_MaxZero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0ul, rand.Get(0ul));
        }

        [Theory]
        [InlineData(6, 1, 15, 11)]
        [InlineData(6987, 144, 320, 307)]
        [InlineData(34534634, 45675467, 346357888, 74843548)]
        public void GetULong_MinMaxSingleIteration_ReturnsExpectedValue(uint seed, ulong min, ulong max, ulong expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(12, 300, 600, 24, 387)]
        [InlineData(9999, 1, 5000, 12, 4077)]
        [InlineData(7466345, 456456, 789789789, 36, 238535500)]
        public void GetULong_MinMaxMultipleIterations_ReturnsExpectedValue(uint seed, ulong min, ulong max, int numIterations, ulong expectedValue)
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
        public void GetLong_MaxSingleIteration_ReturnsExpectedValue(uint seed, long max, long expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(50, 235, 7, 166)]
        [InlineData(1241, 436, 24, 401)]
        [InlineData(123567788, 200, 566, 128)]
        [InlineData(564234523, -500, 436, 112)]
        public void GetLong_MaxMultipleIterations_ReturnsExpectedValue(uint seed, long max, int numIterations, long expectedValue)
        {
            Rand rand = new(seed);

            long value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetLong_MaxZero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0L, rand.Get(0L));
        }

        [Theory]
        [InlineData(15, 63, 375, 211)]
        [InlineData(7892, 100, 200, 120)]
        [InlineData(358022368, 4732, 7976, 6995)]
        public void GetLong_MinMaxSingleIteration_ReturnsExpectedValue(uint seed, ulong min, long max, long expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(125, 40, 80, 12, 63)]
        [InlineData(47990, 1000, 2000, 96, 1687)]
        [InlineData(567923789, 799, 5589, 3, 1419)]
        public void GetLong_MinMaxMultipleIterations_ReturnsExpectedValue(uint seed, ulong min, long max, int numIterations, long expectedValue)
        {
            Rand rand = new(seed);

            long value = 0;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(min, max);

            Assert.Equal(expectedValue, value);
        }

        #endregion

        #region float

        [Theory]
        [InlineData(31, 0.292771935f)]
        [InlineData(8536, 0.787935495f)]
        [InlineData(15234788, 0.188070059f)]
        public void GetFloat_NoRange_ReturnsExpectedValue(uint seed, float expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.GetFloat());
        }

        [Theory]
        [InlineData(61, 120f, 34.2840233f)]
        [InlineData(500, 623.5345f, 550.092834f)]
        [InlineData(23463461, 0.2352f, 0.0914127976f)]
        public void GetFloat_MaxSingleIteration_ReturnsExpectedValue(uint seed, float max, float expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(55, 10f, 235, 1.1476326f)]
        [InlineData(3457, 123f, 2, 121.178253f)]
        [InlineData(34634577, 0.33f, 21, 0.018087592f)]
        public void GetFloat_MaxMultipleIterations_ReturnsExpectedValue(uint seed, float max, int numIterations, float expectedValue)
        {
            Rand rand = new(seed);

            float value = 0f;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetFloat_MaxZero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0f, rand.Get(0f));
        }

        [Fact]
        public void GetFloat_MaxNegative_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0f, rand.Get(-1f));
        }

        [Theory]
        [InlineData(1, 1f, 10f, 3.69859314f)]
        [InlineData(123, 1200f, 4000f, 3639.03882f)]
        [InlineData(123123123, 0.5f, 0.75f, 0.69243288f)]
        public void GetFloat_MinMaxSingleIteration_ReturnsExpectedValue(uint seed, float min, float max, float expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(2, 2f, 12f, 4, 11.0796909f)]
        [InlineData(456, 700f, 900f, 52, 787.192627f)]
        [InlineData(456456, 0.12f, 0.24f, 101, 0.184809297f)]
        public void GetFloat_MinMaxMultipleIterations_ReturnsExpectedValue(uint seed, float min, float max, int numIterations, float expectedValue)
        {
            Rand rand = new(seed);

            float value = 0f;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(min, max);

            Assert.Equal(expectedValue, value);
        }

        #endregion

        #region double

        [Theory]
        [InlineData(43, 0.11954659917621657)]
        [InlineData(6852, 0.87916414079784388)]
        [InlineData(80324356, 0.084059570460851107)]
        public void GetDouble_NoRange_ReturnsExpectedValue(uint seed, double expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.GetDouble());
        }

        [Theory]
        [InlineData(125, 15.0, 11.473661053223553)]
        [InlineData(12555, 12567.22, 4077.7080265752666)]
        [InlineData(33876521, 0.533365, 0.11020305569271709)]
        public void GetDouble_MaxSingleIteration_ReturnsExpectedValue(uint seed, double max, double expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(max));
        }

        [Theory]
        [InlineData(346, 85.0, 44, 41.290812749627889)]
        [InlineData(336888, 45345.0, 124, 21452.01823700016)]
        [InlineData(143467772, 0.66, 1000, 0.62660411751637701)]
        public void GetDouble_MaxMultipleIterations_ReturnsExpectedValue(uint seed, double max, int numIterations, double expectedValue)
        {
            Rand rand = new(seed);

            double value = 0f;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(max);

            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public void GetDouble_MaxZero_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0.0, rand.Get(0.0));
        }

        [Fact]
        public void GetDouble_MaxNegative_ReturnsZero()
        {
            Rand rand = new(0);
            Assert.Equal(0.0, rand.Get(-1.0));
        }

        [Theory]
        [InlineData(2, 1.0, 10.0, 8.1717721898430113)]
        [InlineData(234, 4122.0, 6211.0, 4454.9016847384219)]
        [InlineData(234234234, 0.742, 0.931, 0.87701535858245006)]
        public void GetDouble_MinMaxSingleIteration_ReturnsExpectedValue(uint seed, double min, double max, double expectedValue)
        {
            Rand rand = new(seed);
            Assert.Equal(expectedValue, rand.Get(min, max));
        }

        [Theory]
        [InlineData(3, 7.9, 21.2, 5, 16.370633654546904)]
        [InlineData(56756, 136.0, 152.0, 72, 137.08451710120684)]
        [InlineData(567567567, 0.456, 0.654, 201, 0.61802306029211118)]
        public void GetDouble_MinMaxMultipleIterations_ReturnsExpectedValue(uint seed, double min, double max, int numIterations, double expectedValue)
        {
            Rand rand = new(seed);

            double value = 0f;
            for (int i = 0; i < numIterations; i++)
                value = rand.Get(min, max);

            Assert.Equal(expectedValue, value);
        }

        #endregion
    }
}
