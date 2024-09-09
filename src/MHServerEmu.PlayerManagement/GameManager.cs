using MHServerEmu.Core.Logging;
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, Game> _gameDict = new();

        public int GameCount { get => _gameDict.Count; }

        /// <summary>
        /// Constructs a new <see cref="GameManager"/> instance.
        /// </summary>
        public GameManager()
        {
            
        }

        /// <summary>
        /// Creates and returns a new <see cref="Game"/> instance.
        /// </summary>
        public Game CreateGame()
        {
            ulong id = _idGenerator.Generate();
            Game game = new(id);
            _gameDict.Add(id, game);
            game.Run();
            return game;
        }

        /// <summary>
        /// Returns the <see cref="Game"/> instance with the specified id. Returns <see langword="null"/> if not found.
        /// </summary>
        public Game GetGameById(ulong id)
        {
            if (id == 0) return null;   // 0 just means the client is not in a game, this is valid output

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

            Game availableGame = _gameDict.First().Value;
            if (availableGame.HasBeenShutDown)
            {
                // We need to recreate the game if the one we had has been shut down
                _gameDict.Clear();
                availableGame = CreateGame();
            }

            return availableGame;
        }

        public void ShutdownAllGames()
        {
            foreach (var kvp in _gameDict)
            {
                kvp.Value.RequestShutdown();
                _gameDict.Remove(kvp.Key);  // Should be safe to remove while iterating as long as we use a dictionary
            }
        }
    }
}
