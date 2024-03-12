using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu.Core.Commands
{
    [CommandGroup("tower", "Changes region to Avengers Tower (original).", AccountUserLevel.User)]
    public class TowerCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Tower(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            client.CurrentGame.MovePlayerToRegion(client, RegionPrototypeId.AvengersTowerHUBRegion, (PrototypeId)15322252936284737788);

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

            client.CurrentGame.MovePlayerToRegion(client, RegionPrototypeId.CosmicDoopSectorSpaceRegion, (PrototypeId)15872240608618488803);

            return "Travel to Cosmic Doop Sector";
        }
    }

    [CommandGroup("Bovineheim", "Travel to Bovineheim.", AccountUserLevel.User)]
    public class BovineCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Bovineheim(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            client.CurrentGame.MovePlayerToRegion(client, (RegionPrototypeId)17913362697985334451, (PrototypeId)12083387244127461092);

            return "Travel to Bovineheim";
        }
    }

    [CommandGroup("Cow", "Travel to Classified Bovine Sector.", AccountUserLevel.User)]
    public class CowCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Cow(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            client.CurrentGame.MovePlayerToRegion(client, (RegionPrototypeId)12735255224807267622, (PrototypeId)2342633323497265984);

            return "Travel to Classified Bovine Sector.";
        }
    }

    [CommandGroup("position", "Shows current position.", AccountUserLevel.User)]
    public class PositionCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Position(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";            
            return $"Current position: {client.LastPosition.ToStringNames()}";
        }
    }

    [CommandGroup("dance", "Performs the Dance emote", AccountUserLevel.User)]
    public class DanceCommand : CommandGroup
    {
        [DefaultCommand(AccountUserLevel.User)]
        public string Dance(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            AvatarPrototypeId avatar = client.Session.Account.Player.Avatar;
            switch (avatar)
            {
                case AvatarPrototypeId.BlackPanther:
                case AvatarPrototypeId.BlackWidow:
                case AvatarPrototypeId.CaptainAmerica:
                case AvatarPrototypeId.Colossus:
                case AvatarPrototypeId.EmmaFrost:
                case AvatarPrototypeId.Hulk:
                case AvatarPrototypeId.IronMan:
                case AvatarPrototypeId.RocketRaccoon:
                case AvatarPrototypeId.ScarletWitch:
                case AvatarPrototypeId.Spiderman:
                case AvatarPrototypeId.Storm:
                case AvatarPrototypeId.Thing:
                case AvatarPrototypeId.Thor:
                    client.CurrentGame.EventManager.AddEvent(client, Games.Events.EventEnum.EmoteDance, 0, avatar);
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

            client.CurrentGame.EventManager.AddEvent(client, Games.Events.EventEnum.ToTeleport, 0, teleportPoint);
            return $"Teleporting to {teleportPoint.ToStringNames()}";
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
            if (ConfigManager.PlayerManager.BypassAuth) return "Disable BypassAuth to use this command";

            if (Enum.TryParse(typeof(AvatarPrototypeId), @params[0], true, out object avatar))
            {
                client.Session.Account.Player.Avatar = (AvatarPrototypeId)avatar;
                return $"Changing avatar to {client.Session.Account.Player.Avatar}. Relog for changes to take effect.";
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
            if (@params.Length == 0) return $"Current AOI volume = {client.Session.Account.Player.AOIVolume}";
            //if (ConfigManager.PlayerManager.BypassAuth) return "Disable BypassAuth to use this command";

            if (int.TryParse( @params[0], out int volume) && volume >= 1600 && volume <= 5000)
            {
                client.Session.Account.Player.AOIVolume = volume;
                client.AOI.SetAOIVolume(volume);
                return $"Changes player AOI volume size to {volume}.";
            }
            else
            {
                return $"Failed to change AOI volume size to {@params[0]}. Available range [1600..5000]";
            }
        }

        [Command("region", "Changes player starting region.\nUsage: player region", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help player region' to get help.";
            if (ConfigManager.PlayerManager.BypassAuth) return "Disable BypassAuth to use this command";

            if (Enum.TryParse(typeof(RegionPrototypeId), @params[0], true, out object region))
            {
                client.Session.Account.Player.Region = (RegionPrototypeId)region;
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
                var prototypeId = (PrototypeId)ulong.Parse(@params[0]);
                string prototypePath = GameDatabase.GetPrototypeName(prototypeId);

                if (prototypeId == 0 || prototypePath.Contains("Entity/Items/Costumes/Prototypes/"))
                {
                    // Get replication id for the client avatar
                    ulong replicationId = (ulong)client.Session.Account.Player.Avatar.ToPropertyCollectionReplicationId();

                    // Update account data if needed
                    if (ConfigManager.PlayerManager.BypassAuth == false) client.Session.Account.CurrentAvatar.Costume = (ulong)prototypeId;

                    // Send NetMessageSetProperty message with a CostumeCurrent property for the purchased costume
                    client.SendMessage(1, new(
                        Property.ToNetMessageSetProperty(replicationId, new(PropertyEnum.CostumeCurrent), prototypeId)
                        ));
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

    [CommandGroup("omega", "Manages the Omega system.", AccountUserLevel.User)]
    public class OmegaCommand : CommandGroup
    {
        [Command("points", "Adds omega points.\nUsage: omega points", AccountUserLevel.User)]
        public string Points(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (ConfigManager.GameOptions.InfinitySystemEnabled) return "Set InfinitySystemEnabled to false in Config.ini to enable the Omega system.";
            client.SendMessage(1, new(Property.ToNetMessageSetProperty(9078332, new(PropertyEnum.OmegaPoints), 7500)));
            return "Setting Omega points to 7500.";
        }
    }

    [CommandGroup("achievement", "Manages achievements.", AccountUserLevel.User)]
    public class AchievementCommand : CommandGroup
    {
        [Command("unlock", "Unlocks an achievement.\nUsage: achievement unlock [id]", AccountUserLevel.User)]
        public string Unlock(string[] @params, FrontendClient client)
        {
            if (client == null)
                return "You can only invoke this command from the game.";

            if (@params.IsNullOrEmpty())
                return "Invalid arguments. Type 'help achievement unlock' to get help.";

            if (uint.TryParse(@params[0], out uint id) == false)
                return "Failed to parse achievement id.";

            AchievementInfo info = AchievementDatabase.Instance.GetAchievementInfoById(id);

            if (info == null)
                return $"Invalid achievement id {id}.";

            if (info.Enabled == false)
                return $"Achievement id {id} is disabled.";

            AchievementState state = client.Session.Account.Player.AchievementState;
            state.SetAchievementProgress(id, new(info.Threshold, Clock.UnixTime));
            client.SendMessage(1, new(state.ToUpdateMessage(true)));
            return string.Empty;
        }

        [Command("info", "Outputs info for the specified achievement.\nUsage: achievement info [id]", AccountUserLevel.User)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (@params.IsNullOrEmpty())
                return "Invalid arguments. Type 'help achievement unlock' to get help.";

            if (uint.TryParse(@params[0], out uint id) == false)
                return "Failed to parse achievement id.";

            AchievementInfo info = AchievementDatabase.Instance.GetAchievementInfoById(id);

            if (info == null)
                return $"Invalid achievement id {id}.";

            // Output as a single string with line breaks if the command was invoked from the console
            if (client == null)
                return info.ToString();

            // Output as a list of chat messages if the command was invoked from the in-game chat.
            ChatHelper.SendMetagameMessage(client, "Achievement Info:");
            ChatHelper.SendMetagameMessages(client, info.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries), false);
            return string.Empty;
        }
    }
}
