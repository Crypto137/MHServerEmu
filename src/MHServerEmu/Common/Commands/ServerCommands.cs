using MHServerEmu.GameServer.Frontend;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("server", "Allows you to control the servers.")]
    public class ServerCommands : CommandGroup
    {
        [Command("info", "Usage: server info")]
        public string Info(string[] @params, FrontendClient client)
        {
            return $"Server Information\nUptime: {DateTime.Now - Program.StartupTime:hh\\:mm\\:ss}\nSessions: {Program.FrontendServer.FrontendService.SessionCount}";
        }

        [Command("shutdown", "Usage: server shutdown")]
        public string Shutdown(string[] @params, FrontendClient client)
        {
            Program.Shutdown();
            return string.Empty;
        }
    }

    [CommandGroup("client", "Allows you to interact with clients.")]
    public class ClientCommands : CommandGroup
    {
        [Command("info", "Usage: client info [sessionId]")]
        public string Info(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help client info' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId))
            {
                if (Program.FrontendServer.FrontendService.TryGetClientSession(sessionId, out ClientSession session))
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

    }

    [CommandGroup("packet", "Provides commands to interact with packet files.")]
    public class PacketCommands : CommandGroup
    {
        [Command("parse", "Parses messages from all packets\nUsage: packet parse")]
        public string Extract(string[] @params, FrontendClient client)
        {
            if (client != null)
                return "You can only invoke this command from the server console.";

            PacketHelper.ParseServerMessagesFromAllPacketFiles();

            return string.Empty;
        }
    }

    [CommandGroup("gpak", "Provides commands to interact with GPAK files.")]
    public class GpakCommands : CommandGroup
    {
        [Command("extract", "Extracts entries and/or data from GPAK files.\nUsage: gpak extract [entries|data|all]")]
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
