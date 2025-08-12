using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Games.Network.InstanceManagement
{
    /// <summary>
    /// Manages <see cref="Game"/> instances.
    /// </summary>
    public class GameManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _clientLock = new();

        private readonly Dictionary<ulong, Game> _gameDict = new();                     // GameId -> Game

        // We use separate lookups for IFrontendClient and PlayerDbId because in theory a player
        // may attempt to reconnect before they are removed from the game they were in.
        // In practice the PlayerManager should prevent this from happening, but better safe than sorry.
        private readonly Dictionary<IFrontendClient, Game> _gameByClientDict = new();   // Client -> Game
        private readonly Dictionary<ulong, Game> _gameByPlayerDbIdDict = new();         // PlayerDbId -> Game

        private readonly GameInstanceService _gis;

        public int GameCount { get => _gameDict.Count; }
        public int PlayerCount { get => _gameByClientDict.Count; }

        public GameManager(GameInstanceService gis)
        {
            _gis = gis;
        }

        public bool TryGetGameById(ulong gameId, out Game game)
        {
            lock (_gameDict)
                return _gameDict.TryGetValue(gameId, out game);
        }

        public bool TryGetGameForClient(IFrontendClient client, out Game game)
        {
            lock (_clientLock)
                return _gameByClientDict.TryGetValue(client, out game);
        }

        public bool TryGetGameForPlayerDbId(ulong playerDbId, out Game game)
        {
            lock (_clientLock)
                return _gameByPlayerDbIdDict.TryGetValue(playerDbId, out game);
        }

        public bool CreateGame(ulong gameId)
        {
            Logger.Info($"Received creation request for gameId=0x{gameId:X}");

            if (TryGetGameById(gameId, out _))
                return Logger.WarnReturn(false, $"CreateGame(): GameId 0x{gameId:X} is already in use by another game");

            Game game = new(gameId, this);

            lock (_gameDict)
                _gameDict.Add(gameId, game);

            _gis.GameThreadManager.EnqueueGameToUpdate(game);

            ServiceMessage.GameInstanceOp message = new(GameInstanceOpType.CreateResponse, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);

            return true;
        }

        public bool ShutdownGame(ulong gameId, GameShutdownReason reason)
        {
            Logger.Info($"Received shutdown request for gameId=0x{gameId:X}");

            if (TryGetGameById(gameId, out Game game) == false)
                return Logger.WarnReturn(false, $"ShutdownGame(): GameId 0x{gameId:X} not found");

            game.Shutdown(reason);
            return true;
        }

        public bool AddClientToGame(IFrontendClient client, ulong gameId)
        {
            Logger.Info($"Received add client request for client=[{client}] gameId=0x{gameId:X}");

            if (TryGetGameForClient(client, out Game game))
                return Logger.WarnReturn(false, $"AddClientToGame(): Attempting to add client [{client}] to game 0x{gameId:X}, but the client is already in game 0x{game.Id:X}");

            if (TryGetGameForPlayerDbId(client.DbId, out game))
                return Logger.WarnReturn(false, $"AddClientToGame(): Attempting to add client [{client}] to game 0x{gameId:X}, but the PlayerDbId is already in use in game 0x{game.Id:X}");

            if (TryGetGameById(gameId, out game) == false)
                return Logger.WarnReturn(false, $"AddClientToGame(): Attempting to add client [{client}] to game 0x{gameId:X}, but no game with this id exists");

            game.AddClient(client);

            lock (_clientLock)
            {
                _gameByClientDict.Add(client, game);
                _gameByPlayerDbIdDict.Add(client.DbId, game);
            }

            // Clients are added on the next game tick, so don't send the Ack message to the player manager yet

            return true;
        }

        public bool RemoveClientFromGame(IFrontendClient client, ulong gameId, bool isGameOriginatingRequest = false)
        {
            Logger.Info($"Received remove request for client=[{client}] gameId=0x{gameId:X}");

            if (TryGetGameForClient(client, out Game game) == false)
                return Logger.WarnReturn(false, $"RemoveClientFromGame(): Attempting to remove client [{client}] from game 0x{gameId:X}, but the client is not in a game");

            if (game.Id != gameId)
                return Logger.WarnReturn(false, $"RemoveClientFromGame(): Attempting to remove client [{client}] from game 0x{gameId:X}, but the client is in game 0x{game.Id:X}");

            // This request may be originating from a game instance that failed to add a client (e.g. if it disconnected while pending)
            if (isGameOriginatingRequest == false)
                game.RemoveClient(client);

            lock (_clientLock)
            {
                _gameByClientDict.Remove(client);
                _gameByPlayerDbIdDict.Remove(client.DbId);
            }

            // Clients are removed on the next game tick, so don't send the Ack message to the player manager yet

            return true;
        }

        #region Message Routing

        public bool RouteMessageBuffer(IFrontendClient client, in MessageBuffer messageBuffer)
        {
            if (TryGetGameForClient(client, out Game game) == false)
            {
                // The player may be transferring to another game instance, in which case this message is not going to be delivered.
                //Logger.Debug($"RouteMessageBuffer(): Cannot deliver {(ClientToGameServerMessage)messageBuffer.MessageId}, client [{client}] is not in a game");
                messageBuffer.Destroy();
                return false;
            }

            game.ReceiveMessageBuffer(client, messageBuffer);
            return true;
        }

        public bool RouteServiceMessageToPlayer<T>(ulong playerDbId, in T message) where T: struct, IGameServiceMessage
        {
            if (TryGetGameForPlayerDbId(playerDbId, out Game game) == false)
                return Logger.WarnReturn(false, $"RouteServiceMessageToPlayer(): Failed to route {typeof(T).Name}, player 0x{playerDbId:X} is not in a game");

            game.ReceiveServiceMessage(message);
            return true;
        }

        public void BroadcastServiceMessageToGames<T>(in T message) where T: struct, IGameServiceMessage
        {
            lock (_gameDict)
            {
                foreach (Game game in _gameDict.Values)
                    game.ReceiveServiceMessage(message);
            }
        }

        #endregion

        #region Game Event Handling

        public void OnGameShutdown(Game game)
        {
            lock (_gameDict)
                _gameDict.Remove(game.Id);

            ServiceMessage.GameInstanceOp message = new(GameInstanceOpType.ShutdownNotice, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        public void OnClientAdded(Game game, IFrontendClient client)
        {
            ServiceMessage.GameInstanceClientOp message = new(GameInstanceClientOpType.AddResponse, client, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        public void OnClientRemoved(Game game, IFrontendClient client)
        {
            ServiceMessage.GameInstanceClientOp message = new(GameInstanceClientOpType.RemoveResponse, client, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        #endregion
    }
}
