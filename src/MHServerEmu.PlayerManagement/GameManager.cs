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
            return game;
        }

        /// <summary>
        /// Returns the <see cref="Game"/> instance with the specified id. Returns <see langword="null"/> if not found.
        /// </summary>
        public Game GetGameById(ulong id)
        {
            if (_gameDict.TryGetValue(id, out Game game) == false)
                return Logger.WarnReturn<Game>(null, $"GetGameById(): id {id} not found");

            return game;
        }

        /// <summary>
        /// Returns an available <see cref="Game"/> instance.
        /// </summary>
        public Game GetAvailableGame()
        {
            if (_gameDict.Count == 0)
                Logger.WarnReturn<Game>(null, $"GetAvailableGame(): No games are available");

            return _gameDict.First().Value;
        }
    }
}
