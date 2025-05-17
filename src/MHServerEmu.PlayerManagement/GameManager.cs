using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.Games;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// Manages <see cref="Game"/> instances.
    /// </summary>
    /// <remarks>
    /// TODO: GameInstanceServer (GIS) implementation.
    /// </remarks>
    public class GameManager
    {
        // NOTE: This is a very rough early implementation just to do some testing with multiple game instances running at the same time.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, Game> _gameDict = new();

        private int _targetGameInstanceCount = -1;
        private int _playerCountDivisor = 1;

        public int GameCount { get => _gameDict.Count; }

        /// <summary>
        /// Constructs a new <see cref="GameManager"/> instance.
        /// </summary>
        public GameManager() { }

        public void InitializeGames(int gameInstanceCount, int playerCountDivisor)
        {
            // Should always have at least 1 game instance
            gameInstanceCount = Math.Max(gameInstanceCount, 1);

            for (int i = 0; i < gameInstanceCount; i++)
                CreateGame();

            _targetGameInstanceCount = gameInstanceCount;
            _playerCountDivisor = Math.Max(playerCountDivisor, 1);
        }

        /// <summary>
        /// Returns the <see cref="Game"/> instance with the specified id. Returns <see langword="null"/> if not found.
        /// </summary>
        public Game GetGameById(ulong id)
        {
            // 0 just means the client is not in a game, this is valid output
            if (id == 0)
                return null;

            // Having a valid id and not finding a game for it is bad
            if (_gameDict.TryGetValue(id, out Game game) == false)
                return Logger.WarnReturn<Game>(null, $"GetGameById(): id 0x{id:X} not found");

            return game;
        }

        /// <summary>
        /// Returns an available <see cref="Game"/> instance.
        /// </summary>
        public Game GetAvailableGame()
        {
            if (_gameDict.Count == 0)
                Logger.WarnReturn<Game>(null, $"GetAvailableGame(): No games are available");

            RefreshGames();

            return FindAvailableGame();
        }

        public void ShutdownAllGames()
        {
            foreach (var kvp in _gameDict)
            {
                kvp.Value.RequestShutdown();
                _gameDict.Remove(kvp.Key);  // Should be safe to remove while iterating as long as we use a dictionary
            }
        }

        public void BroadcastServiceMessage<T>(in T message) where T: struct, IGameServiceMessage
        {
            // REMOVEME: This should be handled by the GameInstanceService
            foreach (Game game in _gameDict.Values)
                game.ReceiveServiceMessage(message);
        }

        /// <summary>
        /// Creates and returns a new <see cref="Game"/> instance.
        /// </summary>
        private Game CreateGame()
        {
            ulong id = _idGenerator.Generate();
            Game game = new(id);
            _gameDict.Add(id, game);
            game.Run();
            return game;
        }

        private void RefreshGames()
        {
            // Clean up game instances that were shut down
            foreach (var kvp in _gameDict)
            {
                Game game = kvp.Value;

                if (game.HasBeenShutDown)
                    _gameDict.Remove(game.Id);
            }

            // Create replacement game instances if needed
            while (GameCount < _targetGameInstanceCount)
                CreateGame();
        }

        private Game FindAvailableGame()
        {
            // If there is only one game instance, just return it
            if (GameCount == 1)
            {
                foreach (Game game in _gameDict.Values)
                    return game;
            }

            // Do very basic load balancing based on player count
            Game resultGame = null;
            int lowestPlayerCount = int.MaxValue;

            foreach (Game game in _gameDict.Values)
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
