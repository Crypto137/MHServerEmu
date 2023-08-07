using MHServerEmu.GameServer.Data;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    public static class CommandHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void Parse(string input)
        {
            switch (input.ToLower())
            {
                case "shutdown": Program.Shutdown(); break;
                case "parse": PacketHelper.ParseServerMessagesFromAllPacketFiles(); break;
                case "exportgpakentries": Database.ExportGpakEntries(); break;
                case "exportgpakdata": Database.ExportGpakData(); break;
                default: Logger.Info($"Unknown command {input}"); break;
            }
        }
    }
}
