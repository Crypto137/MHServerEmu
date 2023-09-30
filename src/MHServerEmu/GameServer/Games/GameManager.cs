using MHServerEmu.Common;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.GameServer.Games
{
    public class GameManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly GameServerManager _gameServerManager;
        private Dictionary<ulong, Game> _gameDict = new();

        public GameManager(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
            CreateGame();
        }

        public void CreateGame()
        {
            ulong id = IdGenerator.Generate(IdType.Game);
            _gameDict.Add(id, new(_gameServerManager, id));
        }

        public Game GetGameById(ulong id)
        {
            if (_gameDict.ContainsKey(id))
            {
                return _gameDict[id];
            }
            else
            {
                Logger.Warn($"Failed to get game {id}: id is not in the dictionary");
                return null;
            }
        }

        public Game GetAvailableGame()
        {
            if (_gameDict.Count > 0)
            {
                return _gameDict.First().Value;
            }
            else
            {
                Logger.Warn($"Unable to get available game: no games are available");
                return null;
            }
        }
    }
}
