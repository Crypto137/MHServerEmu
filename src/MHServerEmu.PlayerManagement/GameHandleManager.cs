using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// Manages <see cref="GameHandle"/> instances.
    /// </summary>
    public class GameHandleManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, GameHandle> _gameDict = new();

        private readonly DoubleBufferQueue<GameServiceProtocol.GameInstanceOp> _messageQueue = new();

        private int _targetGameInstanceCount = -1;
        private int _playerCountDivisor = 1;

        public int GameCount { get => _gameDict.Count; }
        public bool IsShuttingDown { get; set; }

        public GameHandleManager() { }

        public void Update()
        {
            ProcessMessageQueue();
            RefreshGames();
        }

        public void ReceiveMessage(in GameServiceProtocol.GameInstanceOp message)
        {
            _messageQueue.Enqueue(message);
        }

        public void Initialize(int gameInstanceCount, int playerCountDivisor)
        {
            // Should always have at least 1 game instance
            gameInstanceCount = Math.Max(gameInstanceCount, 1);

            for (int i = 0; i < gameInstanceCount; i++)
                CreateGame();

            _targetGameInstanceCount = gameInstanceCount;
            _playerCountDivisor = Math.Max(playerCountDivisor, 1);
        }

        public GameHandle CreateGame()
        {
            ulong gameId = _idGenerator.Generate();

            GameHandle game = new(gameId);
            _gameDict.Add(gameId, game);

            Logger.Info($"Created game handle [{game}]");

            game.RequestInstanceCreation();

            return game;
        }

        public void ShutDownAllGames()
        {
            foreach (GameHandle game in _gameDict.Values)
                game.RequestInstanceShutdown();
        }

        public bool TryGetGameById(ulong gameId, out GameHandle game)
        {
            return _gameDict.TryGetValue(gameId, out game);
        }

        /// <summary>
        /// Returns a<see cref="GameHandle"/> for an available game instance.
        /// </summary>
        public GameHandle GetAvailableGame()
        {
            if (_gameDict.Count == 0)
                return Logger.WarnReturn<GameHandle>(null, $"GetAvailableGame(): No games are available");

            // If there is only one game instance, just return it
            if (GameCount == 1)
            {
                foreach (GameHandle game in _gameDict.Values)
                    return game;
            }

            // Do very basic load balancing based on player count
            GameHandle resultGame = null;
            int lowestPlayerCount = int.MaxValue;

            foreach (GameHandle game in _gameDict.Values)
            {
                if (game.State != GameHandleState.Running)
                    continue;

                // Divide player count to make sure:
                // - Instances are not underpopulated at lower player counts
                // - Players logging in at the same time are more likely to be put into the same instance
                int playerCount = game.PlayerCount / _playerCountDivisor;
                if (playerCount < lowestPlayerCount)
                {
                    resultGame = game;
                    lowestPlayerCount = playerCount;
                }
            }

            return resultGame;
        }

        #region Ticking

        private void ProcessMessageQueue()
        {
            _messageQueue.Swap();

            while (_messageQueue.CurrentCount > 0)
            {
                GameServiceProtocol.GameInstanceOp gameInstanceOp = _messageQueue.Dequeue();

                switch (gameInstanceOp.Type)
                {
                    case GameInstanceOpType.CreateResponse:
                        OnCreateResponse(gameInstanceOp.GameId);
                        break;

                    case GameInstanceOpType.ShutdownNotice:
                        OnShutdownNotice(gameInstanceOp.GameId);
                        break;

                    default:
                        Logger.Warn($"OnGameInstanceOp(): Unhandled operation type {gameInstanceOp.Type}");
                        break;
                }
            }
        }

        private void RefreshGames()
        {
            // Remove handles for games that were shut down
            foreach (var kvp in _gameDict)
            {
                if (kvp.Value.State == GameHandleState.Shutdown)
                {
                    Logger.Info($"Removing handle for shutdown game [{kvp.Value}]");
                    _gameDict.Remove(kvp.Key);
                }
            }

            // Create replacement game instances if needed
            if (IsShuttingDown == false)
            {
                while (GameCount < _targetGameInstanceCount)
                    CreateGame();
            }
        }

        #endregion

        #region Message Handling

        private bool OnCreateResponse(ulong gameId)
        {
            if (TryGetGameById(gameId, out GameHandle game) == false)
                return Logger.WarnReturn(false, $"OnCreateResponse(): No handle found for gameId 0x{gameId:X}");

            game.OnInstanceCreateResponse();

            return true;
        }

        private bool OnShutdownNotice(ulong gameId)
        {
            if (TryGetGameById(gameId, out GameHandle game) == false)
                return Logger.WarnReturn(false, $"OnShutdownNotice(): No handle found for gameId 0x{gameId:X}");

            game.OnInstanceShutdownNotice();

            return true;
        }

        #endregion
    }
}
