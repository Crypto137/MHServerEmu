using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games;
using MHServerEmu.Networking;

namespace MHServerEmu.PlayerManagement
{
    public class GameManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ServerManager _serverManager;
        private readonly Dictionary<ulong, Game> _gameDict = new();

        public GameManager(ServerManager serverManager)
        {
            _serverManager = serverManager;
            CreateGame();
        }

        public void CreateGame()
        {
            ulong id = IdGenerator.Generate(IdType.Game);
            _gameDict.Add(id, new(_serverManager, id));
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
