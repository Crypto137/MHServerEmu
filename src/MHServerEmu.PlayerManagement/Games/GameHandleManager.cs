using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;

namespace MHServerEmu.PlayerManagement.Games
{
    /// <summary>
    /// Manages <see cref="GameHandle"/> instances.
    /// </summary>
    public class GameHandleManager
    {
        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, GameHandle> _games = new();

        private readonly PlayerManagerService _playerManager;

        public int GameCount { get => _games.Count; }

        public GameHandleManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public GameHandle CreateGame()
        {
            ulong gameId = _idGenerator.Generate();

            GameHandle game = new(gameId);
            _games.Add(gameId, game);

            game.RequestInstanceCreation();

            return game;
        }

        public void Shutdown()
        {
            foreach (GameHandle game in _games.Values)
                game.RequestInstanceShutdown();
        }

        public bool TryGetGameById(ulong gameId, out GameHandle game)
        {
            return _games.TryGetValue(gameId, out game);
        }

        public void OnInstanceCreateResponse(ulong gameId)
        {
            if (!Verify.IsTrue(TryGetGameById(gameId, out GameHandle game), $"No handle found for gameId 0x{gameId:X}"))
                return;

            game.OnInstanceCreateResponse();
        }

        public void OnInstanceShutdownNotice(ulong gameId)
        {
            if (!Verify.IsTrue(TryGetGameById(gameId, out GameHandle game), $"No handle found for gameId 0x{gameId:X}"))
                return;

            game.OnInstanceShutdownNotice();
            _games.Remove(game.Id);
        }
    }
}
