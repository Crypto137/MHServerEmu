using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;

namespace MHServerEmu.PlayerManagement.Games
{
    /// <summary>
    /// Manages <see cref="GameHandle"/> instances.
    /// </summary>
    public class GameHandleManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, GameHandle> _gameDict = new();

        private readonly PlayerManagerService _playerManager;

        public int GameCount { get => _gameDict.Count; }

        public GameHandleManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
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

        public void Shutdown()
        {
            foreach (GameHandle game in _gameDict.Values)
                game.RequestInstanceShutdown();
        }

        public bool TryGetGameById(ulong gameId, out GameHandle game)
        {
            return _gameDict.TryGetValue(gameId, out game);
        }

        public bool OnInstanceCreateResponse(ulong gameId)
        {
            if (TryGetGameById(gameId, out GameHandle game) == false)
                return Logger.WarnReturn(false, $"OnCreateResponse(): No handle found for gameId 0x{gameId:X}");

            game.OnInstanceCreateResponse();

            return true;
        }

        public bool OnInstanceShutdownNotice(ulong gameId)
        {
            if (TryGetGameById(gameId, out GameHandle game) == false)
                return Logger.WarnReturn(false, $"OnShutdownNotice(): No handle found for gameId 0x{gameId:X}");

            game.OnInstanceShutdownNotice();
            _gameDict.Remove(game.Id);

            return true;
        }
    }
}
