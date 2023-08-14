using System.Security.Cryptography;

namespace MHServerEmu.Common
{
    public static class Cryptography
    {
        private const int PasswordKeySize = 64;
        private const int PasswordIterationCount = 350000;

        // The game uses AES-256 CBC encryption for auth
        // The real server probably generated a new key for each session, but until we have auth <-> frontend communication we'll use a static one
        public static readonly byte[] AuthEncryptionKey = Convert.FromBase64String("M/+i0JeQS/xWW5+FN4C1LVvc3nerQc3G3VoCcqWTC9A=");
        private static byte[] AuthEncryptionIV;

        public static void SetIV(byte[] iv)
        {
            AuthEncryptionIV = iv;
        }

        public static byte[] DecryptSessionToken(Gazillion.ClientCredentials clientCredentials)
        {
            byte[] decryptedToken;
            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.Key = AuthEncryptionKey;
                aesAlgorithm.IV = AuthEncryptionIV;

                ICryptoTransform decryptor = aesAlgorithm.CreateDecryptor();
                byte[] cipher = clientCredentials.EncryptedToken.ToByteArray();

                using (MemoryStream memoryStream = new(cipher))
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

        public static byte[] EncryptNumber(int number)
        {
            // Test encryption method for figuring out what the whole auth randomnumber thing is about

            byte[] encryptedNumber;

            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.Key = AuthEncryptionKey;
                aesAlgorithm.IV = AuthEncryptionIV;

                ICryptoTransform encryptor = aesAlgorithm.CreateEncryptor();

                using (MemoryStream memoryStream = new())
                {
                    using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (BinaryWriter binaryWriter = new(cryptoStream))
                        {
                            binaryWriter.Write(number);
                        }

                        encryptedNumber = memoryStream.ToArray();
                    }
                }
            }

            return encryptedNumber;
        }

        public static byte[] GenerateAesKey(int size)
        {
            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.KeySize = size;
                aesAlgorithm.GenerateKey();
                return aesAlgorithm.Key;
            }
        }

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
    }
}
