using MHServerEmu.Core.Extensions;
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

            var matches = GameDatabase.SearchPrototypes(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, HardcodedBlueprints.Region);

            if (matches.Any() == false)
                return $"Failed to find any regions containing {@params[0]}.";

            if (matches.Count() > 1)
            {
                ChatHelper.SendMetagameMessage(client, $"Found multiple matches for {@params[0]}:");
                ChatHelper.SendMetagameMessages(client, matches.Select(match => GameDatabase.GetPrototypeName(match)), false);
                return string.Empty;
            }

            PrototypeId regionId = matches.First();
            string regionName = GameDatabase.GetPrototypeName(regionId);

            // Check for unsafe warps (regions that are potentially missing assets and can make the client get stuck)
            bool allowUnsafe = client.Session.Account.UserLevel == AccountUserLevel.Admin && @params.Length > 1 && @params[1].ToLower() == "unsafe";
            if (allowUnsafe == false && Enum.GetValues<RegionPrototypeId>().Contains((RegionPrototypeId)regionId) == false)
                return $"Unsafe warp destination: {regionName}.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            game.MovePlayerToRegion(playerConnection, matches.First(), PrototypeId.Invalid);
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
    }
}
