using System.Globalization;
using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("client", "Allows you to interact with clients.", AccountUserLevel.Admin)]
    public class ClientCommands : CommandGroup
    {
        private static readonly char[] HexPrefix = new char[] { '0', 'x' };

        [Command("info", "Usage: client info [sessionId]", AccountUserLevel.Admin)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help client info' to get help.";

            if (ulong.TryParse(@params[0].TrimStart(HexPrefix), NumberStyles.HexNumber, null, out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
                return "Failed to connect to the player manager.";

            if (playerManager.TryGetSession(sessionId, out ClientSession session) == false)
                return $"SessionId 0x{sessionId:X} not found.";

            return session.GetClientInfo();
        }

        [Command("kick", "Usage: client kick [playerName]", AccountUserLevel.Moderator)]
        public string Kick(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help client kick' to get help.";

            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as GroupingManagerService;
            if (groupingManager == null)
                return "Failed to connect to the grouping manager.";

            if (groupingManager.TryGetPlayerByName(@params[0], out IFrontendClient target) == false)
                return $"Player {@params[0]} not found.";

            target.Disconnect();
            return $"Kicked {target}.";
        }

        [Command("send", "Usage: client send [sessionId] [messageName] [messageContent]", AccountUserLevel.Admin)]
        public string Send(string[] @params, FrontendClient client)
        {
            if (@params.Length < 3) return "Invalid arguments. Type 'help client send' to get help.";

            if (ulong.TryParse(@params[0].TrimStart(HexPrefix), NumberStyles.HexNumber, null, out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
                return "Failed to connect to the player manager.";

            if (playerManager.TryGetClient(sessionId, out FrontendClient target) == false)
                return $"Client for sessionId 0x{sessionId:X} not found";

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
}
