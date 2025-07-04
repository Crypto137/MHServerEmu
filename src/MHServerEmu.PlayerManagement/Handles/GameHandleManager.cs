using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;

namespace MHServerEmu.PlayerManagement.Handles
{
    /// <summary>
    /// Manages <see cref="GameHandle"/> instances.
    /// </summary>
    public class GameHandleManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, GameHandle> _gameDict = new();

        private int _targetGameInstanceCount = -1;
        private int _playerCountDivisor = 1;

        public int GameCount { get => _gameDict.Count; }

        public GameHandleManager() { }

        public void InitializeGames(int gameInstanceCount, int playerCountDivisor)
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

            game.RequestInstanceCreation();

            return game;
        }

        // TODO: Shutdown

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

            RefreshGames();

            return FindAvailableGame();
        }

        private void RefreshGames()
        {
            // Create replacement game instances if needed
            while (GameCount < _targetGameInstanceCount)
                CreateGame();
        }

        private GameHandle FindAvailableGame()
        {
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
    }
}
