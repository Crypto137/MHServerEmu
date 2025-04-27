using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("level", "Provides commands for creating items.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class LevelCommands : CommandGroup
    {
        [Command("up", "Levels up the current avatar.\nUsage: level up")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Up(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            long xp = avatar.Properties[PropertyEnum.ExperiencePoints];
            long xpNeeded = avatar.Properties[PropertyEnum.ExperiencePointsNeeded];
            long xpAmount = xpNeeded - xp;

            avatar.AwardXP(xpAmount, xpAmount, true);            

            return $"Awarded {xpAmount} experience.";
        }

        [Command("max", "Maxes out the current avatar's experience.\nUsage: level max")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Max(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.ExperiencePoints);
            long expToAdd = (long)propertyInfo.Prototype.Max - avatar.Properties[PropertyEnum.ExperiencePoints];

            avatar.AwardXP(expToAdd, expToAdd, true);

            return $"Awarded {expToAdd} experience.";
        }

        [Command("reset", "Resets the current avatar to level 1.\nUsage: level reset")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Reset(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            avatar.InitializeLevel(1);

            return "Reset to level 1.";
        }

        [Command("maxinfinity", "Maxes out Infinity experience.\nUsage: level max")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string MaxInfinity(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            player.Properties[PropertyEnum.InfinityXP] = GameDatabase.AdvancementGlobalsPrototype.InfinityXPCap;
            player.TryInfinityLevelUp(true);

            return $"Infinity experience maxed out.";
        }

        [Command("resetinfinity", "Removes all Infinity progression.\nUsage: level resetinfinity")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ResetInfinity(string[] @params, FrontendClient client)
        {
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            // Force respec for all avatars
            foreach (Avatar avatar in new AvatarIterator(player))
                avatar.RespecInfinity(InfinityGem.None);

            player.Properties.RemovePropertyRange(PropertyEnum.InfinityPoints);
            player.Properties[PropertyEnum.InfinityXP] = 0;

            return $"Infinity reset.";
        }

        [Command("awardxp", "Awards the specified amount of experience.\nUsage: level awardxp [amount]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string AwardXP(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help level awardxp' to get help.";

            if (long.TryParse(@params[0], out long amount) == false)
                return $"Failed to parse argument {@params[0]}.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;
            avatar.AwardXP(amount, amount, true);

            return $"Awarded {amount} experience.";
        }
    }
}
