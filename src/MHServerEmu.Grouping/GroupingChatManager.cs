using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Grouping
{
    public class GroupingChatManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly ChatErrorMessage NoSuchUserErrorMessage = ChatErrorMessage.CreateBuilder()
            .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER)
            .Build();

        private readonly GroupingManagerService _groupingManager;

        private readonly ChatBroadcastMessage _motd;
        private readonly bool _logTells;

        public GroupingChatManager(GroupingManagerService groupingManager)
        {
            _groupingManager = groupingManager;

            GroupingManagerConfig config = ConfigManager.Instance.GetConfig<GroupingManagerConfig>();

            _motd = ChatBroadcastMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                .SetFromPlayerName(config.ServerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(config.MotdText))
                .SetPrestigeLevel(config.ServerPrestigeLevel)
                .Build();

            _logTells = config.LogTells;
        }

        public void OnClientAdded(IFrontendClient client)
        {
            _groupingManager.ClientManager.SendMessage(_motd, client);
        }

        public void OnChat(IFrontendClient client, NetMessageChat chat, int prestigeLevel, List<ulong> playerFilter)
        {
            DBAccount account = ((IDBAccountOwner)client).Account;

            if (string.IsNullOrEmpty(chat.TheMessage.Body) == false)
                Logger.Info($"[{GetRoomName(chat.RoomType)}] [{account})]: {chat.TheMessage.Body}", LogCategory.Chat);

            ChatNormalMessage message = ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(account.PlayerName)
                .SetTheMessage(chat.TheMessage)
                .SetPrestigeLevel(prestigeLevel)
                .Build();

            if (playerFilter != null)
                _groupingManager.ClientManager.SendMessageFiltered(message, playerFilter);
            else
                _groupingManager.ClientManager.SendMessageToAll(message);
        }

        public void OnTell(IFrontendClient client, NetMessageTell tell)
        {
            DBAccount account = ((IDBAccountOwner)client).Account;

            Logger.Info($"[Tell] [{account.PlayerName} => {tell.TargetPlayerName}]: {(_logTells ? tell.TheMessage.Body : "***")}", LogCategory.Chat);

            // Respond with an error for now
            _groupingManager.ClientManager.SendMessage(NoSuchUserErrorMessage, client);
        }

        /// <summary>
        /// Returns the <see cref="string"/> name of the specified chat room type.
        /// </summary>
        public static string GetRoomName(ChatRoomTypes type)
        {
            return type switch
            {
                ChatRoomTypes.CHAT_ROOM_TYPE_LOCAL                  => "Local",
                ChatRoomTypes.CHAT_ROOM_TYPE_SAY                    => "Say",
                ChatRoomTypes.CHAT_ROOM_TYPE_PARTY                  => "Party",
                ChatRoomTypes.CHAT_ROOM_TYPE_TELL                   => "Tell",
                ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS  => "Broadcast",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_ZH              => "Social-CH",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_EN              => "Social-EN",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_FR              => "Social-FR",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_DE              => "Social-DE",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_EL              => "Social-EL",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_JP              => "Social-JP",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_KO              => "Social-KO",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_PT              => "Social-PT",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_RU              => "Social-RU",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_ES              => "Social-ES",
                ChatRoomTypes.CHAT_ROOM_TYPE_TRADE                  => "Trade",
                ChatRoomTypes.CHAT_ROOM_TYPE_LFG                    => "LFG",
                ChatRoomTypes.CHAT_ROOM_TYPE_GUILD                  => "Supergroup",
                ChatRoomTypes.CHAT_ROOM_TYPE_FACTION                => "Team",
                ChatRoomTypes.CHAT_ROOM_TYPE_EMOTE                  => "Emote",
                ChatRoomTypes.CHAT_ROOM_TYPE_ENDGAME                => "Endgame",
                ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME               => "Match",
                ChatRoomTypes.CHAT_ROOM_TYPE_GUILD_OFFICER          => "Officer",
                _                                                   => type.ToString(),
            };
        }
    }
}
