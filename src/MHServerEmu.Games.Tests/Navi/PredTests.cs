using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Tests.Navi
{
    public class PredTests
    {
        [Theory]
        [InlineData(1f, 1f, 1f, 1f)]
        [InlineData(1.25f, 1.25f, 2.75f, 2.75f)]
        [InlineData(1234.12f, 1234.12f, 1236.12f, 1236.12f)]
        public void NaviPointCompare2D_TwoPoints_ReturnsTrue(float x0, float y0, float x1, float y1)
        {
            Vector3 p0 = new(x0, y0, 0f);
            Vector3 p1 = new(x1, y1, 0f);
            Assert.True(Pred.NaviPointCompare2D(p0, p1));
        }

        [Theory]
        [InlineData(1f, 1f, 4f, 4f)]
        [InlineData(1.25f, 1.25f, 3.5f, 3.5f)]
        [InlineData(1234.12f, 1234.12f, 2236.12f, 2236.12f)]
        public void NaviPointCompare2D_TwoPoints_ReturnsFalse(float x0, float y0, float x1, float y1)
        {
            Vector3 p0 = new(x0, y0, 0f);
            Vector3 p1 = new(x1, y1, 0f);
            Assert.False(Pred.NaviPointCompare2D(p0, p1));
        }

        [Theory]
        [InlineData(7653, 5124, 5124, 7653)]
        [InlineData(3, 2, 2, 3)]
        [InlineData(1, 2, 1, 2)]
        public void SortInputs_TwoInts_OrderIsCorrect(int input0, int input1, int output0, int output1)
        {
            Pred.SortInputs(ref input0, ref input1);
            Assert.True(input0 == output0 && input1 == output1);
        }

        [Theory]
        [InlineData(7.653f, 5.124f, 5.124f, 7.653f)]
        [InlineData(3f, 2f, 2f, 3f)]
        [InlineData(1f, 2f, 1f, 2f)]
        public void SortInputs_TwoFloats_OrderIsCorrect(float input0, float input1, float output0, float output1)
        {
            Pred.SortInputs(ref input0, ref input1);
            Assert.True(input0 == output0 && input1 == output1);
        }

        [Theory]
        [InlineData(7532, 1251, 6784, 1251, 6784, 7532)]
        [InlineData(9, 7, 8, 7, 8, 9)]
        [InlineData(1, 2, 3, 1, 2, 3)]
        [InlineData(6, 4, 5, 4, 5, 6)]
        [InlineData(1, 3, 2, 1, 2, 3)]
        [InlineData(1, 1, 1, 1, 1, 1)]
        [InlineData(2, 1, 2, 1, 2, 2)]
        public void SortInputs_ThreeInts_OrderIsCorrect(int input0, int input1, int input2, int output0, int output1, int output2)
        {
            Pred.SortInputs(ref input0, ref input1, ref input2);
            Assert.True(input0 == output0 && input1 == output1 && input2 == output2);
        }

        [Theory]
        [InlineData(7.532f, 1.251f, 6.784f, 1.251f, 6.784f, 7.532f)]
        [InlineData(9f, 7f, 8f, 7f, 8f, 9f)]
        [InlineData(1f, 2f, 3f, 1f, 2f, 3f)]
        [InlineData(6f, 4f, 5f, 4f, 5f, 6f)]
        [InlineData(1f, 3f, 2f, 1f, 2f, 3f)]
        [InlineData(1f, 1f, 1f, 1f, 1f, 1f)]
        [InlineData(2f, 1f, 2f, 1f, 2f, 2f)]
        public void SortInputs_ThreeFloats_OrderIsCorrect(float input0, float input1, float input2, float output0, float output1, float output2)
        {
            Pred.SortInputs(ref input0, ref input1, ref input2);
            Assert.True(input0 == output0 && input1 == output1 && input2 == output2);
        }

        [Theory]
        [InlineData(7532, 1251, 6784, false)]
        [InlineData(9, 7, 8, false)]
        [InlineData(1, 2, 3, false)]
        [InlineData(6, 4, 5, false)]
        [InlineData(1, 3, 2, true)]
        [InlineData(1, 1, 1, true)]
        [InlineData(2, 1, 2, true)]
        public void SortInputs_ThreeInts_IsFlipped(int input0, int input1, int input2, bool isFlipped)
        {
            Assert.Equivalent(Pred.SortInputs(ref input0, ref input1, ref input2), isFlipped);
        }

        [Theory]
        [InlineData(7.532f, 1.251f, 6.784f, false)]
        [InlineData(9f, 7f, 8f, false)]
        [InlineData(1f, 2f, 3f, false)]
        [InlineData(6f, 4f, 5f, false)]
        [InlineData(1f, 3f, 2f, true)]
        [InlineData(2f, 1f, 2f, true)]
        public void SortInputs_ThreeFloats_IsFlipped(float input0, float input1, float input2, bool isFlipped)
        {
            Assert.Equivalent(Pred.SortInputs(ref input0, ref input1, ref input2), isFlipped);
        }
    }
}
