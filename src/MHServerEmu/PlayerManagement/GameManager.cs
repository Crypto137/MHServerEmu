using MHServerEmu.Core;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games;

namespace MHServerEmu.PlayerManagement
{
    public class GameManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, Game> _gameDict = new();

        public GameManager()
        {
            CreateGame();
        }

        public void CreateGame()
        {
            ulong id = _idGenerator.Generate();
            _gameDict.Add(id, new(id));
        }

        public Game GetGameById(ulong id)
        {
            if (_gameDict.TryGetValue(id, out Game game) == false)
            {
                Logger.Warn($"Failed to get game: id {id} not found");
                return null;
            }

            return game;
        }

        public Game GetAvailableGame()
        {
            if (_gameDict.Count == 0)
            {
                Logger.Warn($"Unable to get available game: no games are available");
                return null;
            }

            return _gameDict.First().Value;
        }
    }
}
