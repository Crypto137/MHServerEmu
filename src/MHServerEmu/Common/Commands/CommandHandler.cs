using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Data;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    public static class CommandHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void Parse(string input, FrontendClient client = null)
        {
            string[] command = input.Split(' ', 2);

            if (client == null)
            {
                switch (command[0].ToLower())
                {
                    case "shutdown": Program.Shutdown(); break;
                    case "parse": PacketHelper.ParseServerMessagesFromAllPacketFiles(); break;
                    case "exportgpakentries": Database.ExportGpakEntries(); break;
                    case "exportgpakdata": Database.ExportGpakData(); break;
                    default: Logger.Info($"Unknown command {input}"); break;
                }
            }
            else
            {
                switch (command[0].ToLower())
                {
                    case "echo": SendClientResponse(command[1], client); break;

                    case "tower":
                        SendClientResponse("Changing region to Avengers Tower", client);
                        client.CurrentRegion = RegionPrototype.NPEAvengersTowerHUBRegion;
                        client.SendMultipleMessages(1, RegionLoader.GetBeginLoadingMessages(client.CurrentRegion, client.CurrentAvatar));
                        client.IsLoading = true;
                        break;

                    default: SendClientResponse($"Unknown client command \"{input}\"", client); break;
                }
            }
        }

        public static void SendClientResponse(string output, FrontendClient client)
        {
            var chatMessage = ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(output))
                .SetPrestigeLevel(6)
                .Build().ToByteArray();

            client.SendMessage(2, new(GroupingManagerMessage.ChatNormalMessage, chatMessage));
        }
    }
}
