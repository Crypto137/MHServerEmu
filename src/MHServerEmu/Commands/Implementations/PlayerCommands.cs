using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("player", "Changes player data for this account.", AccountUserLevel.User)]
    public class PlayerCommand : CommandGroup
    {
        [Command("avatar", "Changes player avatar.\nUsage: player avatar [avatar]", AccountUserLevel.User)]
        public string Avatar(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player avatar' to get help.";

            if (Enum.TryParse(typeof(AvatarPrototypeId), @params[0], true, out object avatar))
            {
                CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);

                playerConnection.Player.SetAvatar((PrototypeId)avatar);
                game.MovePlayerToRegion(playerConnection, playerConnection.RegionDataRef, playerConnection.WaypointDataRef);
                return $"Changing avatar to {avatar}.";
            }
            else
            {
                return $"Failed to change player avatar to {@params[0]}";
            }
        }

        [Command("AOIVolume", "Changes player AOI volume size.\nUsage: player AOIVolume", AccountUserLevel.User)]
        public string AOIVolume(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            if (@params.Length == 0) return $"Current AOI volume = {playerConnection.AOI.AOIVolume}";
            //if (ConfigManager.PlayerManager.BypassAuth) return "Disable BypassAuth to use this command";

            if (int.TryParse(@params[0], out int volume) && volume >= 1600 && volume <= 5000)
            {
                playerConnection.AOI.AOIVolume = volume;
                return $"Changed player AOI volume size to {volume}.";
            }
            else
            {
                return $"Failed to change AOI volume size to {@params[0]}. Available range [1600..5000]";
            }
        }

        [Command("region", "Changes player region.\nUsage: player region", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player region' to get help.";

            if (Enum.TryParse(@params[0], true, out RegionPrototypeId region))
            {
                CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);

                game.MovePlayerToRegion(playerConnection, (PrototypeId)region, 0);
                return $"Changing region to {region}.";
            }
            else
            {
                return $"Failed to change starting region to {@params[0]}";
            }
        }

        [Command("costume", "Changes costume override.\nUsage: player costume [prototypeId]", AccountUserLevel.User)]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player costume' to get help.";

            try
            {
                // Try to parse costume prototype id from command
                var prototypeId = (PrototypeId)ulong.Parse(@params[0]);
                string prototypePath = GameDatabase.GetPrototypeName(prototypeId);

                if (prototypeId == 0 || prototypePath.Contains("Entity/Items/Costumes/Prototypes/"))
                {
                    CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
                    var player = playerConnection.Player;
                    var avatar = player.CurrentAvatar;

                    // Update player and avatar properties
                    avatar.Properties[PropertyEnum.CostumeCurrent] = prototypeId;
                    player.Properties[PropertyEnum.AvatarLibraryCostume, 0, avatar.BaseData.PrototypeId] = prototypeId;

                    // Send client property updates (TODO: Remove this when we have those generated automatically)
                    // Avatar entity
                    client.SendMessage(1, Property.ToNetMessageSetProperty(
                        avatar.Properties.ReplicationId, new(PropertyEnum.CostumeCurrent), prototypeId));

                    // Player entity
                    PropertyParam enumValue = Property.ToParam(PropertyEnum.AvatarLibraryCostume, 1, avatar.BaseData.PrototypeId);
                    client.SendMessage(1, Property.ToNetMessageSetProperty(
                        player.Properties.ReplicationId, new(PropertyEnum.AvatarLibraryCostume, 0, enumValue), prototypeId));

                    return $"Changing costume to {GameDatabase.GetPrototypeName(prototypeId)}";
                }
                else
                {
                    return $"{prototypeId} is not a costume prototype id";
                }
            }
            catch
            {
                return $"Failed to parse costume id {@params[0]}.";
            }
        }
    }
}
