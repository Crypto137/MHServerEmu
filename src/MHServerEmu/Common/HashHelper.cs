using System.Security.Cryptography;
using System.IO.Hashing;

namespace MHServerEmu.Common
{
    public static class HashHelper
    {
        public static ulong GenerateRandomId()
        {
            byte[] hash = MD5.HashData(BitConverter.GetBytes(DateTime.Now.Ticks));
            return BitConverter.ToUInt64(hash);
        }

        public static ulong GenerateUniqueRandomId<T>(Dictionary<ulong, T> dict)
        {
            ulong sessionId = GenerateRandomId();
            while (dict.ContainsKey(sessionId)) sessionId = GenerateRandomId();
            return sessionId;
        }

        public static uint Adler32(string str)
        {
            const int mod = 65521;
            uint a = 1, b = 0;
            foreach (char c in str)
            {
                a = (a + c) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }

        public static uint Crc32(string str)
        {
            byte[] hash = System.IO.Hashing.Crc32.Hash(System.Text.Encoding.UTF8.GetBytes(str));
            return BitConverter.ToUInt32(hash);
        }

        public static ulong HashPath(string path)
        {
            path = path.ToLower();
            ulong adler = Adler32(path);
            ulong crc = Crc32(path);
            return (adler | (crc << 32)) - 1;          
        }
    }
}
