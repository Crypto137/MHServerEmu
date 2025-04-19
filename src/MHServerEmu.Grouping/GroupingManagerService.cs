using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameService, IMessageBroadcaster
    {
        private const ushort MuxChannel = 2;    // All messages come from GroupingManager over mux channel 2

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _playerLock = new();
        private readonly Dictionary<string, IFrontendClient> _playerDict = new();    // Store players in a name-client dictionary because tell messages are sent by player name

        private ICommandParser _commandParser;

        public GroupingManagerService(ICommandParser commandParser = null)
        {
            _commandParser = commandParser;
        }

        #region IGameService Implementation

        public void Run() { }

        public void Shutdown() { }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                // NOTE: We haven't really seen this, but there is a ClientToGroupingManager protocol
                // that includes a single message - GetPlayerInfoByName. If we ever receive it, it should end up here.

                case GameServiceProtocol.AddClient addClient:
                    OnAddClient(addClient);
                    break;

                case GameServiceProtocol.RemoveClient removeClient:
                    OnRemoveClient(removeClient);
                    break;

                case GameServiceProtocol.RouteMessage routeMailboxMessage:
                    OnRouteMailboxMessage(routeMailboxMessage);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Players: {_playerDict.Count}";
        }

        private void OnAddClient(in GameServiceProtocol.AddClient addClient)
        {
            AddClient(addClient.Client);
        }

        private void OnRemoveClient(in GameServiceProtocol.RemoveClient removeClient)
        {
            RemoveClient(removeClient.Client);
        }

        private void OnRouteMailboxMessage(in GameServiceProtocol.RouteMessage routeMailboxMessage)
        {
            IFrontendClient client = routeMailboxMessage.Client;
            MailboxMessage message = routeMailboxMessage.Message;

            // Handle messages routed from games
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageChat: OnChat(client, message); break;
                case ClientToGameServerMessage.NetMessageTell: OnTell(client, message); break;
                case ClientToGameServerMessage.NetMessageTryModifyCommunityMemberCircle: OnTryModifyCommunityMemberCircle(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        #endregion

        #region Client Management

        private bool AddClient(IFrontendClient client)
        {
            lock (_playerLock)
            {
                DBAccount account = ((IDBAccountOwner)client).Account;
                string playerName = account.PlayerName.ToLower();

                if (_playerDict.ContainsKey(playerName))
                    return Logger.WarnReturn(false, "AddFrontendClient(): Already added");

                _playerDict.Add(playerName, client);
                client.SendMessage(MuxChannel, ChatHelper.Motd);

                Logger.Info($"Added client [{client}]");
                return true;
            }
        }

        private bool RemoveClient(IFrontendClient client)
        {
            lock (_playerLock)
            {
                DBAccount account = ((IDBAccountOwner)client).Account;
                string playerName = account.PlayerName.ToLower();

                if (_playerDict.Remove(playerName) == false)
                    return Logger.WarnReturn(false, $"RemoveFrontendClient(): Player {account.PlayerName} not found");

                Logger.Info($"Removed client [{client}]");
                return true;
            }
        }

        public void BroadcastMessage(IMessage message)
        {
            lock (_playerLock)
            {
                foreach (var kvp in _playerDict)
                    kvp.Value.SendMessage(MuxChannel, message);
            }
        }

        public bool TryGetPlayerByName(string playerName, out IFrontendClient client)
        {
            return _playerDict.TryGetValue(playerName.ToLower(), out client);
        }

        #endregion

        #region Message Handling

        private bool OnChat(IFrontendClient client, MailboxMessage message)
        {
            var chat = message.As<NetMessageChat>();
            if (chat == null) return Logger.WarnReturn(false, $"OnChat(): Failed to retrieve message");

            // Try to parse the message as a command first
            if (_commandParser != null && _commandParser.TryParse(chat.TheMessage.Body, client))
                return true;

            DBAccount account = ((IDBAccountOwner)client).Account;

            // Limit broadcast and metagame channels to users with moderator privileges and higher
            if ((chat.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS || chat.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                && account.UserLevel < AccountUserLevel.Moderator)
            {
                // There are two chat error sources: NetMessageChatError from GameServerToClient.proto and ChatErrorMessage from GroupingManager.proto.
                // The client expects the former from mux channel 1, and the latter from mux channel 2. Local region chat might be handled by the game
                // instance instead. CHAT_ERROR_COMMAND_NOT_RECOGNIZED works only with NetMessageChatError, so this might have to be handled by the
                // game instance as well.

                client.SendMessage(1, NetMessageChatError.CreateBuilder()
                    .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_COMMAND_NOT_RECOGNIZED)
                    .Build());

                return true;
            }

            // Broadcast the message if everything's okay
            if (string.IsNullOrEmpty(chat.TheMessage.Body) == false)
                Logger.Trace($"[{ChatHelper.GetRoomName(chat.RoomType)}] [{account})]: {chat.TheMessage.Body}", LogCategory.Chat);

            // Right now all messages are broadcasted to all connected players
            BroadcastMessage(ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(account.PlayerName)
                .SetTheMessage(chat.TheMessage)
                .Build());

            return true;
        }

        private bool OnTell(IFrontendClient client, MailboxMessage message)
        {
            var tell = message.As<NetMessageTell>();
            if (tell == null) return Logger.WarnReturn(false, $"OnTell(): Failed to retrieve message");

            Logger.Trace($"Received tell for {tell.TargetPlayerName}");

            // Respond with an error for now
            client.SendMessage(MuxChannel, ChatErrorMessage.CreateBuilder()
                .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER)
                .Build());

            return true;
        }

        private bool OnTryModifyCommunityMemberCircle(IFrontendClient client, MailboxMessage message)
        {
            // We are handling this in the grouping manager to avoid exposing the ChatHelper class
            // TODO: Remove this and handle it in game after we implemented social functionality there.
            ChatHelper.SendMetagameMessage(client, "Social features are not yet implemented.", false);
            return true;
        }

        #endregion
    }
}
