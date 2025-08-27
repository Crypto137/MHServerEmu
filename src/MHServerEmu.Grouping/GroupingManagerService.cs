﻿using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerService : IGameService, IMessageBroadcaster
    {
        private const ushort MuxChannel = 2;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _playerLock = new();
        private readonly Dictionary<ulong, IFrontendClient> _playerDbIdDict = new();
        private readonly Dictionary<string, IFrontendClient> _playerNameDict = new(StringComparer.OrdinalIgnoreCase); 
        // Store players in a name-client dictionary because tell messages are sent by player name

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        public GroupingManagerService()
        {
        }

        #region IGameService Implementation

        public void Run()
        {
            State = GameServiceState.Running;
        }

        public void Shutdown()
        {
            State = GameServiceState.Shutdown;
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                // NOTE: We haven't really seen this, but there is a ClientToGroupingManager protocol
                // that includes a single message - GetPlayerInfoByName. If we ever receive it, it should end up here.

                case ServiceMessage.AddClient addClient:
                    OnAddClient(addClient);
                    break;

                case ServiceMessage.RemoveClient removeClient:
                    OnRemoveClient(removeClient);
                    break;

                case ServiceMessage.GroupingManagerChat chat:
                    OnChat(chat.Client, chat.Chat, chat.PrestigeLevel, chat.PlayerFilter);
                    break;

                case ServiceMessage.GroupingManagerTell tell:
                    OnTell(tell.Client, tell.Tell);
                    break;

                case ServiceMessage.PlayerNameChanged playerNameChanged:
                    OnPlayerNameChanged(playerNameChanged);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Players: {_playerDbIdDict.Count}";
        }

        private void OnAddClient(in ServiceMessage.AddClient addClient)
        {
            AddClient(addClient.Client);
        }

        private void OnRemoveClient(in ServiceMessage.RemoveClient removeClient)
        {
            RemoveClient(removeClient.Client);
        }

        private void OnPlayerNameChanged(in ServiceMessage.PlayerNameChanged playerNameChanged)
        {
            OnPlayerNameChanged(playerNameChanged.PlayerDbId, playerNameChanged.OldPlayerName, playerNameChanged.NewPlayerName);
        }

        private bool OnChat(IFrontendClient client, NetMessageChat chat, int prestigeLevel, List<ulong> playerFilter)
        {
            DBAccount account = ((IDBAccountOwner)client).Account;

            if (string.IsNullOrEmpty(chat.TheMessage.Body) == false)
                Logger.Info($"[{ChatHelper.GetRoomName(chat.RoomType)}] [{account})]: {chat.TheMessage.Body}", LogCategory.Chat);

            ChatNormalMessage message = ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(account.PlayerName)
                .SetTheMessage(chat.TheMessage)
                .SetPrestigeLevel(prestigeLevel)
                .Build();

            if (playerFilter != null)
                SendMessageFiltered(playerFilter, message);
            else
                BroadcastMessage(message);

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
                ulong playerDbId = (ulong)account.Id;
                string playerName = account.PlayerName;

                if (_playerDbIdDict.ContainsKey(playerDbId))
                    return Logger.WarnReturn(false, $"AddFrontendClient(): Account {account} is already added");

                _playerDbIdDict.Add(playerDbId, client);
                _playerNameDict.Add(playerName, client);

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
                ulong playerDbId = (ulong)account.Id;
                string playerName = account.PlayerName;

                if (_playerDbIdDict.Remove(playerDbId) == false)
                    return Logger.WarnReturn(false, $"RemoveFrontendClient(): Account {account} not found");

                _playerNameDict.Remove(playerName);

                Logger.Info($"Removed client [{client}]");
                return true;
            }
        }

        private void OnPlayerNameChanged(ulong playerDbId, string oldPlayerName, string newPlayerName)
        {
            lock (_playerLock)
            {
                // Update the currently logged in player name lookup
                if (_playerDbIdDict.TryGetValue(playerDbId, out IFrontendClient client) == false)
                    return;

                if (_playerNameDict.Remove(oldPlayerName) == false)
                    Logger.Warn($"OnPlayerNameChanged(): Player 0x{playerDbId:X} is logged in, but doesn't have a name lookup");

                _playerNameDict.Add(newPlayerName, client);

                Logger.Info($"Update name for player 0x{playerDbId:X}: {oldPlayerName} => {newPlayerName}");
            }
        }

        public void SendMessageFiltered(List<ulong> playerFilter, IMessage message)
        {
            lock (_playerLock)
            {
                foreach (ulong playerDbId in playerFilter)
                {
                    if (TryGetClient(playerDbId, out IFrontendClient client) == false)
                    {
                        Logger.Warn($"SendMessageToMultiple(): Player 0x{playerDbId:X} not found");
                        continue;
                    }

                    client.SendMessage(MuxChannel, message);
                }
            }
        }

        public void BroadcastMessage(IMessage message)
        {
            lock (_playerLock)
            {
                foreach (var kvp in _playerDbIdDict)
                    kvp.Value.SendMessage(MuxChannel, message);
            }
        }

        public bool TryGetClient(ulong playerDbId, out IFrontendClient client)
        {
            return _playerDbIdDict.TryGetValue(playerDbId, out client);
        }

        public bool TryGetClient(string playerName, out IFrontendClient client)
        {
            return _playerNameDict.TryGetValue(playerName, out client);
        }

        #endregion
    }
}
