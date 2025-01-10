using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Leaderboards;
using System.Text;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("leaderboards", "Manages leaderboards.", AccountUserLevel.Admin)]
    public class LeaderboardsCommands : CommandGroup
    {
        [Command("jsonreload", "Reload json config file for leaderboards.\nUsage: leaderboards jsonreload")]
        public string JsonReload(string[] @params, FrontendClient client)
        {
            LeaderboardDatabase.Instance.ReloadJsonConfig();
            return "Leaderboards Reloaded";
        }

        [Command("instance", "Show details info for leaderboard Instance.\nUsage: leaderboards instance [id]")]
        public string Instance(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help leaderboards instance' to get help.";
            if (long.TryParse(@params[0], out long instanceId) == false)
                return $"Failed to parse InstanceId {@params[0]}";

            var instance = LeaderboardDatabase.Instance.FindInstance((ulong)instanceId);
            if (instance == null)
                return $"InstanceId {instanceId} not found";

            return $"{instance}";
        }

        [Command("leaderboard", "Show details info for leaderboard.\nUsage: leaderboards leaderboard [guid]")]
        public string Leaderboard(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help leaderboards leaderboard' to get help.";
            if (long.TryParse(@params[0], out long leaderboardId) == false)
                return $"Failed to parse LeaderboardId {@params[0]}";

            var leaderboard = LeaderboardDatabase.Instance.GetLeaderboard((PrototypeGuid)leaderboardId);
            if (leaderboard == null)
                return $"LeaderboardId {leaderboardId} not found";

            return $"{leaderboard}";
        }

        [Command("now", "Show all active instances.\nUsage: leaderboards now")]
        public string Now(string[] @params, FrontendClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");
            var leaderboards = LeaderboardDatabase.Instance.GetLeaderboards();
            foreach (var leaderboard in leaderboards)
                if (leaderboard.IsActive)
                    sb.AppendLine(
                        $"{leaderboard.Prototype.DataRef.GetNameFormatted()}" +
                        $"[{leaderboard.ActiveInstance.InstanceId}] = " +
                        $"{leaderboard.ActiveInstance.ActivationTime} - " +
                        $"{leaderboard.ActiveInstance.ExpirationTime}");

            return sb.ToString();
        }

        [Command("active", "Show all IsActive leaderboards.\nUsage: leaderboards active")]
        public string Active(string[] @params, FrontendClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");
            var leaderboards = LeaderboardDatabase.Instance.GetLeaderboards();
            foreach (var leaderboard in leaderboards)
                if (leaderboard.Scheduler.IsActive)
                    sb.AppendLine(
                        $"{leaderboard.Prototype.DataRef.GetNameFormatted()}" +
                        $"[{leaderboard.ActiveInstance.InstanceId}][{leaderboard.ActiveInstance.State}] = " +
                        $"{leaderboard.ActiveInstance.ActivationTime} - " +
                        $"{leaderboard.ActiveInstance.ExpirationTime}");

            return sb.ToString();
        }

        [Command("all", "Show all leaderboards.\nUsage: leaderboards all")]
        public string All(string[] @params, FrontendClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");
            var leaderboards = LeaderboardDatabase.Instance.GetLeaderboards();
            foreach (var leaderboard in leaderboards)
                sb.AppendLine(
                    $"[{(leaderboard.Scheduler.IsActive ? "+" : "-")}]" +
                    $"[{(long)leaderboard.LeaderboardId}] " +
                    $"{leaderboard.Prototype.DataRef.GetNameFormatted()} = " +
                    $"{leaderboard.Scheduler.StartEvent} - " +
                    $"{leaderboard.Scheduler.EndEvent}");

            return sb.ToString();
        }
    }
}
