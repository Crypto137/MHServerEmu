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
    }
}
