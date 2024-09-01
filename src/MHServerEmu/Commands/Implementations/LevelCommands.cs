using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("level", "Provides commands for creating items.", AccountUserLevel.Admin)]
    public class LevelCommands : CommandGroup
    {
        [Command("up", "Levels up the current avatar.\nUsage: level up")]
        public string Up(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            long xp = avatar.Properties[PropertyEnum.ExperiencePoints];
            long xpNeeded = avatar.Properties[PropertyEnum.ExperiencePointsNeeded];
            long xpAmount = xpNeeded - xp;

            avatar.AwardXP(xpAmount, true);            

            return $"Awarded {xpAmount} experience.";
        }

        [Command("max", "Maxes out the current avatar's level.\nUsage: level max")]
        public string Max(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            avatar.AwardXP(long.MaxValue, true);

            return $"Awarded {long.MaxValue} experience.";
        }

        [Command("awardxp", "Awards the specified amount of experience.\nUsage: level awardxp [amount]")]
        public string AwardXP(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help level awardxp' to get help.";

            if (long.TryParse(@params[0], out long amount) == false)
                return $"Failed to parse argument {@params[0]}.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;
            avatar.AwardXP(amount, true);

            return $"Awarded {amount} experience.";
        }
    }
}
