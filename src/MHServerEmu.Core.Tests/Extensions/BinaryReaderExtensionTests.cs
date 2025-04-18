using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Core.Tests.Extensions
{
    public class BinaryReaderExtensionTests
    {
        [Theory]
        [InlineData("0000", "")]
        [InlineData("060042414E414E41", "BANANA")]
        [InlineData("1E00616C6C20796F75722062617365206172652062656C6F6E6720746F207573", "all your base are belong to us")]
        public void ReadFixedString16_HexString_ReturnsExpectedString(string hexString, string expectedString)
        {
            using (MemoryStream ms = new(Convert.FromHexString(hexString)))
            using (BinaryReader reader = new(ms))
            {
                string @string = reader.ReadFixedString16();
                Assert.Equal(expectedString, @string);
            }
        }

        [Theory]
        [InlineData("00000000", "")]
        [InlineData("060000004B4157414949", "KAWAII")]
        [InlineData("170000006E6576657220676F6E6E61206769766520796F75207570", "never gonna give you up")]
        public void ReadFixedString32_HexString_ReturnsExpectedString(string hexString, string expectedString)
        {
            using (MemoryStream ms = new(Convert.FromHexString(hexString)))
            using (BinaryReader reader = new(ms))
            {
                string @string = reader.ReadFixedString32();
                Assert.Equal(expectedString, @string);
            }
        }
    }
}
