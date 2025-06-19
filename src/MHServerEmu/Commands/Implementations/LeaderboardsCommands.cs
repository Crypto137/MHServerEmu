using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
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
        public string ReloadSchedule(string[] @params, NetClient client)
        {
            LeaderboardDatabase leaderboardDB = LeaderboardDatabase.Instance;
            if (leaderboardDB.IsInitialized == false)
                return "Leaderboard database is not available.";

            leaderboardDB.ReloadAndReapplySchedule();
            return "Leaderboard schedule reloaded.";
        }

        [Command("instance")]
        [CommandDescription("Shows details for the specified leaderboard instance.")]
        [CommandUsage("leaderboards instance [instanceId]")]
        public string Instance(string[] @params, NetClient client)
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
        [CommandDescription("Shows details for the specified leaderboard.")]
        [CommandUsage("leaderboards leaderboard [prototypeGuid]")]
        public string Leaderboard(string[] @params, NetClient client)
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
        public string Now(string[] @params, NetClient client)
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

        [Command("enabled")]
        [CommandDescription("Shows enabled leaderboards.")]
        [CommandUsage("leaderboards enabled")]
        public string Enabled(string[] @params, NetClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");

            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            LeaderboardDatabase.Instance.GetLeaderboards(leaderboards);

            foreach (var leaderboard in leaderboards)
                if (leaderboard.Scheduler.IsEnabled)
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
        public string All(string[] @params, NetClient client)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current Time: [{Clock.UtcNowTimestamp}] {Clock.UtcNowPrecise}");

            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            LeaderboardDatabase.Instance.GetLeaderboards(leaderboards);

            foreach (var leaderboard in leaderboards)
                sb.AppendLine(
                    $"[{(leaderboard.Scheduler.IsEnabled ? "+" : "-")}]" +
                    $"[{(long)leaderboard.LeaderboardId}] " +
                    $"{leaderboard.Prototype.DataRef.GetNameFormatted()} = " +
                    $"{leaderboard.Scheduler.StartTime}");

            ListPool<Leaderboard>.Instance.Return(leaderboards);
            return sb.ToString();
        }
    }
}
