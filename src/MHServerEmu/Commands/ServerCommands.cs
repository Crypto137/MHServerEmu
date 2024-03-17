using Gazillion;
using System.Text;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Network;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands
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

    [CommandGroup("client", "Allows you to interact with clients.", AccountUserLevel.Admin)]
    public class ClientCommands : CommandGroup
    {
        [Command("info", "Usage: client info [sessionId]", AccountUserLevel.Admin)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help client info' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
                return "Failed to connect to the player manager.";

            if (playerManager.TryGetSession(sessionId, out ClientSession session) == false)
                return $"SessionId {sessionId} not found.";

            return session.ToString();
        }

        [Command("kick", "Usage: client kick [playerName]", AccountUserLevel.Moderator)]
        public string Kick(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help client kick' to get help.";

            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as GroupingManagerService;
            if (groupingManager == null)
                return "Failed to connect to the grouping manager.";

            if (groupingManager.TryGetPlayerByName(@params[0], out FrontendClient target) == false)
                return $"Player {@params[0]} not found.";

            target.Disconnect();
            return $"Kicked {target.Session.Account}.";
        }

        [Command("send", "Usage: client send [sessionId] [messageName] [messageContent]", AccountUserLevel.Admin)]
        public string Send(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length < 3) return "Invalid arguments. Type 'help client send' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
                return "Failed to connect to the player manager.";

            if (playerManager.TryGetClient(sessionId, out FrontendClient target) == false)
                return $"Client for sessionId {sessionId} not found";

            switch (@params[1].ToLower())
            {
                case "chatnormalmessage":
                    string message = @params[2];
                    for (int i = 3; i < @params.Length; i++)
                        message += " " + @params[i];

                    var config = ConfigManager.Instance.GetConfig<GroupingManagerConfig>();

                    var chatMessage = ChatNormalMessage.CreateBuilder()
                        .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                        .SetFromPlayerName(config.MotdPlayerName)
                        .SetTheMessage(ChatMessage.CreateBuilder().SetBody(message))
                        .SetPrestigeLevel(6)
                        .Build();

                    target.SendMessage(2, chatMessage);
                    break;

                default:
                    return $"Unsupported message {@params[1]}";
            }

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
