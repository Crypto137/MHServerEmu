using System.Security.Cryptography;
using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Tests.Helpers
{
    public class CryptographyHelperTests
    {
        private static byte[] Encrypt(byte[] token, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                return aes.EncryptCbc(token, iv);
            }
        }

        [Fact]
        public void TryDecryptToken_ValidInput_RoundTrips()
        {
            byte[] key = CryptographyHelper.GenerateAesKey();
            byte[] token = CryptographyHelper.GenerateToken();
            byte[] iv = RandomNumberGenerator.GetBytes(16);
            byte[] encryptedToken = Encrypt(token, key, iv);

            Assert.True(CryptographyHelper.TryDecryptToken(encryptedToken, key, iv, out byte[] decryptedToken));
            Assert.Equal(token, decryptedToken);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(17)]
        [InlineData(32)]
        public void TryDecryptToken_InvalidIvLength_ReturnsFalse(int ivLength)
        {
            byte[] key = CryptographyHelper.GenerateAesKey();
            byte[] encryptedToken = Encrypt(CryptographyHelper.GenerateToken(), key, RandomNumberGenerator.GetBytes(16));

            Assert.False(CryptographyHelper.TryDecryptToken(encryptedToken, key, new byte[ivLength], out byte[] decryptedToken));
            Assert.Null(decryptedToken);
        }

        [Fact]
        public void TryDecryptToken_NullIv_ReturnsFalse()
        {
            byte[] key = CryptographyHelper.GenerateAesKey();
            byte[] encryptedToken = Encrypt(CryptographyHelper.GenerateToken(), key, RandomNumberGenerator.GetBytes(16));

            Assert.False(CryptographyHelper.TryDecryptToken(encryptedToken, key, null, out byte[] decryptedToken));
            Assert.Null(decryptedToken);
        }
    }
}
