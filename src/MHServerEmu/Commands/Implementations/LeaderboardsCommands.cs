using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Leaderboards;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("leaderboards")]
    [CommandGroupDescription("Commands related to the leaderboard system")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class LeaderboardsCommands : CommandGroup
    {
        [Command("reloadschedule")]
        [CommandDescription("Reloads leaderboard schedule from JSON.")]
        [CommandUsage("leaderboards reloadschedule")]
        public string ReloadSchedule(string[] @params, FrontendClient client)
        {
            LeaderboardDatabase.Instance.ReloadSchedule();
            return "Leaderboard schedule reloaded.";
        }

        [Command("instance")]
        [CommandDescription("Shows details info for leaderboard Instance.")]
        [CommandUsage("leaderboards instance [instanceId]")]
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

        [Command("leaderboard")]
        [CommandDescription("Shows details info for leaderboard.")]
        [CommandUsage("leaderboards leaderboard [prototypeGuid]")]
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

        [Command("now")]
        [CommandDescription("Shows all active instances.")]
        [CommandUsage("leaderboards now")]
        public string Now(string[] @params, FrontendClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");

            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            LeaderboardDatabase.Instance.GetLeaderboards(leaderboards);

            foreach (var leaderboard in leaderboards)
                if (leaderboard.IsActive)
                    sb.AppendLine(
                        $"{leaderboard.Prototype.DataRef.GetNameFormatted()}" +
                        $"[{leaderboard.ActiveInstance.InstanceId}] = " +
                        $"{leaderboard.ActiveInstance.ActivationTime} - " +
                        $"{leaderboard.ActiveInstance.ExpirationTime}");

            ListPool<Leaderboard>.Instance.Return(leaderboards);
            return sb.ToString();
        }

        [Command("active")]
        [CommandDescription("Shows all IsActive leaderboards.")]
        [CommandUsage("leaderboards active")]
        public string Active(string[] @params, FrontendClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");

            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            LeaderboardDatabase.Instance.GetLeaderboards(leaderboards);

            foreach (var leaderboard in leaderboards)
                if (leaderboard.Scheduler.IsActive)
                    sb.AppendLine(
                        $"{leaderboard.Prototype.DataRef.GetNameFormatted()}" +
                        $"[{leaderboard.ActiveInstance.InstanceId}][{leaderboard.ActiveInstance.State}] = " +
                        $"{leaderboard.ActiveInstance.ActivationTime} - " +
                        $"{leaderboard.ActiveInstance.ExpirationTime}");

            ListPool<Leaderboard>.Instance.Return(leaderboards);
            return sb.ToString();
        }

        [Command("all")]
        [CommandDescription("Shows all leaderboards.")]
        [CommandUsage("leaderboards all")]
        public string All(string[] @params, FrontendClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");

            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            LeaderboardDatabase.Instance.GetLeaderboards(leaderboards);

            foreach (var leaderboard in leaderboards)
                sb.AppendLine(
                    $"[{(leaderboard.Scheduler.IsActive ? "+" : "-")}]" +
                    $"[{(long)leaderboard.LeaderboardId}] " +
                    $"{leaderboard.Prototype.DataRef.GetNameFormatted()} = " +
                    $"{leaderboard.Scheduler.StartEvent} - " +
                    $"{leaderboard.Scheduler.EndEvent}");

            ListPool<Leaderboard>.Instance.Return(leaderboards);
            return sb.ToString();
        }
    }
}
