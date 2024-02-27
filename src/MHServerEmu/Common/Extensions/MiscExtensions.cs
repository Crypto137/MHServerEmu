using System.Net;

namespace MHServerEmu.Common.Extensions
{
    public static class MiscExtensions
    {
        /// <summary>
        /// Returns a masked <see cref="string"/> representation of this <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string ToStringMasked(this IPEndPoint endpoint)
        {
            string address = endpoint.Address.ToString();
            return $"{address.Substring(0, address.Length / 2)}****:{endpoint.Port}";
        }
    }
}
