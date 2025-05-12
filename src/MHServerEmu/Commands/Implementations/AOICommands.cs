using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("aoi")]
    [CommandGroupDescription("Commands for interacting with the invoker player's area of interest (AOI).")]
    public class AOICommands : CommandGroup
    {
        [Command("volume")]
        [CommandDescription("Changes player AOI volume size.")]
        [CommandUsage("aoi volume [value]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Volume(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;

            if (@params.Length == 0) return $"Current AOI volume = {playerConnection.AOI.AOIVolume}.";

            if (@params[0].ToLower() == "reset")
            {
                playerConnection.AOI.AOIVolume = 3200;
                return $"Resetting player AOI volume size to {playerConnection.AOI.AOIVolume}.";
            }

            if ((int.TryParse(@params[0], out int volume) && volume.IsWithin(1600, 5000)) == false)
                return $"Failed to change AOI volume size to {@params[0]}. Available range [1600..5000]";

            playerConnection.AOI.AOIVolume = volume;
            return $"Changed player AOI volume size to {volume}.";
        }

        [Command("print")]
        [CommandDescription("Prints player AOI information to the server console.")]
        [CommandUsage("aoi print")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Print(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            AdminCommandManager.SendAdminCommandResponseSplit(playerConnection, playerConnection.AOI.DebugPrint());

            return "AOI information printed to the console.";
        }

        [Command("update")]
        [CommandDescription("Forces AOI proximity update.")]
        [CommandUsage("aoi update")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Update(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;
            playerConnection.AOI.Update(avatar.RegionLocation.Position, true);

            return "AOI updated.";
        }

        [Command("refs")]
        [CommandDescription("Prints interest references for the current player.")]
        [CommandUsage("aoi refs")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Refs(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;
            Avatar avatar = player.CurrentAvatar;

            return $"Interest References\n(Player) {player.InterestReferences}\n(Avatar) {avatar.InterestReferences}";
        }
    }
}
