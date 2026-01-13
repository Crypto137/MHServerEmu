using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Core.Extensions
{
    public static class WebExtensions
    {
        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        // These are extensions rather thanWebRequestContext methods to keep the latter free
        // from MHServerEmu specific dependencies.

        public static string GetIPAddressHandle(this WebRequestContext context, out string ipAddress)
        {
            ipAddress = context.GetIPAddress();
            // Hash the IP address to prevent it from appearing in logs if needed
            return HideSensitiveInformation ? $"0x{HashHelper.Djb2(ipAddress):X8}" : ipAddress;
        }

        public static string GetIPAddressHandle(this WebRequestContext context)
        {
            return context.GetIPAddressHandle(out _);
        }
    }
}
