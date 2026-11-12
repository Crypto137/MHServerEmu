using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Tests.Properties
{
    public class PropertyValueTests
    {
        [Theory]
        [InlineData(10f, 10f, 10f, 0x000028000140000A)]
        [InlineData(10000f, 10000f, 10000f, 0x009C4004E2002710)]
        [InlineData(-10f, -10f, -10f, 0x7FFFDBFFFEDFFFF6)]
        [InlineData(-10000f, -10000f, -10000f, 0x7F63C3FB1E1FD8F0)]
        [InlineData(0, 0, 0, 0)]
        public void PropertyValue_Vector3_EncodesToExpectedBits(float x, float y, float z, ulong expectedBits)
        {
            Vector3 v = new(x, y, z);
            PropertyValue propValue = new(v);
            ulong bits = (ulong)propValue.RawLong;
            Assert.Equal(expectedBits, bits);
        }

        [Theory]
        [InlineData(0x000028000140000A, 10f, 10f, 10f)]
        [InlineData(0x009C4004E2002710, 10000f, 10000f, 10000f)]
        [InlineData(0x7FFFDBFFFEDFFFF6, -10f, -10f, -10f)]
        [InlineData(0x7F63C3FB1E1FD8F0, -10000f, -10000f, -10000f)]
        [InlineData(0, 0, 0, 0)]
        public void PropertyValue_Vector3_DecodesToExpectedValues(ulong bits, float x, float y, float z)
        {
            PropertyValue propValue = new(bits);
            Vector3 v = propValue;

            Assert.Equal(x, v.X);
            Assert.Equal(y, v.Y);
            Assert.Equal(z, v.Z);
        }
    }
}
