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
        [CommandDescription("Converts Eternity Splinters to Gs. Defaults to 100 if no amount specified.")] // MODIFIED Description
        [CommandUsage("store convertes [amount]")] // Usage still shows optional amount
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(0)] // CHANGED: Allows 0 parameters, making [amount] optional
        public string ConvertES(string[] @params, NetClient client)
        {
            int numToConvert;
            const int defaultConversionAmount = 100; // Default amount if none specified

            // Check if an amount parameter is provided
            if (@params.Length > 0)
            {
                if (!int.TryParse(@params[0], out numToConvert))
                {
                    return "Invalid amount specified. Usage: !store convertes [amount]";
                }

                if (numToConvert <= 0)
                {
                    return "Amount to convert must be a positive number.";
                }
            }
            else
            {
                // No amount provided, use the default
                numToConvert = defaultConversionAmount;
            }

            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            PropertyId esPropId = new(PropertyEnum.Currency, GameDatabase.CurrencyGlobalsPrototype.EternitySplinters);

            long esBalance = player.Properties[esPropId];
            if (esBalance < numToConvert)
            {
                return $"You need at least {numToConvert} Eternity Splinters to convert them to Gs. You have {esBalance}.";
            }

            var config = ConfigManager.Instance.GetConfig<BillingConfig>();
            long gAmount = Math.Max((long)(numToConvert * config.ESToGazillioniteConversionRatio), 0);

            if (gAmount == 0 && numToConvert > 0)
            {
                return "Current server settings do not allow Eternity Splinter to G conversion for this amount, or the conversion ratio is zero.";
            }

            if (player.AcquireGazillionite(gAmount) == false)
            {
                return "Failed to acquire Gs.";
            }

            player.Properties.AdjustProperty(-numToConvert, esPropId);

            return $"Converted {numToConvert} Eternity Splinters to {gAmount} Gs.";
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
