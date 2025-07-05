using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.PlayerManagement
{
    public class ClientManager
    {
        // This is conceptually similar to NetworkManager, but PlayerHandle can be a reference to a disconnect player that is currently being saved.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PlayerHandle> _playerDict = new();

        private readonly object _messageLock = new();
        private Queue<IGameServiceMessage> _pendingMessageQueue = new();
        private Queue<IGameServiceMessage> _messageQueue = new();

        private readonly PlayerManagerService _playerManagerService;

        public ClientManager(PlayerManagerService playerManagerService) 
        {
            _playerManagerService = playerManagerService;
        }

        public void Update()
        {
            ProcessMessageQueue();

            ProcessIdlePlayers();
        }

        public void EnqueueMessage<T>(in T message) where T: struct, IGameServiceMessage
        {
            lock (_messageLock)
                _pendingMessageQueue.Enqueue(message);
        }

        #region Ticking

        private void ProcessMessageQueue()
        {
            lock (_messageLock)
                (_pendingMessageQueue, _messageQueue) = (_messageQueue, _pendingMessageQueue);

            while (_messageQueue.Count > 0)
            {
                IGameServiceMessage message = _messageQueue.Dequeue();

                switch (message)
                {
                    case GameServiceProtocol.AddClient addClient:
                        OnAddClient(addClient);
                        break;

                    case GameServiceProtocol.RemoveClient removeClient:
                        OnRemoveClient(removeClient);
                        break;

                    case GameServiceProtocol.GameInstanceClientOp gameInstanceClientOp:
                        OnGameInstanceClientOp(gameInstanceClientOp);
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

                    IFrontendClient client = player.Client;

                    if (client.IsConnected)
                    {
                        GameHandle game = _playerManagerService.GameHandleManager.GetAvailableGame();
                        game.AddPlayer(player);
                    }
                    else
                    {
                        RemovePlayerHandle(client);
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

        private bool CreatePlayerHandle(IFrontendClient client, out PlayerHandle player)
        {
            player = null;
            ulong playerDbId = client.DbId;

            if (_playerDict.TryGetValue(playerDbId, out player) == false)
            {
                player = new(client);
                _playerDict.Add(playerDbId, player);

                player.LoadPlayerData();
            }
            else
            {
                Logger.Info($"Existing handle found for player [{player}]");
                player.MigrateSession(client);
            }

            return true;
        }

        private bool RemovePlayerHandle(IFrontendClient client)
        {
            ulong playerDbId = client.DbId;

            if (_playerDict.Remove(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"RemovePlayer(): Client [{client}] is not bound to a player");

            return true;
        }

        #endregion

        #region Service Message Handling

        private bool OnAddClient(in GameServiceProtocol.AddClient addClient)
        {
            IFrontendClient client = addClient.Client;

            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, $"AddClient(): Client [{client}] has no valid session assigned");

            if (CreatePlayerHandle(client, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"AddClient(): Failed to get or create player handle for client [{client}]");

            Logger.Info($"Added client [{client}]");

            return true;
        }

        private bool OnRemoveClient(in GameServiceProtocol.RemoveClient removeClient)
        {
            IFrontendClient client = removeClient.Client;

            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, $"OnRemoveClient(): Client [{client}] has no valid session assigned");

            _playerManagerService.SessionManager.RemoveActiveSession(client.Session.Id);

            if (TryGetPlayerHandle(client.DbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnRemoveClient(): Failed to get player handle for client [{client}]");

            // When we are handling duplicate logins this handle may already have a different client,
            // in which case removal from game will be handled by the migration process.
            if (client == player.Client)
                player.RemoveFromCurrentGame();

            TimeSpan sessionLength = client.Session != null ? ((ClientSession)client.Session).SessionLength : TimeSpan.Zero;
            Logger.Info($"Removed client [{client}] (SessionLength={sessionLength:hh\\:mm\\:ss})");
            return true;
        }

        private bool OnGameInstanceClientOp(in GameServiceProtocol.GameInstanceClientOp gameInstanceClientOp)
        {
            IFrontendClient client = gameInstanceClientOp.Client;
            ulong gameId = gameInstanceClientOp.GameId;

            if (TryGetPlayerHandle(client.DbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): No handle found for client [{client}]");

            switch (gameInstanceClientOp.Type)
            {
                case GameServiceProtocol.GameInstanceClientOp.OpType.AddAck:
                    player.FinishAddToGame(gameId);
                    break;

                case GameServiceProtocol.GameInstanceClientOp.OpType.RemoveAck:
                    player.FinishRemoveFromGame(gameId);
                    break;

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Unhandled operation type {gameInstanceClientOp.Type}");
            }

            return true;
        }

        #endregion
    }
}
