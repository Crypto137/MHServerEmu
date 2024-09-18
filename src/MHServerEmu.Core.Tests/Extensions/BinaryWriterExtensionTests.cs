using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Core.Tests.Extensions
{
    public class BinaryWriterExtensionTests
    {
        [Theory]
        [InlineData(2, "020000")]
        [InlineData(7654321, "B1CB74")]
        [InlineData(16777215, "FFFFFF")]
        public void WriteUInt24_TestValue_ReturnsExpectedHexString(int testValue, string expectedHexString)
        {
            using (MemoryStream ms = new(3))
            using (BinaryWriter writer = new(ms))
            {
                writer.WriteUInt24(testValue);
                string hexString = Convert.ToHexString(ms.ToArray());
                Assert.Equal(expectedHexString, hexString);
            }
        }

    }
}
