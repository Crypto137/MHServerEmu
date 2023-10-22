using Gazillion;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameService
    {
        private const ushort MuxChannel = 2;    // All messages come to and from GroupingManager over mux channel 2

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ServerManager _serverManager;
        private readonly object _playerLock = new();
        private readonly Dictionary<string, FrontendClient> _playerDict = new();    // Store players in a name-client dictionary because tell messages are sent by player name

        public GroupingManagerService(ServerManager serverManager)
        {
            _serverManager = serverManager;
        }

        public void AcceptClientHandshake(FrontendClient client)
        {
            client.FinishedGroupingManagerHandshake = true;
        }

        public void AddPlayer(FrontendClient client)
        {
            lock (_playerLock)
            {
                if (_playerDict.ContainsValue(client))
                {
                    Logger.Warn("Failed to add player: already added");
                    return;
                }

                _playerDict.Add(client.Session.Account.PlayerName.ToLower(), client);
                SendMotd(client);
            }
        }

        public void RemovePlayer(FrontendClient client)
        {
            lock (_playerLock)
            {
                if (_playerDict.ContainsValue(client) == false)
                {
                    Logger.Warn("Failed to remove player: not found");
                    return;
                }

                _playerDict.Remove(client.Session.Account.PlayerName.ToLower());
            }
        }

        public void BroadcastMessage(GameMessage message)
        {
            lock (_playerLock)
            {
                foreach (var kvp in _playerDict)
                    kvp.Value.SendMessage(MuxChannel, message);
            }
        }

        public bool TryGetPlayerByName(string playerName, out FrontendClient client) => _playerDict.TryGetValue(playerName.ToLower(), out client);

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            if (muxId != 1) throw new($"GroupingManagerService message handling on mux channel {muxId} is not implemented");    // In case we ever get a message directly from the client on channel 2

            // Handle messages routed from the PlayerManager
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageChat:
                    var chatMessageIn = NetMessageChat.ParseFrom(message.Payload);

                    if (CommandManager.TryParse(chatMessageIn.TheMessage.Body, client) == false)
                    {
                        // Limit broadcast and metagame channels to users with moderator privileges and higher
                        if ((chatMessageIn.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS || chatMessageIn.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                            && client.Session.Account.UserLevel < AccountUserLevel.Moderator)
                        {
                            client.SendMessage(1, new(NetMessageChatError.CreateBuilder().SetErrorMessage(ChatErrorMessages.CHAT_ERROR_COMMAND_NOT_RECOGNIZED).Build()));
                        }
                        else
                        {
                            Logger.Trace($"[{chatMessageIn.RoomType}] [{client.Session.Account})]: {chatMessageIn.TheMessage.Body}");

                            var chatMessageOut = ChatNormalMessage.CreateBuilder()
                                .SetRoomType(chatMessageIn.RoomType)
                                .SetFromPlayerName(client.Session.Account.PlayerName)
                                .SetTheMessage(chatMessageIn.TheMessage)
                                .Build();

                            BroadcastMessage(new(chatMessageOut));
                        }
                    }

                    break;

                case ClientToGameServerMessage.NetMessageTell:
                    var tellMessage = NetMessageTell.ParseFrom(message.Payload);
                    Logger.Trace($"Received tell for {tellMessage.TargetPlayerName}");

                    // Respond with an error for now
                    client.SendMessage(MuxChannel, new(ChatErrorMessage.CreateBuilder().SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER).Build()));
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }

        public static void SendMetagameChatMessage(FrontendClient client, string text)
        {
            client.SendMessage(MuxChannel, new(ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(text))
                .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                .Build()));
        }

        public static void SendMetagameChatMessages(FrontendClient client, IEnumerable<string> texts)
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

        private void SendMotd(FrontendClient client)
        {
            client.SendMessage(MuxChannel, new(ChatBroadcastMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(ConfigManager.GroupingManager.MotdText))
                .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                .Build()));
        }
    }
}
