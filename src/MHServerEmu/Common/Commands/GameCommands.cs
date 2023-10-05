using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("tower", "Changes region to Avengers Tower (original).", AccountUserLevel.User)]
    public class TowerCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Tower(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            client.CurrentGame.MovePlayerToRegion(client, RegionPrototype.AvengersTowerHUBRegion);

            return "Changing region to Avengers Tower (original)";
        }
    }

    [CommandGroup("doop", "Travel to Cosmic Doop Sector.", AccountUserLevel.User)]
    public class DoopCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Doop(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            client.CurrentGame.MovePlayerToRegion(client, RegionPrototype.CosmicDoopSectorSpaceRegion);

            return "Travel to Cosmic Doop Sector";
        }
    }

    [CommandGroup("player", "Changes player data for this account.", AccountUserLevel.User)]
    public class PlayerCommand : CommandGroup
    {
        [Command("avatar", "Changes player avatar.\nUsage: player avatar [avatar]", AccountUserLevel.User)]
        public string Avatar(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player avatar' to get help.";
            if (ConfigManager.Frontend.BypassAuth) return "Disable BypassAuth to use this command";

            if (Enum.TryParse(typeof(AvatarPrototype), @params[0], true, out object avatar))
            {
                client.Session.Account.Player.Avatar = (AvatarPrototype)avatar;
                return $"Changing avatar to {client.Session.Account.Player.Avatar}. Relog for changes to take effect.";
            }
            else
            {
                return $"Failed to change player avatar to {@params[0]}";
            }
        }

        [Command("region", "Changes player starting region.\nUsage: player region", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player region' to get help.";
            if (ConfigManager.Frontend.BypassAuth) return "Disable BypassAuth to use this command";

            if (Enum.TryParse(typeof(RegionPrototype), @params[0], true, out object region))
            {
                client.Session.Account.Player.Region = (RegionPrototype)region;
                return $"Changing starting region to {client.Session.Account.Player.Region}. Relog for changes to take effect.";
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
                ulong prototypeId = ulong.Parse(@params[0]);
                string prototypePath = GameDatabase.GetPrototypePath(prototypeId);

                if (prototypeId == 0 || prototypePath.Contains("Entity/Items/Costumes/Prototypes/"))
                {
                    // Create a new CostumeCurrent property for the purchased costume
                    Property property = new(PropertyEnum.CostumeCurrent, prototypeId);

                    // Get replication id for the client avatar
                    ulong replicationId = (ulong)client.Session.Account.Player.Avatar.ToPropertyCollectionReplicationId();

                    // Update account data if needed
                    if (ConfigManager.Frontend.BypassAuth == false) client.Session.Account.CurrentAvatar.Costume = prototypeId;

                    // Send NetMessageSetProperty message
                    client.SendMessage(1, new(property.ToNetMessageSetProperty(replicationId)));
                    return $"Changing costume to {GameDatabase.GetPrototypePath(prototypeId)}";
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
