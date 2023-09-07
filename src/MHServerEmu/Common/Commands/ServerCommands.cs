using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Frontend;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("server", "Allows you to interact with the server.", AccountUserLevel.User)]
    public class ServerCommands : CommandGroup
    {
        [Command("info", "Usage: server info", AccountUserLevel.User)]
        public string Info(string[] @params, FrontendClient client)
        {
            return $"Server Information\nUptime: {DateTime.Now - Program.StartupTime:hh\\:mm\\:ss}\nSessions: {Program.FrontendServer.FrontendService.SessionCount}";
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

            if (ulong.TryParse(@params[0], out ulong sessionId))
            {
                if (Program.FrontendServer.FrontendService.TryGetSession(sessionId, out ClientSession session))
                {
                    return session.ToString();
                }
                else
                {
                    return $"SessionId {sessionId} not found";
                }
            }
            else
            {
                return $"Failed to parse sessionId {@params[0]}";
            }
        }

        [Command("kick", "Usage: client kick [sessionId]", AccountUserLevel.Moderator)]
        public string Kick(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help client kick' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId))
            {
                if (Program.FrontendServer.FrontendService.TryGetClient(sessionId, out FrontendClient target))
                {
                    string email = target.Session.Account.Email;
                    target.Connection.Disconnect();
                    return $"Kicked {sessionId} ({email})";
                }
                else
                {
                    return $"SessionId {sessionId} not found";
                }
            }
            else
            {
                return $"Failed to parse sessionId {@params[0]}";
            }
        }

        [Command("send", "Usage: client send [sessionId] [messageName] [messageContent]", AccountUserLevel.Admin)]
        public string Send(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length < 3) return "Invalid arguments. Type 'help client send' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId))
            {
                if (Program.FrontendServer.FrontendService.TryGetClient(sessionId, out FrontendClient target))
                {
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
                else
                {
                    return $"Client for sessionId {sessionId} not found";
                }
            }
            else
            {
                return $"Failed to parse sessionId {@params[0]}";
            }
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

    [CommandGroup("gpak", "Provides commands to interact with GPAK files.", AccountUserLevel.Admin)]
    public class GpakCommands : CommandGroup
    {
        [Command("extract", "Extracts entries and/or data from GPAK files.\nUsage: gpak extract [entries|data|all]", AccountUserLevel.Admin)]
        public string Extract(string[] @params, FrontendClient client)
        {
            if (client != null)
                return "You can only invoke this command from the server console.";

            if (@params != null && @params.Length > 0)
            {
                if (@params[0] == "entries")
                {
                    GameDatabase.ExtractGpakEntries();
                    return "Finished extracting GPAK entries.";
                }
                else if (@params[0] == "data")
                {
                    GameDatabase.ExtractGpakData();
                    return "Finished extracting GPAK data.";
                }
                else if (@params[0] == "all")
                {
                    GameDatabase.ExtractGpakEntries();
                    GameDatabase.ExtractGpakData();
                    return "Finished extracting GPAK entries and data.";
                }
            }

            return "Invalid parameters. Type 'help gpak extract' to get help.";
        }
    }
}
