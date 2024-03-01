using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Frontend;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("server", "Allows you to interact with the server.", AccountUserLevel.User)]
    public class ServerCommands : CommandGroup
    {
        [Command("status", "Usage: server status", AccountUserLevel.User)]
        public string Info(string[] @params, FrontendClient client)
        {
            return ServerManager.Instance.GetServerStatus();
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

            if (ServerManager.Instance.PlayerManagerService.TryGetSession(sessionId, out ClientSession session) == false)
                return $"SessionId {sessionId} not found";

            return session.ToString();
        }

        [Command("kick", "Usage: client kick [playerName]", AccountUserLevel.Moderator)]
        public string Kick(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help client kick' to get help.";

            if (ServerManager.Instance.GroupingManagerService.TryGetPlayerByName(@params[0], out FrontendClient target) == false)
                return $"Player {@params[0]} not found.";

            target.Connection.Disconnect();
            return $"Kicked {target.Session.Account}.";
        }

        [Command("send", "Usage: client send [sessionId] [messageName] [messageContent]", AccountUserLevel.Admin)]
        public string Send(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length < 3) return "Invalid arguments. Type 'help client send' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            if (ServerManager.Instance.PlayerManagerService.TryGetClient(sessionId, out FrontendClient target) == false)
                return $"Client for sessionId {sessionId} not found";

            switch (@params[1].ToLower())
            {
                case "chatnormalmessage":
                    string message = @params[2];
                    for (int i = 3; i < @params.Length; i++)
                        message += " " + @params[i];

                    var chatMessage = ChatNormalMessage.CreateBuilder()
                        .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                        .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                        .SetTheMessage(ChatMessage.CreateBuilder().SetBody(message))
                        .SetPrestigeLevel(6)
                        .Build();

                    target.SendMessage(2, new(chatMessage));
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
