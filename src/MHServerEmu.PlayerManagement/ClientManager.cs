using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.PlayerManagement
{
    public class ClientManager
    {
        // This is conceptually similar to NetworkManager, but PlayerHandle can be a reference to a disconnect player that is currently being saved.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PlayerHandle> _playerDict = new();

        private readonly DoubleBufferQueue<IGameServiceMessage> _messageQueue = new();

        private readonly PlayerManagerService _playerManager;

        public int PlayerCount { get => _playerDict.Count; }

        public ClientManager(PlayerManagerService playerManager) 
        {
            _playerManager = playerManager;
        }

        public void Update(bool allowNewClients)
        {
            ProcessMessageQueue(allowNewClients);

            ProcessIdlePlayers();
        }

        public void ReceiveMessage<T>(in T message) where T: struct, IGameServiceMessage
        {
            _messageQueue.Enqueue(message);
        }

        #region Ticking

        private void ProcessMessageQueue(bool allowNewClients)
        {
            _messageQueue.Swap();

            while (_messageQueue.CurrentCount > 0)
            {
                IGameServiceMessage message = _messageQueue.Dequeue();

                switch (message)
                {
                    case ServiceMessage.AddClient addClient:
                        if (OnAddClient(addClient, allowNewClients) == false)
                            addClient.Client.Disconnect();
                        break;

                    case ServiceMessage.RemoveClient removeClient:
                        OnRemoveClient(removeClient);
                        break;

                    case ServiceMessage.GameInstanceClientOp gameInstanceClientOp:
                        OnGameInstanceClientOp(gameInstanceClientOp);
                        break;

                    case ServiceMessage.ChangeRegionRequest changeRegionRequest:
                        OnChangeRegionRequest(changeRegionRequest);
                        break;

                    case ServiceMessage.RegionTransferFinished regionTransferFinished:
                        OnRegionTransferFinished(regionTransferFinished);
                        break;

                    case ServiceMessage.ClearPrivateStoryRegions clearPrivateStoryRegions:
                        OnClearPrivateStoryRegions(clearPrivateStoryRegions);
                        break;

                    default:
                        Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {message.GetType().Name}");
                        break;
                }
            }
        }

        private void ProcessIdlePlayers()
        {
            lock (_playerDict)
            {
                foreach (PlayerHandle player in _playerDict.Values)
                {
                    if (player.State != PlayerHandleState.Idle)
                        continue;

                    if (player.IsConnected)
                    {
                        if (player.HasTransferParams == false)
                            player.BeginRegionTransferToStartTarget();

                        player.TryJoinGame();
                    }
                    else
                    {
                        RemovePlayerHandle(player.Client);
                    }
                }
            }
        }

        #endregion

        #region PlayerHandle Management

        public bool TryGetPlayerHandle(ulong playerDbId, out PlayerHandle player)
        {
            lock (_playerDict)
                return _playerDict.TryGetValue(playerDbId, out player);
        }

        public void BroadcastMessage(IMessage message)
        {
            lock (_playerDict)
            {
                foreach (PlayerHandle player in _playerDict.Values)
                    player.SendMessage(message);
            }
        }

        private bool CreatePlayerHandle(IFrontendClient client, out PlayerHandle player)
        {
            player = null;
            ulong playerDbId = client.DbId;

            if (_playerDict.TryGetValue(playerDbId, out player) == false)
            {
                player = new(client);
                _playerDict.Add(playerDbId, player);
                Logger.Info($"Created new PlayerHandle: [{player}]");

                player.LoadPlayerData();
            }
            else
            {
                Logger.Info($"Reusing existing PlayerHandle: [{player}]");
                if (player.MigrateSession(client) == false)
                {
                    Logger.Warn($"CreatePlayerHandle(): Failed to migrate existing session to client [{client}], disconnecting");
                    client.Disconnect();
                    player = null;
                    return false;
                }
            }

            return true;
        }

        private bool RemovePlayerHandle(IFrontendClient client)
        {
            ulong playerDbId = client.DbId;

            if (_playerDict.Remove(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"RemovePlayer(): Client [{client}] is not bound to a PlayerHandle");

            Logger.Info($"Removed PlayerHandle [{player}]");

            player.OnRemoved();
            return true;
        }

        #endregion

        #region Service Message Handling

        private bool OnAddClient(in ServiceMessage.AddClient addClient, bool allowNewClients)
        {
            IFrontendClient client = addClient.Client;

            if (allowNewClients == false)
                return Logger.WarnReturn(false, $"AddClient(): Client [{client}] is not allowed to connect because the server is shutting down");

            ClientSession session = (ClientSession)client.Session;
            if (session == null || session.Account == null)
                return Logger.WarnReturn(false, $"AddClient(): Client [{client}] has no valid session assigned");

            if (_playerManager.LoginQueueManager.RemovePendingClient(client) == false)
                return Logger.WarnReturn(false, $"AddClient(): Client [{client}] is attempting to log in without passing the login queue");

            if (CreatePlayerHandle(client, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"AddClient(): Failed to get or create player handle for client [{client}]");

            Logger.Info($"Added client [{client}]");
            player.SendMessage(NetMessageReadyAndLoggedIn.DefaultInstance);

            return true;
        }

        private bool OnRemoveClient(in ServiceMessage.RemoveClient removeClient)
        {
            IFrontendClient client = removeClient.Client;

            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, $"OnRemoveClient(): Client [{client}] has no valid session assigned");

            _playerManager.SessionManager.RemoveActiveSession(client.Session.Id);

            if (TryGetPlayerHandle(client.DbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnRemoveClient(): Failed to get player handle for client [{client}]");

            // When we are handling duplicate logins this handle may already have a different client,
            // in which case removal from game will be handled by the migration process.
            if (client == player.Client)
                player.RemoveFromCurrentGame();

            TimeSpan sessionLength = client.Session != null ? ((ClientSession)client.Session).Length : TimeSpan.Zero;
            Logger.Info($"Removed client [{client}] (SessionLength={sessionLength:hh\\:mm\\:ss})");
            return true;
        }

        private bool OnGameInstanceClientOp(in ServiceMessage.GameInstanceClientOp gameInstanceClientOp)
        {
            IFrontendClient client = gameInstanceClientOp.Client;
            ulong gameId = gameInstanceClientOp.GameId;

            if (TryGetPlayerHandle(client.DbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): No handle found for client [{client}]");

            switch (gameInstanceClientOp.Type)
            {
                case GameInstanceClientOpType.AddResponse:
                    player.FinishAddToGame(gameId);
                    break;

                case GameInstanceClientOpType.RemoveResponse:
                    player.FinishRemoveFromGame(gameId);
                    break;

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Unhandled operation type {gameInstanceClientOp.Type}");
            }

            return true;
        }

        private bool OnChangeRegionRequest(in ServiceMessage.ChangeRegionRequest changeRegionRequest)
        {
            ulong requestingGameId = changeRegionRequest.Header.RequestingGameId;
            ulong playerDbId = changeRegionRequest.Header.RequestingPlayerGuid;
            TeleportContextEnum context = changeRegionRequest.Header.Type;

            if (TryGetPlayerHandle(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnChangeRegionRequest(): No player handle for dbid 0x{playerDbId:X}");

            if (changeRegionRequest.DestTarget != null)
                return player.BeginRegionTransferToTarget(requestingGameId, context, changeRegionRequest.DestTarget, changeRegionRequest.CreateRegionParams);

            if (changeRegionRequest.DestLocation != null)
                return player.BeginRegionTransferToLocation(requestingGameId, context, changeRegionRequest.DestLocation);

            if (changeRegionRequest.DestPlayerDbId != 0)
                return player.BeginRegionTransferToPlayer(requestingGameId, changeRegionRequest.DestPlayerDbId);

            Logger.Warn($"BeginRegionTransfer(): ChangeRegionRequest for player [{this}] does not include transfer params");
            player.CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_GenericError);
            return false;
        }

        private bool OnRegionTransferFinished(in ServiceMessage.RegionTransferFinished regionTransferFinished)
        {
            if (TryGetPlayerHandle(regionTransferFinished.PlayerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnRegionTransferFinished(): No handle found for playerDbId 0x{regionTransferFinished.PlayerDbId}");

            return player.FinishRegionTransfer(regionTransferFinished.TransferId);
        }

        private bool OnClearPrivateStoryRegions(in ServiceMessage.ClearPrivateStoryRegions clearPrivateStoryRegions)
        {
            if (TryGetPlayerHandle(clearPrivateStoryRegions.PlayerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnClearPrivateStoryRegions(): No handle found for playerDbId 0x{clearPrivateStoryRegions.PlayerDbId}");

            player.WorldView.ClearPrivateStoryRegions();
            return true;
        }

        #endregion
    }
}
