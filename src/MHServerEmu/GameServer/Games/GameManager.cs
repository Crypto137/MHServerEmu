using MHServerEmu.Common;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.GameServer.Games
{
    public class GameManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<ulong, Game> _gameDict = new();

        public GameManager()
        {
            CreateGame();
        }

        public void CreateGame()
        {
            ulong id = HashHelper.GenerateUniqueRandomId(_gameDict);
            _gameDict.Add(id, new(id));
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
