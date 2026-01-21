using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("ultimateprestige")]
    [CommandGroupDescription("Ultimate Prestige system commands.")]
    public class UltimatePrestigeCommands : CommandGroup
    {
        [Command(nameof(Activate))]
        [CommandDescription("Activates the Ultimate Prestige for the current hero.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Activate(string[] @params, NetClient client)
        {
            Player player = ((PlayerConnection)client).Player;

            Game game = player.Game;
            if (game.CustomGameOptions.EnableUltimatePrestige == false)
                return "Ultimate Prestige is disabled by server settings.";

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null)
                return "Hero not found.";

            if (avatar.CanActivateUltimatePrestigeMode() == false)
                return "Ultimate Prestige requirements are not met. Your current hero must be at level 60 Cosmic prestige, in a hub region, and not be a member of party.";

            if (avatar.ActivateUltimatePrestigeMode() == false)
                return "Failed to activate Ultimate Prestige.";

            string avatarName = avatar.PrototypeDataRef.GetNameFormatted();
            return $"{avatarName} has reached Ultimate Prestige level {avatar.UltimatePrestigeLevel}.";
        }

        [Command(nameof(Level))]
        [CommandDescription("Prints the current Ultimate Prestige level.")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Level(string[] @params, NetClient client)
        {
            Player player = ((PlayerConnection)client).Player;

            Game game = player.Game;
            if (game.CustomGameOptions.EnableUltimatePrestige == false)
                return "Ultimate Prestige is disabled by server settings.";

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null)
                return "Hero not found.";

            string avatarName = avatar.PrototypeDataRef.GetNameFormatted();
            return $"Current Ultimate Prestige level for {avatarName}: {avatar.UltimatePrestigeLevel}.";
        }
    }
}
