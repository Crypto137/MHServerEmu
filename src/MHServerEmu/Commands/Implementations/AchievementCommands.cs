using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("achievement", "Manages achievements.", AccountUserLevel.Admin)]
    public class AchievementCommands : CommandGroup
    {
        [Command("info", "Outputs info for the specified achievement.\nUsage: achievement info [id]")]
        public string Info(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help achievement unlock' to get help.";

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
            ChatHelper.SendMetagameMessageSplit(client, info.ToString(), false);
            return string.Empty;
        }
    }
}
