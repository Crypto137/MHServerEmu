using System.Security.Cryptography;

namespace MHServerEmu.Common
{
    public static class Cryptography
    {
        private const int PasswordKeySize = 64;
        private const int PasswordIterationCount = 210000;  // Appropriate iteration count for PBKDF2-HMAC-SHA512 according to 2023 OWASP recommendations

        public static byte[] HashPassword(string password, out byte[] salt)
        {
            salt = RandomNumberGenerator.GetBytes(PasswordKeySize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterationCount, HashAlgorithmName.SHA512, PasswordKeySize);
            return hash;
        }

        public static bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            byte[] hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterationCount, HashAlgorithmName.SHA512, PasswordKeySize);
            return CryptographicOperations.FixedTimeEquals(hashToCompare, hash);
        }

        public static byte[] GenerateToken()
        {
            // The game uses AES-256 CBC encryption for tokens.
            // AuthServer sends a token and a session key, and when the client connects to FES it adds IV, encrypts the token and sends it
            return RandomNumberGenerator.GetBytes(256);
        }

        public static byte[] GenerateAesKey(int size = 256)
        {
            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.KeySize = size;
                aesAlgorithm.GenerateKey();
                return aesAlgorithm.Key;
            }
        }

        public static byte[] DecryptToken(byte[] token, byte[] key, byte[] iv)
        {
            byte[] decryptedToken;
            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.Key = key;
                aesAlgorithm.IV = iv;

                ICryptoTransform decryptor = aesAlgorithm.CreateDecryptor();

                using (MemoryStream memoryStream = new(token))
                {
                    using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream decryptionBuffer = new())
                        {
                            cryptoStream.CopyTo(decryptionBuffer);
                            decryptedToken = decryptionBuffer.ToArray();
                        }
                    }
                }
            }

            return decryptedToken;
        }

        public static bool VerifyToken(byte[] credentialsToken, byte[] sessionToken)
        {
            return CryptographicOperations.FixedTimeEquals(credentialsToken, sessionToken);
        }
    }
}
