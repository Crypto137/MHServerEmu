using System.Text;
using Free.Ports.zLib;

namespace MHPakTool
{
    public class HashHelper
    {
        /// <summary>
        /// Hashes a <see cref="string"/> using the Adler32 algorithm.
        /// </summary>
        public static uint Adler32(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return zlib.adler32(1, bytes, (uint)bytes.Length);
        }

        /// <summary>
        /// Hashes a <see cref="byte"/> array using the CRC32 algorithm.
        /// </summary>
        public static uint Crc32(byte[] bytes)
        {
            return zlib.crc32(0, bytes, bytes.Length);
        }

        /// <summary>
        /// Hashes a <see cref="string"/> using the CRC32 algorithm.
        /// </summary>
        public static uint Crc32(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return zlib.crc32(0, bytes, bytes.Length);
        }

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
