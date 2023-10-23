using Gazillion;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameService, IMessageHandler
    {
        private const ushort MuxChannel = 2;    // All messages come from GroupingManager over mux channel 2

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ServerManager _serverManager;
        private readonly object _playerLock = new();
        private readonly Dictionary<string, FrontendClient> _playerDict = new();    // Store players in a name-client dictionary because tell messages are sent by player name

        public GroupingManagerService(ServerManager serverManager)
        {
            _serverManager = serverManager;
        }

        #region Player Management

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
                client.SendMessage(MuxChannel, new(ChatHelper.Motd));
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

        #endregion

        #region Message Handling

        // Direct message handling from mux channel 2. We haven't really seen this, but there's a
        // ClientToGroupingManager protocol that includes a single message - GetPlayerInfoByName
        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            throw new NotImplementedException();
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            throw new NotImplementedException();
        }

        // Routed message handling
        public void Handle(FrontendClient client, GameMessage message)
        {
            // Handle messages routed from the PlayerManager
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageChat: OnChat(client, message.Deserialize<NetMessageChat>()); break;
                case ClientToGameServerMessage.NetMessageTell: OnTell(client, message.Deserialize<NetMessageTell>()); break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client,IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, message);
        }

        private void OnChat(FrontendClient client, NetMessageChat chat)
        {
            // Try to parse the message as a command first
            if (CommandManager.TryParse(chat.TheMessage.Body, client)) return;

            // Limit broadcast and metagame channels to users with moderator privileges and higher
            if ((chat.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS || chat.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                && client.Session.Account.UserLevel < AccountUserLevel.Moderator)
            {
                // There are two chat error sources: NetMessageChatError from GameServerToClient.proto and ChatErrorMessage from GroupingManager.proto.
                // The client expects the former from mux channel 1, and the latter from mux channel 2. Local region chat might be handled by the game
                // instance instead. CHAT_ERROR_COMMAND_NOT_RECOGNIZED works only with NetMessageChatError, so this might have to be handled by the
                // game instance as well.

                client.SendMessage(1, new(NetMessageChatError.CreateBuilder()
                    .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_COMMAND_NOT_RECOGNIZED)
                    .Build()));

                return;
            }

            // Broadcast the message if everything's okay
            Logger.Trace($"[{ChatHelper.GetRoomName(chat.RoomType)}] [{client.Session.Account})]: {chat.TheMessage.Body}");

            // Right now all messages are broadcasted to all connected players
            BroadcastMessage(new(ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(client.Session.Account.PlayerName)
                .SetTheMessage(chat.TheMessage)
                .Build()));
        }

        private void OnTell(FrontendClient client, NetMessageTell tell)
        {
            Logger.Trace($"Received tell for {tell.TargetPlayerName}");

            // Respond with an error for now
            client.SendMessage(MuxChannel, new(ChatErrorMessage.CreateBuilder()
                .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER)
                .Build()));
        }

        #endregion
    }
}
