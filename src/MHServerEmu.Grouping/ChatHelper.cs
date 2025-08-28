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
        // NOTE: This is used primarily by the server command system to output messages to the client.

        private const ushort MuxChannel = 2;

        private static readonly string ServerName;
        private static readonly int ServerPrestigeLevel;

        /// <summary>
        /// Initializes <see cref="ChatHelper"/>.
        /// </summary>
        static ChatHelper()
        {
            var config = ConfigManager.Instance.GetConfig<GroupingManagerConfig>();
            ServerName = config.ServerName;
            ServerPrestigeLevel = config.ServerPrestigeLevel;
        }

        /// <summary>
        /// Sends the specified text as a metagame chat message to the provided <see cref="IFrontendClient"/>.
        /// </summary>
        /// <remarks>
        /// The in-game chat window does not handle well messages longer than 25-30 lines (~40 characters per line).
        /// If you need to send a long message, use SendMetagameMessages() or SendMetagameMessageSplit().
        /// </remarks>
        public static void SendMetagameMessage(IFrontendClient client, string text, bool showSender = true)
        {
            client.SendMessage(MuxChannel, ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(showSender ? ServerName : string.Empty)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                .SetPrestigeLevel(ServerPrestigeLevel)
                .Build());
        }

        /// <summary>
        /// Sends the specified collection of texts as metagame chat messages to the provided <see cref="IFrontendClient"/>.
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
        /// Splits the specified text at line breaks and sends it as a collection of metagame chat messages to the provided <see cref="IFrontendClient"/>.
        /// </summary>
        public static void SendMetagameMessageSplit(IFrontendClient client, string text, bool showSender = true)
        {
            SendMetagameMessages(client, text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), showSender);
        }
    }
}
