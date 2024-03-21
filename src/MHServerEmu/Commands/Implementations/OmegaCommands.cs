using MHServerEmu.Core.Config;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("omega", "Manages the Omega system.", AccountUserLevel.User)]
    public class OmegaCommands : CommandGroup
    {
        [Command("points", "Adds omega points.\nUsage: omega points", AccountUserLevel.User)]
        public string Points(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            var config = ConfigManager.Instance.GetConfig<GameOptionsConfig>();
            if (config.InfinitySystemEnabled) return "Set InfinitySystemEnabled to false in Config.ini to enable the Omega system.";

            client.SendMessage(1, Property.ToNetMessageSetProperty(9078332, new(PropertyEnum.OmegaPoints), 7500));
            return "Setting Omega points to 7500.";
        }
    }
}
