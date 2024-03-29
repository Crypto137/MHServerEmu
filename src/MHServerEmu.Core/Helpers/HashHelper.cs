﻿using System.Text;
using Free.Ports.zLib;

namespace MHServerEmu.Core.Helpers
{
    /// <summary>
    /// Provides various hashing functionality.
    /// </summary>
    public static class HashHelper
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
        /// Hashes a <see cref="string"/> using the djb2 algorithm.
        /// </summary>
        public static uint Djb2(string str)
        {
            uint hash = 5381;
            for (int i = 0; i < str.Length; i++)
                hash = (hash << 5) + hash + ((byte)str[i]);
            return hash;
        }

        /// <summary>
        /// Hashes a path with Adler32 and CRC32 to make a <see cref="ulong"/> value that can be used as a DataRef.
        /// </summary>
        public static ulong HashPath(string path)
        {
            // Hashes generated by this method are used as DataRefs.
            // Calligraphy and resource data file paths are prepared for hashing differently (this may be done to avoid hash collisions):
            //      Calligraphy:    1) Replace '.' with '?' 2) Replace '/' with '.'
            //      Resource:       Insert '&' as the first char
            // Use ArrayExtensions.ToCalligraphyPath() to process Calligraphy paths before hashing.
            // NOTE: default prototypes reuse hashes of blueprints they are paired with.

            // We subtract 1 from the resulting hash so that the hash for an empty string is 0, which is used as an invalid data ref.
            // This is because the output of an empty string is 0 for CRC32 and 1 for Adler32.
            path = path.ToLower();
            ulong adler = Adler32(path);
            ulong crc = Crc32(path);
            return (adler | (crc << 32)) - 1;
        }
    }
}
