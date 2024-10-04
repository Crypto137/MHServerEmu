using System.Text;
using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("server", "Allows you to interact with the server.", AccountUserLevel.User)]
    public class ServerCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("status", "Usage: server status", AccountUserLevel.User)]
        public string Status(string[] @params, FrontendClient client)
        {
            StringBuilder sb = new();
            sb.AppendLine("Server Status");
            sb.AppendLine(ServerApp.VersionInfo);
            sb.Append(ServerManager.Instance.GetServerStatus(client == null));
            string status = sb.ToString();

            // Display in the console as is
            if (client == null)
                return status;

            // Split for the client chat window
            ChatHelper.SendMetagameMessageSplit(client, status, false);
            return string.Empty;
        }

        [Command("broadcast", "Broadcasts a notification to all players.\nUsage: server broadcast", AccountUserLevel.Admin)]
        public string Broadcast(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help server broadcast' to get help.";

            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as IMessageBroadcaster;
            if (groupingManager == null) return "Failed to connect to the grouping manager.";

            string message = string.Join(' ', @params);

            groupingManager.BroadcastMessage(ChatServerNotification.CreateBuilder().SetTheMessage(message).Build());
            Logger.Trace($"Broadcasting server notification: \"{message}\"");

            return string.Empty;
        }

        [Command("reloadlivetuning", "Reloads live tuning settings.\nUsage: server reloadlivetuning", AccountUserLevel.Admin)]
        public string ReloadLiveTuning(string[] @params, FrontendClient client)
        {
            if (client != null) return "You can only invoke this command from the server console.";
            LiveTuningManager.Instance.LoadLiveTuningDataFromDisk();
            return string.Empty;
        }

        [Command("shutdown", "Usage: server shutdown", AccountUserLevel.Admin)]
        public string Shutdown(string[] @params, FrontendClient client)
        {
            string shutdownRequester = client == null ? "the server console" : client.ToString();
            Logger.Info($"Server shutdown request received from {shutdownRequester}");

            // We need to run shutdown as a separate task in case this command is invoked from the game.
            // Otherwise, the game thread is going to break, and we are not going to be able to clean up.
            Task.Run(() => ServerApp.Instance.Shutdown());
            return string.Empty;
        }
    }
}
