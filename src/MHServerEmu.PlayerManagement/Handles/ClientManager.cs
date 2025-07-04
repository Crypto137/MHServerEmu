using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.PlayerManagement.Handles
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

        public void EnqueueMessage<T>(in T message) where T: struct, IGameServiceMessage
        {
            lock (_messageLock)
                _pendingMessageQueue.Enqueue(message);
        }

        public bool TryGetPlayer(IFrontendClient client, out PlayerHandle player)
        {
            lock (_playerDict)
                return _playerDict.TryGetValue(client.DbId, out player);
        }

        #region PlayerHandle Management

        public bool AddPlayer(IFrontendClient client, out PlayerHandle player)
        {
            player = null;
            ulong playerDbId = client.DbId;

            if (_playerDict.TryGetValue(playerDbId, out player) == false)
            {
                player = new(client);
                _playerDict.Add(playerDbId, player);
            }
            else
            {
                // TODO: Transfer handle between clients
                client.Disconnect();
                player = null;
                return Logger.ErrorReturn(false, $"AddPlayer(): PlayerDbId 0x{playerDbId:X} is already in use by another client");
            }

            return true;
        }

        public bool RemovePlayer(IFrontendClient client)
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

            if (AddPlayer(client, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"AddClient(): Failed to get or create player handle for client [{client}]");

            player.LoadPlayerData();

            GameHandle game = _playerManagerService.GameHandleManager.GetAvailableGame();

            game.AddPlayer(player);

            return true;
        }

        private bool OnRemoveClient(in GameServiceProtocol.RemoveClient removeClient)
        {
            IFrontendClient client = removeClient.Client;

            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, $"RemoveFrontendClient(): Client [{client}] has no valid session assigned");

            _playerManagerService.SessionManager.RemoveActiveSession(client.Session.Id);

            if (TryGetPlayer(client, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"RemoveClient(): Failed to get player handle for client [{client}]");

            player.BeginRemoveFromGame(player.Game);

            RemovePlayer(client);

            TimeSpan sessionLength = client.Session != null ? ((ClientSession)client.Session).SessionLength : TimeSpan.Zero;
            Logger.Info($"Removed client [{client}] (SessionLength={sessionLength:hh\\:mm\\:ss})");
            return true;
        }

        private bool OnGameInstanceClientOp(in GameServiceProtocol.GameInstanceClientOp gameInstanceClientOp)
        {
            IFrontendClient client = gameInstanceClientOp.Client;
            ulong gameId = gameInstanceClientOp.GameId;

            if (TryGetPlayer(client, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): No handle found for client [{client}]");

            switch (gameInstanceClientOp.Type)
            {
                case GameServiceProtocol.GameInstanceClientOp.OpType.AddAck:
                    player.FinalizePendingState();
                    break;

                case GameServiceProtocol.GameInstanceClientOp.OpType.RemoveAck:
                    player.FinalizePendingState();
                    break;

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Unhandled operation type {gameInstanceClientOp.Type}");
            }

            return true;
        }

        #endregion
    }
}
