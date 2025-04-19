using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Grouping
{
    /// <summary>
    /// Provides helper functions for interacting with the in-game chat.
    /// </summary>
    public static class ChatHelper
    {
        private const ushort MuxChannel = 2;

        /// <summary>
        /// Initializes <see cref="ChatHelper"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the <see cref="ChatBroadcastMessage"/> instance for the current message of the day.
        /// </summary>
        public static ChatBroadcastMessage Motd { get; }

        /// <summary>
        /// Sends the specified text as a metagame chat message to the provided <see cref="FrontendClient"/>.
        /// </summary>
        /// <remarks>
        /// The in-game chat window does not handle well messages longer than 25-30 lines (~40 characters per line).
        /// If you need to send a long message, use SendMetagameMessages() or SendMetagameMessageSplit().
        /// </remarks>
        public static void SendMetagameMessage(IFrontendClient client, string text, bool showSender = true)
        {
            client.SendMessage(MuxChannel, ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(showSender ? Motd.FromPlayerName : string.Empty)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                .SetPrestigeLevel(Motd.PrestigeLevel)
                .Build());
        }

        /// <summary>
        /// Sends the specified collection of texts as metagame chat messages to the provided <see cref="FrontendClient"/>.
        /// </summary>
        public static void SendMetagameMessages(IFrontendClient client, IEnumerable<string> texts, bool showSender = true)
        {
            foreach (string text in texts)
            {
                SendMetagameMessage(client, text, showSender);
                showSender = false; // Remove sender from messages after the first one
            }
        }

        /// <summary>
        /// Splits the specified text at line breaks and sends it as a collection of metagame chat messages to the provided <see cref="FrontendClient"/>.
        /// </summary>
        public static void SendMetagameMessageSplit(IFrontendClient client, string text, bool showSender = true)
        {
            SendMetagameMessages(client, text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), showSender);
        }

        /// <summary>
        /// Returns the <see cref="string"/> name of the specified chat room type.
        /// </summary>
        public static string GetRoomName(ChatRoomTypes type)
        {
            // All room enums start with "CHAT_ROOM_TYPE_", which is 15 characters
            return Enum.GetName(type).Substring(15);
        }
    }
}
