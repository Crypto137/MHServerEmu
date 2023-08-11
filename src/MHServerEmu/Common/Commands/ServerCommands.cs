using MHServerEmu.GameServer.Data;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("server", "Allows you to control the servers.")]
    public class ServerCommands : CommandGroup
    {
        [Command("shutdown", "Usage: server shutdown")]
        public string Shutdown(string[] @params, FrontendClient client)
        {
            Program.Shutdown();
            return string.Empty;
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
        [Command("export", "Exports data from GPAK files.\nUsage: gpak export [entries|data|all]")]
        public string Extract(string[] @params, FrontendClient client)
        {
            if (client != null)
                return "You can only invoke this command from the server console.";

            if (@params != null && @params.Length > 0)
            {
                if (@params[0] == "entries")
                {
                    Database.ExportGpakEntries();
                    return "Finished exporting GPAK entries";
                }
                else if (@params[0] == "data")
                {
                    Database.ExportGpakData();
                    return "Finished exporting GPAK data";
                }
                else if (@params[0] == "all")
                {
                    Database.ExportGpakEntries();
                    Database.ExportGpakData();
                    return "Finished exporting GPAK entries and data";
                }
            }

            return "Invalid parameters. Type 'help gpak export' to get help";
        }
    }
}
