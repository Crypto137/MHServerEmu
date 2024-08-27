using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Configs
{
    /// <summary>
    /// Contains data for the default <see cref="DBAccount"/> used when BypassAuth is enabled.
    /// </summary>
    public class DefaultPlayerDataConfig : ConfigContainer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public string PlayerName { get; private set; } = "Player";

        /// <summary>
        /// Returns a new <see cref="DBAccount"/> instance with data based on this <see cref="DefaultPlayerDataConfig"/>.
        /// </summary>
        public DBAccount InitializeDefaultAccount()
        {
            DBAccount account = new(PlayerName);
            account.Player = new(account.Id);

            return account;
        }
    }
}
