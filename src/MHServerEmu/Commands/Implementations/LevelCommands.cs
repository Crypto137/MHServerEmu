using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("level")]
    [CommandGroupDescription("Level management commands.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class LevelCommands : CommandGroup
    {
        [Command("up")]
        [CommandDescription("Levels up the current avatar.")]
        [CommandUsage("level up")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Up(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            long xp = avatar.Properties[PropertyEnum.ExperiencePoints];
            long xpNeeded = avatar.Properties[PropertyEnum.ExperiencePointsNeeded];
            long xpAmount = xpNeeded - xp;

            avatar.AwardXP(xpAmount, xpAmount, true);            

            return $"Awarded {xpAmount} experience.";
        }

        [Command("max")]
        [CommandDescription("Maxes out the current avatar's experience.")]
        [CommandUsage("level max")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Max(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.ExperiencePoints);
            long expToAdd = (long)propertyInfo.Prototype.Max - avatar.Properties[PropertyEnum.ExperiencePoints];

            avatar.AwardXP(expToAdd, expToAdd, true);

            return $"Awarded {expToAdd} experience.";
        }

        [Command("reset")]
        [CommandDescription("Resets the current avatar to level 1.")]
        [CommandUsage("level reset")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Reset(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            avatar.InitializeLevel(1);

            return "Reset to level 1.";
        }

        [Command("maxomega")]
        [CommandDescription("Maxes out Omega experience.")]
        [CommandUsage("level maxomega")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string MaxOmega(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            player.Properties[PropertyEnum.OmegaXP] = GameDatabase.AdvancementGlobalsPrototype.InfinityXPCap;
            player.TryOmegaLevelUp(true);

            return $"Omega experience maxed out.";
        }

        [Command("resetomega")]
        [CommandDescription("Removes all Omega progression.")]
        [CommandUsage("level resetomega")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string ResetOmega(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Player player = playerConnection.Player;

            // Force respec for all avatars
            foreach (Avatar avatar in new AvatarIterator(player))
                avatar.RespecOmegaBonus();

            player.Properties.RemovePropertyRange(PropertyEnum.OmegaPoints);
            player.Properties[PropertyEnum.OmegaXP] = 0;

            return $"Omega reset.";
        }

        [Command("awardxp")]
        [CommandDescription("Awards the specified amount of experience.")]
        [CommandUsage("level awardxp [amount]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string AwardXP(string[] @params, NetClient client)
        {
            if (long.TryParse(@params[0], out long amount) == false)
                return $"Failed to parse argument {@params[0]}.";

            PlayerConnection playerConnection = (PlayerConnection)client;
            Avatar avatar = playerConnection.Player.CurrentAvatar;
            avatar.AwardXP(amount, amount, true);

            return $"Awarded {amount} experience.";
        }
    }
}
