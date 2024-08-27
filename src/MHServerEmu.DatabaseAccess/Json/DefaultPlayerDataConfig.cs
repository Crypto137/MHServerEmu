using MHServerEmu.Core.Config;

namespace MHServerEmu.DatabaseAccess.Json
{
    /// <summary>
    /// Contains data for the default account used when BypassAuth is enabled.
    /// </summary>
    public class DefaultPlayerDataConfig : ConfigContainer
    {
        public string PlayerName { get; private set; } = "Player";
    }
}
