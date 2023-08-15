using System.Security.Cryptography;

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
    }
}
