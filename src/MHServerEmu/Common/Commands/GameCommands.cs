using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Common;
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

    [CommandGroup("position", "Shows current position.", AccountUserLevel.User)]
    public class PositionCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Position(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";            
            return $"Current position: {client.LastPosition}";
        }
    }

    [CommandGroup("dance", "Performs the Dance emote", AccountUserLevel.User)]
    public class DanceCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Dance(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            AvatarPrototype avatar = client.Session.Account.Player.Avatar;
            switch (avatar)
            {
                case AvatarPrototype.BlackPanther:
                case AvatarPrototype.BlackWidow:
                case AvatarPrototype.CaptainAmerica:
                case AvatarPrototype.Colossus:
                case AvatarPrototype.EmmaFrost:
                case AvatarPrototype.Hulk:
                case AvatarPrototype.IronMan:
                case AvatarPrototype.RocketRaccoon:
                case AvatarPrototype.ScarletWitch:
                case AvatarPrototype.Spiderman:
                case AvatarPrototype.Storm:
                case AvatarPrototype.Thing:
                case AvatarPrototype.Thor:
                    client.CurrentGame.EventManager.AddEvent(client, GameServer.Games.EventEnum.EmoteDance, 0, avatar);
                    return $"{avatar} begins to dance";
                default:
                    return $"{avatar} doesn't want to dance";                   
            }
            
        }
    }

    [CommandGroup("tp", "Teleports to position.\nUsage:\ntp x:+1000 (relative to current position)\ntp x100 y500 z10 (absolute position)", AccountUserLevel.User)]
    public class TeleportCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Teleport(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help teleport' to get help.";
            
            float x = 0f, y = 0f, z = 0f;
            foreach (string param in @params)
            {
                switch (param[0])
                {
                    case 'x':
                        if (float.TryParse(param.AsSpan(1), out x) == false) x = 0f;
                        break;

                    case 'y':
                        if (float.TryParse(param.AsSpan(1), out y) == false) y = 0f;
                        break;

                    case 'z':
                        if (float.TryParse(param.AsSpan(1), out z) == false) z = 0f;
                        break;

                    default:
                        return $"Invalid parameter: {param}";
                }                    
            }

            Vector3 teleportPoint = new(x, y, z);

            if (@params.Length < 3)
                teleportPoint += client.LastPosition;

            client.CurrentGame.EventManager.AddEvent(client, GameServer.Games.EventEnum.ToTeleport, 0, teleportPoint);
            return $"Teleporting to {teleportPoint}";
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

    [CommandGroup("omega", "Manages the Omega system.", AccountUserLevel.User)]
    public class OmegaCommand : CommandGroup
    {
        [Command("points", "Adds omega points.\nUsage: omega points", AccountUserLevel.User)]
        public string Points(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (ConfigManager.GameOptions.InfinitySystemEnabled) return "Set InfinitySystemEnabled to false in Config.ini to enable the Omega system.";

            GameMessage[] messages = new GameMessage[]
            {
                new(new Property(PropertyEnum.OmegaPoints, 7500).ToNetMessageSetProperty(9078332)),
                //new(NetMessageOmegaPointGain.CreateBuilder().SetNumPointsGained(7500).SetAvatarId((ulong)client.Session.Account.Player.Avatar.ToEntityId()).Build()),
                //new(new Property(PropertyEnum.OmegaPointsSpent, 5000).ToNetMessageSetProperty((ulong)client.Session.Account.Player.Avatar.ToEntityId()))
            };

            client.SendMessages(1, messages);

            return "Setting Omega points to 7500.";
        }
    }
}
