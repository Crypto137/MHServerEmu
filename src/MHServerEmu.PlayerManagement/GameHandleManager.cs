using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// Manages <see cref="GameHandle"/> instances.
    /// </summary>
    public class GameHandleManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IdGenerator _idGenerator = new(IdType.Game, 0);
        private readonly Dictionary<ulong, GameHandle> _gameDict = new();

        private readonly DoubleBufferQueue<ServiceMessage.GameInstanceOp> _messageQueue = new();

        private readonly PlayerManagerService _playerManager;

        public int GameCount { get => _gameDict.Count; }
        public bool IsShuttingDown { get; set; }

        public GameHandleManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Update()
        {
            ProcessMessageQueue();
        }

        public void ReceiveMessage(in ServiceMessage.GameInstanceOp message)
        {
            _messageQueue.Enqueue(message);
        }

        public void Initialize()
        {
            // Just a single game instance for testing now
            GameHandle game = CreateGame();

            ulong regionId = _playerManager.WorldManager.NextRegionId;
            ulong regionProtoRef = 9142075282174842340;
            NetStructCreateRegionParams createParams = NetStructCreateRegionParams.CreateBuilder().SetLevel(0).Build();

            game.CreateRegion(regionId, regionProtoRef, createParams);
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

        public void ShutDownAllGames()
        {
            foreach (GameHandle game in _gameDict.Values)
                game.RequestInstanceShutdown();
        }

        public bool TryGetGameById(ulong gameId, out GameHandle game)
        {
            return _gameDict.TryGetValue(gameId, out game);
        }

        /// <summary>
        /// Returns a<see cref="GameHandle"/> for an available game instance.
        /// </summary>
        public GameHandle GetAvailableGame()
        {
            // REMOVEME
            return _gameDict.Values.First();
        }

        #region Ticking

        private void ProcessMessageQueue()
        {
            _messageQueue.Swap();

            while (_messageQueue.CurrentCount > 0)
            {
                ServiceMessage.GameInstanceOp gameInstanceOp = _messageQueue.Dequeue();

                switch (gameInstanceOp.Type)
                {
                    case GameInstanceOpType.CreateResponse:
                        OnCreateResponse(gameInstanceOp.GameId);
                        break;

                    case GameInstanceOpType.ShutdownNotice:
                        OnShutdownNotice(gameInstanceOp.GameId);
                        break;

                    default:
                        Logger.Warn($"OnGameInstanceOp(): Unhandled operation type {gameInstanceOp.Type}");
                        break;
                }
            }
        }

        #endregion

        #region Message Handling

        private bool OnCreateResponse(ulong gameId)
        {
            if (TryGetGameById(gameId, out GameHandle game) == false)
                return Logger.WarnReturn(false, $"OnCreateResponse(): No handle found for gameId 0x{gameId:X}");

            game.OnInstanceCreateResponse();

            return true;
        }

        private bool OnShutdownNotice(ulong gameId)
        {
            if (TryGetGameById(gameId, out GameHandle game) == false)
                return Logger.WarnReturn(false, $"OnShutdownNotice(): No handle found for gameId 0x{gameId:X}");

            game.OnInstanceShutdownNotice();

            return true;
        }

        #endregion
    }
}
