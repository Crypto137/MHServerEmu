using Gazillion;
using System.Text;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Network;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("server", "Allows you to interact with the server.", AccountUserLevel.User)]
    public class ServerCommands : CommandGroup
    {
        [Command("status", "Usage: server status", AccountUserLevel.User)]
        public string Status(string[] @params, FrontendClient client)
        {
            StringBuilder sb = new();
            sb.AppendLine("Server Status");
            sb.AppendLine(Program.VersionInfo);
            sb.Append(ServerManager.Instance.GetServerStatus());
            string status = sb.ToString();

            // Display in the console as is
            if (client == null)
                return status;

            // Split for the client chat window
            ChatHelper.SendMetagameMessages(client, status.Split("\r\n", StringSplitOptions.RemoveEmptyEntries), false);
            return string.Empty;
        }

        [Command("shutdown", "Usage: server shutdown", AccountUserLevel.Admin)]
        public string Shutdown(string[] @params, FrontendClient client)
        {
            Program.Shutdown();
            return string.Empty;
        }
    }

    [CommandGroup("packet", "Provides commands to interact with packet files.", AccountUserLevel.Admin)]
    public class PacketCommands : CommandGroup
    {
        [Command("parse", "Parses messages from all packets\nUsage: packet parse", AccountUserLevel.Admin)]
        public string Extract(string[] @params, FrontendClient client)
        {
            if (client != null)
                return "You can only invoke this command from the server console.";

            PacketHelper.ParseServerMessagesFromAllPacketFiles();

            return string.Empty;
        }
    }
}
