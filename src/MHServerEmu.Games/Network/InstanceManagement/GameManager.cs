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

        private readonly Dictionary<ulong, Game> _gameDict = new();                     // GameId -> Game
        private readonly Dictionary<IFrontendClient, Game> _gameByClientDict = new();   // Client -> Game
        private readonly Dictionary<ulong, Game> _gameByPlayerDbIdDict = new();         // PlayerDbId -> Game

        public int GameCount { get => _gameDict.Count; }

        public GameManager() { }

        public bool TryGetGameById(ulong gameId, out Game game)
        {
            return _gameDict.TryGetValue(gameId, out game);
        }

        public bool TryGetGameForClient(IFrontendClient client, out Game game)
        {
            return _gameByClientDict.TryGetValue(client, out game);
        }

        public bool TryGetGameForPlayerDbId(ulong playerDbId, out Game game)
        {
            return _gameByPlayerDbIdDict.TryGetValue(playerDbId, out game);
        }

        public Dictionary<ulong, Game>.ValueCollection.Enumerator GetEnumerator()
        {
            return _gameDict.Values.GetEnumerator();
        }

        public bool CreateGame(ulong gameId)
        {
            if (TryGetGameById(gameId, out _))
                return Logger.WarnReturn(false, $"CreateGame(): GameId 0x{gameId:X} is already in use by another game");

            Game game = new(gameId, this);
            _gameDict.Add(gameId, game);
            
            game.Run();

            return true;
        }

        public bool ShutdownGame(ulong gameId)
        {
            if (TryGetGameById(gameId, out Game game) == false)
                return Logger.WarnReturn(false, $"ShutdownGame(): GameId 0x{gameId:X} not found");

            game.Shutdown(GameShutdownReason.ServerShuttingDown);
            return true;
        }

        public bool AddClientToGame(IFrontendClient client, ulong gameId)
        {
            if (TryGetGameForClient(client, out Game game))
                return Logger.WarnReturn(false, $"AddClientToGame(): Attempting to add client [{client}] to game 0x{gameId:X}, but the client is already in game 0x{game.Id:X}");

            if (TryGetGameById(gameId, out game) == false)
                return Logger.WarnReturn(false, $"AddClientToGame(): Attempting to add client [{client}] to game 0x{gameId:X}, but no game with this id exists");

            game.AddClient(client);
            _gameByClientDict.Add(client, game);
            _gameByPlayerDbIdDict.Add(client.DbId, game);

            return true;
        }

        public bool RemoveClientFromGame(IFrontendClient client, ulong gameId)
        {
            if (TryGetGameForClient(client, out Game game) == false)
                return Logger.WarnReturn(false, $"RemoveClientFromGame(): Attempting to remove client [{client}] from game 0x{gameId:X}, but the client is not in a game");

            if (game.Id != gameId)
                return Logger.WarnReturn(false, $"RemoveClientFromGame(): Attempting to remove client [{client}] from game 0x{gameId:X}, but the client is in game 0x{game.Id:X}");

            game.RemoveClient(client);
            _gameByClientDict.Remove(client);
            _gameByPlayerDbIdDict.Remove(client.DbId);

            return true;
        }

        #region Game Event Handling

        public void OnGameCreated(Game game)
        {
            GameServiceProtocol.GameInstanceOp message = new(GameServiceProtocol.GameInstanceOp.OpType.CreateAck, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, message);
        }

        public void OnGameShutdown(Game game)
        {
            GameServiceProtocol.GameInstanceOp message = new(GameServiceProtocol.GameInstanceOp.OpType.ShutdownAck, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, message);
        }

        public void OnClientAdded(Game game, IFrontendClient client)
        {
            GameServiceProtocol.GameInstanceClientOp message = new(GameServiceProtocol.GameInstanceClientOp.OpType.AddAck, client, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, message);
        }

        public void OnClientRemoved(Game game, IFrontendClient client)
        {
            GameServiceProtocol.GameInstanceClientOp message = new(GameServiceProtocol.GameInstanceClientOp.OpType.RemoveAck, client, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, message);
        }

        #endregion
    }
}
