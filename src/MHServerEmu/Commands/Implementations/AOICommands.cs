using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Extensions;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("aoi", "Provides commands for interacting with this player's area of interest (AOI).")]
    public class AOICommands : CommandGroup
    {
        [Command("volume", "Changes player AOI volume size.\nUsage: aoi volume [value]", AccountUserLevel.User)]
        public string Volume(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

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

        [Command("print", "Prints player AOI information to the server console.\nUsage: aoi print", AccountUserLevel.Admin)]
        public string Print(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            AdminCommandManager.SendAdminCommandResponseSplit(playerConnection, playerConnection.AOI.DebugPrint());

            return "AOI information printed to the console.";
        }

        [Command("update", "Forces AOI proximity update.\nUsage: aoi update", AccountUserLevel.Admin)]
        public string Update(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;
            playerConnection.AOI.Update(avatar.RegionLocation.Position, true);

            return "AOI updated.";
        }

        [Command("refs", "Prints interest references for the current player.\nUsage: aoi refs", AccountUserLevel.Admin)]
        public string Refs(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;
            Avatar avatar = player.CurrentAvatar;

            return $"Interest References\n(Player) {player.InterestReferences}\n(Avatar) {avatar.InterestReferences}";
        }
    }
}
