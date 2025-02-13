using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Config;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MTXStore;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("store", "Commands for interacting with the in-game store.", AccountUserLevel.User)]
    public class StoreCommands : CommandGroup
    {
        [Command("convertes", "Converts 100 Eternity Splinters to the equivalent amount of Gs.\nUsage: store convertes", AccountUserLevel.User)]
        public string ConvertES(string[] @params, FrontendClient client)
        {
            const int NumConverted = 100;

            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
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

        [Command("addg", "Adds the specified number of Gs to this account.\nUsage: store addg [amount]", AccountUserLevel.Admin)]
        public string AddG(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help store addg' to get help.";

            if (long.TryParse(@params[0], out long amount) == false)
                return $"Failed to parse argument {@params[0]}.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            if (player.AcquireGazillionite(amount) == false)
                return "Failed to acquire Gs.";

            return $"Acquired {amount} Gs.";
        }
    }
}
