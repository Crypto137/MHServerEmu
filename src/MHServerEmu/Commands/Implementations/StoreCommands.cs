using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.MTXStore;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("store")]
    [CommandGroupDescription("Commands for interacting with the in-game store.")]
    public class StoreCommands : CommandGroup
    {
        [Command("convertes")]
        [CommandDescription("Converts Eternity Splinters to the equivalent amount of Gs. Defaults to 100 Eternity Splinters if no value is specified.")]
        [CommandUsage("store convertes [amount]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ConvertES(string[] @params, NetClient client)
        {
            const int DefaultESAmount = 100;

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            int esAmount = DefaultESAmount;
            if (@params.Length > 0)
            {
                if (int.TryParse(@params[0], out esAmount) == false)
                    return $"'{@params[0]}' is not a valid Eternity Splinter amount.";

                // Floor to the nearest step value if needed
                int step = Math.Max(ConfigManager.Instance.GetConfig<MTXStoreConfig>().ESToGazillioniteConversionStep, 1);
                if (step > 1)
                    esAmount = esAmount / step * step;
            }

            int gAmount = player.ConvertEternitySplintersToGazillionite(esAmount);

            if (gAmount == 0)
                return "Failed to convert.";

            return $"Converted {esAmount} Eternity Splinters to {gAmount} Gs.";
        }

        [Command("addg")]
        [CommandDescription("Adds the specified number of Gs to this account.")]
        [CommandUsage("store addg [amount]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string AddG(string[] @params, NetClient client)
        {
            if (long.TryParse(@params[0], out long amount) == false)
                return $"Failed to parse argument {@params[0]}.";

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            if (player.AcquireGazillionite(amount) == false)
                return "Failed to acquire Gs.";

            return $"Acquired {amount} Gs.";
        }
    }
}
