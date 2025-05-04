using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MTXStore;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("store")]
    [CommandGroupDescription("Commands for interacting with the in-game store.")]
    public class StoreCommands : CommandGroup
    {
        [Command("convertes")]
        [CommandDescription("Converts 100 Eternity Splinters to the equivalent amount of Gs.")]
        [CommandUsage("store convertes")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ConvertES(string[] @params, NetClient client)
        {
            const int NumConverted = 100;

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            PropertyId esPropId = new(PropertyEnum.Currency, GameDatabase.CurrencyGlobalsPrototype.EternitySplinters);

            long esBalance = player.Properties[esPropId];
            if (esBalance < NumConverted)
                return $"You need at least {NumConverted} Eternity Splinters to convert them to Gs.";

            var config = ConfigManager.Instance.GetConfig<BillingConfig>();
            long gAmount = Math.Max((long)(NumConverted * config.ESToGazillioniteConversionRatio), 0);
            if (gAmount == 0)
                return "Current server settings do not allow Eternity Splinter to G conversion.";

            if (player.AcquireGazillionite(gAmount) == false)
                return "Failed to acquire Gs.";

            player.Properties.AdjustProperty(-NumConverted, esPropId);

            return $"Converted {NumConverted} Eternity Splinters to {gAmount} Gs.";
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
