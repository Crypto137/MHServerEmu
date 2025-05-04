using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Achievements;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("achievement")]
    [CommandGroupDescription("Commands related to the achievement system.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class AchievementCommands : CommandGroup
    {
        [Command("info")]
        [CommandDescription("Outputs info for the specified achievement.")]
        [CommandUsage("achievement info [id]")]
        [CommandParamCount(1)]
        public string Info(string[] @params, NetClient client)
        {
            if (uint.TryParse(@params[0], out uint id) == false)
                return "Failed to parse achievement id.";

            AchievementInfo info = AchievementDatabase.Instance.GetAchievementInfoById(id);

            if (info == null)
                return $"Invalid achievement id {id}.";

            // Output as a single string with line breaks if the command was invoked from the console
            if (client == null)
                return info.ToString();

            // Output as a list of chat messages if the command was invoked from the in-game chat.
            CommandHelper.SendMessage(client, "Achievement Info:");
            CommandHelper.SendMessageSplit(client, info.ToString(), false);
            return string.Empty;
        }
    }
}
