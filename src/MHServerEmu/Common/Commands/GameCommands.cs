using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Entities;
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

    [CommandGroup("costume", "Changes costume.", AccountUserLevel.Admin)]
    public class CostumeCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.Admin)]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help costume' to get help.";

            try
            {
                // Try to parse costume prototype id from command
                ulong costumePrototypeId = ulong.Parse(@params[0]);

                // Create a new CostumeCurrent property
                Property property = new(0x19e0000000000000, 0);
                property.Value.Set(costumePrototypeId);

                // Get replication id for the client avatar
                ulong replicationId = (ulong)Enum.Parse(typeof(HardcodedAvatarReplicationId), Enum.GetName(typeof(HardcodedAvatarEntity), client.Session.Account.PlayerData.Avatar));

                // Send NetMessageSetProperty message
                client.SendMessage(1, new(property.ToNetMessageSetProperty(replicationId)));
                return $"Changing costume to {GameDatabase.GetPrototypePath(costumePrototypeId)}";
            }
            catch
            {
                return $"Failed to parse costume id {@params[0]}.";
            }            
        }
    }

    [CommandGroup("player", "Changes player data for this account.", AccountUserLevel.User)]
    public class PlayerCommand : CommandGroup
    {
        [Command("name", "Usage: player name", AccountUserLevel.User)]
        public string Name(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player name' to get help.";
            if (ConfigManager.Frontend.BypassAuth) return "Disable BypassAuth to use this command";

            client.Session.Account.PlayerData.PlayerName = @params[0];
            return $"Changing player name to {@params[0]}. Relog for changes to take effect.";
        }

        [Command("avatar", "Usage: player avatar [avatar]", AccountUserLevel.User)]
        public string Avatar(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player avatar' to get help.";
            if (ConfigManager.Frontend.BypassAuth) return "Disable BypassAuth to use this command";

            if (Enum.TryParse(typeof(HardcodedAvatarEntity), @params[0], true, out object avatar))
            {
                client.Session.Account.PlayerData.Avatar = (HardcodedAvatarEntity)avatar;
                return $"Changing avatar to {client.Session.Account.PlayerData.Avatar}. Relog for changes to take effect.";
            }
            else
            {
                return $"Failed to change player avatar to {@params[0]}";
            }
        }

        [Command("region", "Usage: player region", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player region' to get help.";
            if (ConfigManager.Frontend.BypassAuth) return "Disable BypassAuth to use this command";

            if (Enum.TryParse(typeof(RegionPrototype), @params[0], true, out object region))
            {
                client.Session.Account.PlayerData.Region = (RegionPrototype)region;
                return $"Changing starting region to {client.Session.Account.PlayerData.Region}. Relog for changes to take effect.";
            }
            else
            {
                return $"Failed to change starting region to {@params[0]}";
            }
        }

        [Command("costume", "Usage: player costume", AccountUserLevel.User)]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player costume' to get help.";
            if (ConfigManager.Frontend.BypassAuth) return "Disable BypassAuth to use this command";

            try
            {
                // Try to parse costume prototype id from command
                ulong costumePrototypeId = ulong.Parse(@params[0]);
                string costumeName = Path.GetFileNameWithoutExtension(GameDatabase.GetPrototypePath(costumePrototypeId));

                client.Session.Account.PlayerData.CostumeOverride = costumePrototypeId;
                return $"Changing costume to {costumeName}. Relog for changes to take effect.";
            }
            catch
            {
                return $"Failed to parse costume id {@params[0]}.";
            }
        }
    }
}
