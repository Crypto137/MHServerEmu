using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameService, IFrontendService, IMessageBroadcaster
    {
        private const ushort MuxChannel = 2;    // All messages come from GroupingManager over mux channel 2

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _playerLock = new();
        private readonly Dictionary<string, FrontendClient> _playerDict = new();    // Store players in a name-client dictionary because tell messages are sent by player name

        private ICommandParser _commandParser;

        public GroupingManagerService(ICommandParser commandParser = null)
        {
            _commandParser = commandParser;
        }

        #region IGameService Implementation

        public void Run() { }

        public void Shutdown() { }

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            // NOTE: We haven't really seen this, but there is a ClientToGroupingManager protocol
            // that includes a single message - GetPlayerInfoByName. If we ever receive it,
            // it should end up here.
            message.Protocol = typeof(ClientToGroupingManagerMessage);

            switch ((ClientToGroupingManagerMessage)message.Id)
            {
                default:
                    Logger.Warn($"Handle(): Unhandled {(ClientToGroupingManagerMessage)message.Id} [{message.Id}]");
                    break;
            }
        }

        public void Handle(ITcpClient client, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages)
                Handle(client, message);
        }

        public void Handle(ITcpClient tcpClient, MailboxMessage message)
        {
            var client = (FrontendClient)tcpClient;

            // Handle messages routed from games
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageChat: OnChat(client, message); break;
                case ClientToGameServerMessage.NetMessageTell: OnTell(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        public string GetStatus()
        {
            return $"Players: {_playerDict.Count}";
        }

        #endregion

        #region Player Management

        public void ReceiveFrontendMessage(FrontendClient client, IMessage message)
        {
            if (message is InitialClientHandshake)
            {
                client.FinishedGroupingManagerHandshake = true;
                return;
            }

            Logger.Warn($"ReceiveFrontendMessage(): Unhandled message {message.DescriptorForType.Name}");
        }

        public bool AddFrontendClient(FrontendClient client)
        {
            lock (_playerLock)
            {
                string playerName = client.Session.Account.PlayerName.ToLower();

                if (_playerDict.ContainsKey(playerName))
                    return Logger.WarnReturn(false, "AddFrontendClient(): Already added");

                _playerDict.Add(playerName, client);
                client.SendMessage(MuxChannel, ChatHelper.Motd);

                Logger.Info($"Added client [{client}]");
                return true;
            }
        }

        public bool RemoveFrontendClient(FrontendClient client)
        {
            lock (_playerLock)
            {
                string playerName = client.Session.Account.PlayerName.ToLower();

                if (_playerDict.Remove(playerName) == false)
                    return Logger.WarnReturn(false, $"RemoveFrontendClient(): Player {client.Session.Account.PlayerName} not found");

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

        public bool TryGetPlayerByName(string playerName, out FrontendClient client) => _playerDict.TryGetValue(playerName.ToLower(), out client);

        #endregion

        #region Message Handling

        private bool OnChat(FrontendClient client, MailboxMessage message)
        {
            var chat = message.As<NetMessageChat>();
            if (chat == null) return Logger.WarnReturn(false, $"OnChat(): Failed to retrieve message");

            // Try to parse the message as a command first
            if (_commandParser != null && _commandParser.TryParse(chat.TheMessage.Body, client))
                return true;

            // Limit broadcast and metagame channels to users with moderator privileges and higher
            if ((chat.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS || chat.RoomType == ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                && client.Session.Account.UserLevel < AccountUserLevel.Moderator)
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
                Logger.Trace($"[{ChatHelper.GetRoomName(chat.RoomType)}] [{client.Session.Account})]: {chat.TheMessage.Body}");

            // Right now all messages are broadcasted to all connected players
            BroadcastMessage(ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(client.Session.Account.PlayerName)
                .SetTheMessage(chat.TheMessage)
                .Build());

            return true;
        }

        private bool OnTell(FrontendClient client, MailboxMessage message)
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

        #endregion
    }
}
