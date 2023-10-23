using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Frontend;
using MHServerEmu.Networking;

namespace MHServerEmu.Grouping
{
    public static class ChatHelper
    {
        private const ushort MuxChannel = 2;

        public static ChatBroadcastMessage Motd { get; } = ChatBroadcastMessage.CreateBuilder()
            .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
            .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
            .SetTheMessage(ChatMessage.CreateBuilder().SetBody(ConfigManager.GroupingManager.MotdText))
            .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
            .Build();

        public static void SendMetagameMessage(FrontendClient client, string text)
        {
            client.SendMessage(MuxChannel, new(ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                .Build()));
        }

        public static void SendMetagameMessages(FrontendClient client, IEnumerable<string> texts)
        {
            List<GameMessage> messageList = new();
            bool headerIsSet = false;               // Flag to add player name to the header (first) message

            foreach (string text in texts)
            {
                messageList.Add(new(ChatNormalMessage.CreateBuilder()
                    .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                    .SetFromPlayerName(headerIsSet ? string.Empty : ConfigManager.GroupingManager.MotdPlayerName)
                    .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                    .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                    .Build()));

                headerIsSet = true;
            }

            client.SendMessages(MuxChannel, messageList);
        }

        public static string GetRoomName(ChatRoomTypes type)
        {
            // All room enums start with "CHAT_ROOM_TYPE_", which is 15 characters
            return Enum.GetName(type).Substring(15);
        }
    }
}
