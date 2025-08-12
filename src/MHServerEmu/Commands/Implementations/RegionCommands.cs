using System.Diagnostics;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("region")]
    [CommandGroupDescription("Region management commands.")]
    public class RegionCommands : CommandGroup
    {
        [Command("warp")]
        [CommandDescription("Warps the player to another region.")]
        [CommandUsage("region warp [name]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Warp(string[] @params, NetClient client)
        {
            PrototypeId regionProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Region, @params[0], client);
            if (regionProtoRef == PrototypeId.Invalid) return string.Empty;

            RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
            if (regionProto == null) return $"Failed to load region prototype for id {regionProtoRef}";

            string regionName = GameDatabase.GetPrototypeName(regionProtoRef);

            // Check for unsafe warps (regions that are potentially missing assets and can make the client get stuck)
            DBAccount account = CommandHelper.GetClientAccount(client);
            bool allowUnsafe = account.UserLevel == AccountUserLevel.Admin && @params.Length > 1 && @params[1].ToLower() == "unsafe";
            if (allowUnsafe == false && Enum.GetValues<RegionPrototypeId>().Contains((RegionPrototypeId)regionProtoRef) == false)
                return $"Unsafe warp destination: {regionName}.";

            Player player = ((PlayerConnection)client).Player;
            Teleporter.DebugTeleportToTarget(player, regionProto.StartTarget);

            return $"Warping to {regionName}.";
        }

        [Command("reload")]
        [CommandDescription("Reloads the current region.")]
        [CommandUsage("region reload")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Reload(string[] @params, NetClient client)
        {
            Player player = ((PlayerConnection)client).Player;
            RegionPrototype regionProto = player.GetRegion()?.Prototype;
            Teleporter.DebugTeleportToTarget(player, regionProto.StartTarget);

            // TODO: Fix this for endless regions

            return $"Reloading region {regionProto}.";
        }

        [Command("generateallsafe")]
        [CommandDescription("Generates all safe regions.")]
        [CommandUsage("region generateallsafe")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string GenerateAllSafe(string[] @params, NetClient client)
        {
            /*
            PlayerConnection playerConnection = (PlayerConnection)client;
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
            */
            // TODO: Fix or remove this command.
            return "Command disabled";
        }

        [Command("properties")]
        [CommandDescription("Prints properties for the current region.")]
        [CommandUsage("region properties")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Properties(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Region region = playerConnection.Player.GetRegion();

            CommandHelper.SendMessageSplit(client, region.Properties.ToString());

            return string.Empty;
        }

        [Command("info")]
        [CommandDescription("Prints info for the current region.")]
        [CommandUsage("region info")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Info(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Region region = playerConnection.Player.GetRegion();

            return region?.ToString();
        }
    }
}
