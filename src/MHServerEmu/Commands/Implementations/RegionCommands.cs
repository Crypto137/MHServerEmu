using System.Diagnostics;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("region", "Manages region instances.", AccountUserLevel.Admin)]
    public class RegionCommands : CommandGroup
    {
        [Command("warp", "Warps the player to another region.\nUsage: region warp [name]")]
        public string Warp(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help region warp' to get help.";

            PrototypeId regionProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Region, @params[0], client);
            if (regionProtoRef == PrototypeId.Invalid) return string.Empty;

            RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
            if (regionProto == null) return $"Failed to load region prototype for id {regionProtoRef}";

            string regionName = GameDatabase.GetPrototypeName(regionProtoRef);

            // Check for unsafe warps (regions that are potentially missing assets and can make the client get stuck)
            bool allowUnsafe = client.Session.Account.UserLevel == AccountUserLevel.Admin && @params.Length > 1 && @params[1].ToLower() == "unsafe";
            if (allowUnsafe == false && Enum.GetValues<RegionPrototypeId>().Contains((RegionPrototypeId)regionProtoRef) == false)
                return $"Unsafe warp destination: {regionName}.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            playerConnection.MoveToTarget(regionProto.StartTarget);
            return $"Warping to {regionName}.";
        }

        [Command("reload", "Reloads the current region.\nUsage: region reload")]
        public string Reload(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);

            playerConnection.MoveToTarget(playerConnection.TransferParams.DestTargetProtoRef);

            return $"Reloading region {playerConnection.TransferParams.DestTargetRegionProtoRef.GetName()}.";
        }

        [Command("generateallsafe", "Generates all safe regions.\nUsage: region generateallsafe", AccountUserLevel.Admin)]
        public string GenerateAllSafe(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Game game = playerConnection.Game;
            RegionContext regionContext = new();

            int numRegions = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            foreach (RegionPrototypeId value in Enum.GetValues<RegionPrototypeId>())
            {
                regionContext.RegionDataRef = (PrototypeId)value;
                game.RegionManager.GetOrGenerateRegionForPlayer(regionContext, playerConnection);
                numRegions++;
            }

            stopwatch.Stop();
            return $"Generated {numRegions} regions in {stopwatch.Elapsed.TotalSeconds} sec.";
        }

        [Command("properties", "Prints properties for the current region.\nUsage: region properties")]
        public string Properties(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Region region = playerConnection.Player.GetRegion();

            ChatHelper.SendMetagameMessageSplit(client, region.Properties.ToString());

            return string.Empty;
        }
    }
}
