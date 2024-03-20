using MHServerEmu.Core.Extensions;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("player", "Changes player data for this account.", AccountUserLevel.User)]
    public class PlayerCommands : CommandGroup
    {
        [Command("avatar", "Changes player avatar.\nUsage: player avatar [avatar]", AccountUserLevel.User)]
        public string Avatar(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player avatar' to get help.";

            if (Enum.TryParse(@params[0], true, out AvatarPrototypeId avatar) == false)
                return $"Failed to change player avatar to {@params[0]}";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);

            playerConnection.Player.SetAvatar((PrototypeId)avatar);
            game.MovePlayerToRegion(playerConnection, playerConnection.RegionDataRef, playerConnection.WaypointDataRef);
            return $"Changing avatar to {avatar}.";
        }

        [Command("aoi", "Changes player AOI volume size.\nUsage: player aoi [value]", AccountUserLevel.User)]
        public string AOI(string[] @params, FrontendClient client)
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

        [Command("costume", "Changes costume for the current avatar.\nUsage: player costume [name|reset|default]", AccountUserLevel.User)]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player costume' to get help.";

            PrototypeId costumeId;

            switch (@params[0].ToLower())
            {
                case "reset":
                    costumeId = PrototypeId.Invalid;
                    break;

                case "default": // This undoes visual updates for most heroes
                    costumeId = (PrototypeId)HardcodedBlueprints.Costume;
                    break;

                default:
                    var matches = GameDatabase.SearchPrototypes(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, HardcodedBlueprints.Costume);

                    if (matches.Any() == false)
                        return $"Failed to find any costumes containing {@params[0]}.";

                    if (matches.Count() > 1)
                    {
                        ChatHelper.SendMetagameMessage(client, $"Found multiple matches for {@params[0]}:");
                        ChatHelper.SendMetagameMessages(client, matches.Select(match => GameDatabase.GetPrototypeName(match)), false);
                        return string.Empty;
                    }

                    costumeId = matches.First();
                    break;
            }

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            var player = playerConnection.Player;
            var avatar = player.CurrentAvatar;

            // Update player and avatar properties
            avatar.Properties[PropertyEnum.CostumeCurrent] = costumeId;
            player.Properties[PropertyEnum.AvatarLibraryCostume, 0, avatar.BaseData.PrototypeId] = costumeId;

            // Send client property updates (TODO: Remove this when we have those generated automatically)
            // Avatar entity
            client.SendMessage(1, Property.ToNetMessageSetProperty(
                avatar.Properties.ReplicationId, new(PropertyEnum.CostumeCurrent), costumeId));

            // Player entity
            PropertyParam enumValue = Property.ToParam(PropertyEnum.AvatarLibraryCostume, 1, avatar.BaseData.PrototypeId);
            client.SendMessage(1, Property.ToNetMessageSetProperty(
                player.Properties.ReplicationId, new(PropertyEnum.AvatarLibraryCostume, 0, enumValue), costumeId));

            if (costumeId == PrototypeId.Invalid)
                return "Resetting costume.";

            return $"Changing costume to {GameDatabase.GetPrototypeName(costumeId)}.";
        }
    }
}
