using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("region", "Changes player data for this account.", AccountUserLevel.User)]
    public class RegionCommands : CommandGroup
    {
        [Command("warp", "Warps the player to another region.\nUsage: region warp [name]", AccountUserLevel.User)]
        public string Warp(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help region warp' to get help.";

            PrototypeId regionProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Region, @params[0], client);
            if (regionProtoRef == PrototypeId.Invalid) return string.Empty;

            string regionName = GameDatabase.GetPrototypeName(regionProtoRef);

            // Check for unsafe warps (regions that are potentially missing assets and can make the client get stuck)
            bool allowUnsafe = client.Session.Account.UserLevel == AccountUserLevel.Admin && @params.Length > 1 && @params[1].ToLower() == "unsafe";
            if (allowUnsafe == false && Enum.GetValues<RegionPrototypeId>().Contains((RegionPrototypeId)regionProtoRef) == false)
                return $"Unsafe warp destination: {regionName}.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            game.MovePlayerToRegion(playerConnection, regionProtoRef, PrototypeId.Invalid);
            return $"Warping to {regionName}.";
        }

        [Command("reload", "Reloads the current region.\nUsage: region reload", AccountUserLevel.User)]
        public string Reload(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            game.MovePlayerToRegion(playerConnection, playerConnection.RegionDataRef, playerConnection.WaypointDataRef);
            return $"Reloading region {GameDatabase.GetPrototypeName(playerConnection.RegionDataRef)}.";
        }

        [Command("generateallsafe", "Generates all safe regions.\nUsage: region generateallsafe", AccountUserLevel.Admin)]
        public string GenerateAllSafe(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetGame(client, out Game game);

            int numRegions = 0;

            foreach (var value in Enum.GetValues<RegionPrototypeId>())
            {
                Task.Run(() => game.RegionManager.GetRegion(value));
                numRegions++;
            }

            return $"Generating {numRegions} regions.";
        }
    }
}
