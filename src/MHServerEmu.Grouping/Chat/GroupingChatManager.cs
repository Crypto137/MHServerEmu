using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Grouping.Chat
{
    public class GroupingChatManager
    {
        private const string PrivateChatPlaceholder = "***";

        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly ChatErrorMessage NoSuchUserErrorMessage = ChatErrorMessage.CreateBuilder()
            .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER)
            .Build();

        private readonly ChatRoomManager[] _chatRoomManager = new ChatRoomManager[(int)ChatRoomTypes.CHAT_ROOM_TYPE_NUM_TYPES];

        private readonly GroupingManagerService _groupingManager;

        private readonly string _serverName;
        private readonly int _serverPrestigeLevel;

        private readonly ChatBroadcastMessage _motd;
        private readonly bool _logPrivateChatRooms;

        public GroupingChatManager(GroupingManagerService groupingManager)
        {
            _groupingManager = groupingManager;

            GroupingManagerConfig config = ConfigManager.Instance.GetConfig<GroupingManagerConfig>();
            _serverName = config.ServerName;
            _serverPrestigeLevel = config.ServerPrestigeLevel;

            _motd = ChatBroadcastMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                .SetFromPlayerName(_serverName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(config.MotdText))
                .SetPrestigeLevel(_serverPrestigeLevel)
                .Build();

            _logPrivateChatRooms = config.LogPrivateChatRooms;

            // Initialize chat room types
            for (ChatRoomTypes chatRoomType = 0; chatRoomType < ChatRoomTypes.CHAT_ROOM_TYPE_NUM_TYPES; chatRoomType++)
                _chatRoomManager[(int)chatRoomType] = new(chatRoomType);
        }

        public bool AddPlayerToRoom(ChatRoomTypes roomType, ulong roomId, ulong playerDbId)
        {
            ChatRoomManager chatRoomManager = GetChatRoomManager(roomType);
            if (chatRoomManager == null)
                return false;

            return chatRoomManager.AddPlayer(roomId, playerDbId);
        }

        public bool RemovePlayerFromRoom(ChatRoomTypes roomType, ulong roomId, ulong playerDbId)
        {
            ChatRoomManager chatRoomManager = GetChatRoomManager(roomType);
            if (chatRoomManager == null)
                return false;

            return chatRoomManager.RemovePlayer(roomId, playerDbId);
        }

        public void OnClientAdded(IFrontendClient client)
        {
            SendMessage(_motd, client);
        }

        public void OnChat(IFrontendClient client, NetMessageChat chat, int prestigeLevel, List<ulong> playerFilter)
        {
            DBAccount account = ((IDBAccountOwner)client).Account;

            ChatRoomTypes roomType = chat.RoomType;
            ChatMessage theMessage = chat.TheMessage;
            ulong roomId = 0;

            ChatNormalMessage message = ChatNormalMessage.CreateBuilder()
                .SetRoomType(roomType)
                .SetFromPlayerName(account.PlayerName)
                .SetTheMessage(theMessage)
                .SetPrestigeLevel(prestigeLevel)
                .Build();

            if (roomType.IsGlobalChatRoom())
            {
                // Global chat rooms
                if (playerFilter != null)
                    SendMessageFiltered(message, playerFilter);
                else
                    SendMessageToAll(message);
            }
            else
            {
                // Chat rooms with multiple instances
                if (SendMessageToChatRoom(message, roomType, (ulong)account.Id, out roomId) == false)
                    Logger.Warn($"OnChat(): Player [{account}] failed to send message to chat room {roomType}");
            }

            string messageBody = chat.TheMessage.Body;
            if (string.IsNullOrEmpty(messageBody) == false)
            {
                // Hide the contents of messages in private rooms (party / guild) if needed.
                if (_logPrivateChatRooms == false && roomType.IsPrivateChatRoom())
                    messageBody = PrivateChatPlaceholder;

                Logger.Info($"[{roomType.GetRoomName()} (0x{roomId:X})] [{account})]: {messageBody}", LogCategory.Chat);
            }
        }

        public void OnTell(IFrontendClient senderClient, NetMessageTell tell, int prestigeLevel)
        {
            string fromPlayerName = ((IDBAccountOwner)senderClient).Account.PlayerName;

            if (_groupingManager.ClientManager.TryGetClient(tell.TargetPlayerName, out IFrontendClient targetClient) == false) 
            {
                Logger.Trace($"OnTell(): Player [{senderClient}] tried to send a tell to a non-existent or offline player {tell.TargetPlayerName}");
                SendMessage(NoSuchUserErrorMessage, senderClient);
                return;
            }

            ChatTellMessage message = ChatTellMessage.CreateBuilder()
                .SetFromPlayerName(fromPlayerName)
                .SetTheMessage(tell.TheMessage)
                .SetPrestigeLevel(prestigeLevel)
                .Build();

            SendMessage(message, targetClient);

            string messageBody = _logPrivateChatRooms ? tell.TheMessage.Body : PrivateChatPlaceholder;
            Logger.Info($"[Tell] [{fromPlayerName} => {tell.TargetPlayerName}]: {messageBody}", LogCategory.Chat);
        }

        public void OnMetagameMessage(IFrontendClient client, string text, bool showSender)
        {
            ChatNormalMessage message = ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(showSender ? _serverName : string.Empty)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                .SetPrestigeLevel(_serverPrestigeLevel)
                .Build();

            SendMessage(message, client);
        }

        public void OnServerNotification(string notificationText)
        {
            Logger.Info($"Broadcasting server notification: \"{notificationText}\"", LogCategory.Chat);

            ChatServerNotification message = ChatServerNotification.CreateBuilder()
                .SetTheMessage(notificationText)
                .Build();

            SendMessageToAll(message);
        }

        private ChatRoomManager GetChatRoomManager(ChatRoomTypes roomType)
        {
            if (roomType < 0 || roomType >= ChatRoomTypes.CHAT_ROOM_TYPE_NUM_TYPES)
                return Logger.WarnReturn<ChatRoomManager>(null, $"Invalid room type {roomType}");

            return _chatRoomManager[(int)roomType];
        }

        private void SendMessage(IMessage message, IFrontendClient client)
        {
            _groupingManager.ClientManager.SendMessage(message, client);
        }

        private void SendMessageFiltered(IMessage message, List<ulong> playerFilter)
        {
            _groupingManager.ClientManager.SendMessageFiltered(message, playerFilter);
        }

        private bool SendMessageToChatRoom(IMessage message, ChatRoomTypes roomType, ulong playerDbId, out ulong roomId)
        {
            roomId = 0;

            ChatRoomManager chatRoomManager = GetChatRoomManager(roomType);
            if (chatRoomManager == null) return Logger.WarnReturn(false, "SendMessageToChatRoom(): chatRoomManager == null");

            ChatRoom chatRoom = chatRoomManager.GetRoomForPlayer(playerDbId);
            if (chatRoom == null)
                return Logger.WarnReturn(false, $"SendMessageToChatRoom(): Player 0x{playerDbId:X} is not in a chat room of type {roomType}");

            roomId = chatRoom.Id;

            List<ulong> playerFilter = ListPool<ulong>.Instance.Get();
            chatRoom.GetPlayers(playerFilter);

            SendMessageFiltered(message, playerFilter);

            ListPool<ulong>.Instance.Return(playerFilter);
            return true;
        }

        private void SendMessageToAll(IMessage message)
        {
            _groupingManager.ClientManager.SendMessageToAll(message);
        }
    }
}
