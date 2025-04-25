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

                case GameServiceProtocol.GroupingManagerChat chat:
                    OnChat(chat.Client, chat.Chat);
                    break;

                case GameServiceProtocol.GroupingManagerTell tell:
                    OnTell(tell.Client, tell.Tell);
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

        private bool OnChat(IFrontendClient client, NetMessageChat chat)
        {
            // Try to parse the message as a command first
            if (_commandParser != null && _commandParser.TryParse(chat.TheMessage.Body, client))
                return true;

            DBAccount account = ((IDBAccountOwner)client).Account;

            // Broadcast the message if everything's okay
            if (string.IsNullOrEmpty(chat.TheMessage.Body) == false)
                Logger.Info($"[{ChatHelper.GetRoomName(chat.RoomType)}] [{account})]: {chat.TheMessage.Body}", LogCategory.Chat);

            // Right now all messages are broadcasted to all connected players
            BroadcastMessage(ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(account.PlayerName)
                .SetTheMessage(chat.TheMessage)
                .Build());

            return true;
        }

        private bool OnTell(IFrontendClient client, NetMessageTell tell)
        {
            Logger.Trace($"Received tell for {tell.TargetPlayerName}");

            // Respond with an error for now
            client.SendMessage(MuxChannel, ChatErrorMessage.CreateBuilder()
                .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_NO_SUCH_USER)
                .Build());

            return true;
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
    }
}
