using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Games.Network;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("achievement", "Manages achievements.", AccountUserLevel.Admin)]
    public class AchievementCommands : CommandGroup
    {
        [Command("unlock", "Unlocks an achievement.\nUsage: achievement unlock [id]")]
        public string Unlock(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help achievement unlock' to get help.";

            if (uint.TryParse(@params[0], out uint id) == false)
                return "Failed to parse achievement id.";

            AchievementInfo info = AchievementDatabase.Instance.GetAchievementInfoById(id);

            if (info == null)
                return $"Invalid achievement id {id}.";

            if (info.Enabled == false)
                return $"Achievement id {id} is disabled.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            AchievementState state = playerConnection.Player.AchievementState;
            state.SetAchievementProgress(id, new(info.Threshold, Clock.UnixTime));
            client.SendMessage(1, state.ToUpdateMessage(true));
            return string.Empty;
        }

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
