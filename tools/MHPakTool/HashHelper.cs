using System.Text;

namespace MHPakTool
{
    public class HashHelper
    {
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

        public static uint Crc32(byte[] bytes)
        {
            byte[] hash = System.IO.Hashing.Crc32.Hash(bytes);
            return BitConverter.ToUInt32(hash);
        }

        public static uint Crc32(string str) => Crc32(Encoding.UTF8.GetBytes(str));

        /// <summary>
        /// Hashes a path with Adler32 and Crc32.
        /// </summary>
        public static ulong HashPath(string path)
        {
            path = path.ToLower();
            ulong adler = Adler32(path);
            ulong crc = Crc32(path);
            return (adler | (crc << 32)) - 1;
        }
    }
}
