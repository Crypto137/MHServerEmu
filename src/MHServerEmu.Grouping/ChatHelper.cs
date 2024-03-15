using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Frontend;

namespace MHServerEmu.Grouping
{
    public static class ChatHelper
    {
        private const ushort MuxChannel = 2;

        static ChatHelper()
        {
            var config = ConfigManager.Instance.GetConfig<GroupingManagerConfig>();

            Motd = ChatBroadcastMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                .SetFromPlayerName(config.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(config.MotdText))
                .SetPrestigeLevel(config.MotdPrestigeLevel)
                .Build();
        }

        public static ChatBroadcastMessage Motd { get; }

        public static void SendMetagameMessage(FrontendClient client, string text, bool showSender = true)
        {
            client.SendMessage(MuxChannel, ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(showSender ? Motd.FromPlayerName : string.Empty)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                .SetPrestigeLevel(Motd.PrestigeLevel)
                .Build());
        }

        public static void SendMetagameMessages(FrontendClient client, IEnumerable<string> texts, bool showSender = true)
        {
            foreach (string text in texts)
            {
                SendMetagameMessage(client, text, showSender);
                showSender = false; // Remove sender from messages after the first one
            }
        }

        public static string GetRoomName(ChatRoomTypes type)
        {
            // All room enums start with "CHAT_ROOM_TYPE_", which is 15 characters
            return Enum.GetName(type).Substring(15);
        }
    }
}
