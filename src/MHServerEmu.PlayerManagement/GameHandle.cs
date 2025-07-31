using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.PlayerManagement
{
    public enum GameHandleState
    {
        HandleCreated,
        Running,
        Shutdown,
        PendingInstanceCreation,
        PendingShutdown,
    }

    /// <summary>
    /// Represents a game instance managed by a GameInstanceService.
    /// </summary>
    public class GameHandle
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<PlayerHandle> _players = new();

        public ulong Id { get; }
        public GameHandleState State { get; private set; }

        public bool IsRunning { get => State == GameHandleState.Running; }
        public int PlayerCount { get => _players.Count; }

        public GameHandle(ulong id)
        {
            Id = id;
            State = GameHandleState.HandleCreated;
        }

        public override string ToString()
        {
            return $"0x{Id:X}";
        }

        #region State Management

        /// <summary>
        /// Requests the GameInstanceService to create a game instance for this <see cref="GameHandle"/>.
        /// </summary>
        public bool RequestInstanceCreation()
        {
            if (State != GameHandleState.HandleCreated)
                return Logger.WarnReturn(false, $"RequestInstanceCreation(): Invalid state {State} for game [{this}]");

            State = GameHandleState.PendingInstanceCreation;
            Logger.Info($"Requesting instance creation for game [{this}]");

            ServiceMessage.GameInstanceOp gameInstanceOp = new(GameInstanceOpType.Create, Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }

        /// <summary>
        /// Switches this <see cref="GameHandle"/> to the Running state.
        /// </summary>
        public bool OnInstanceCreateResponse()
        {
            if (State != GameHandleState.PendingInstanceCreation)
                return Logger.WarnReturn(false, $"OnInstanceCreateResponse(): Invalid state {State} for game [{this}]");

            State = GameHandleState.Running;
            Logger.Info($"Received instance creation confirmation for game [{this}]");

            return true;
        }

        /// <summary>
        /// Requests the GameInstanceService to shut down the game instance for this <see cref="GameHandle"/>.
        /// </summary>
        public bool RequestInstanceShutdown()
        {
            if (State != GameHandleState.Running)
                return Logger.WarnReturn(false, $"RequestInstanceShutdown(): Invalid state {State} for game [{this}]");

            State = GameHandleState.PendingShutdown;
            Logger.Info($"Requesting instance shutdown for game [{this}]");

            ServiceMessage.GameInstanceOp gameInstanceOp = new(GameInstanceOpType.Shutdown, Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }
        
        /// <summary>
        /// Swithces this <see cref="GameHandle"/> to the Shutdown state.
        /// </summary>
        public bool OnInstanceShutdownNotice()
        {
            if (State != GameHandleState.PendingShutdown)
            {
                if (State == GameHandleState.Running)
                    Logger.Warn($"OnInstanceShutdownNotice(): Game [{this}] was shut down without a request");
                else
                    return Logger.WarnReturn(false, $"OnInstanceShutdownNotice(): Invalid state {State} for game [{this}]");
            }

            State = GameHandleState.Shutdown;
            Logger.Info($"Received instance shutdown notification for game [{this}]");

            foreach (PlayerHandle player in _players)
                player.Disconnect();

            return true;
        }

        #endregion

        #region Player Management

        public bool AddPlayer(PlayerHandle player)
        {
            if (State != GameHandleState.Running)
                return Logger.WarnReturn(false, $"AddPlayer(): Invalid state {State} for game [{this}] when adding player [{player}]");

            if (player.State != PlayerHandleState.Idle)
                return Logger.WarnReturn(false, $"AddPlayer(): Invalid state {player.State} for player [{player}] when adding to game [{this}]");

            if (_players.Add(player) == false)
                return Logger.WarnReturn(false, $"AddClient(): Player [{player}] is already added to game [{this}]");

            if (player.BeginAddToGame(this) == false)
                return Logger.WarnReturn(false, $"AddClient(): BeginAddToGame failed for player [{player}] when adding to game [{this}]");

            return true;
        }

        public bool RemovePlayer(PlayerHandle player)
        {
            // Not checking game state when removing players for now

            if (player.State != PlayerHandleState.InGame)
                return Logger.WarnReturn(false, $"RemovePlayer(): Invalid state {player.State} for player [{player}] when removing from game [{this}]");

            if (_players.Remove(player) == false)
                return Logger.WarnReturn(false, $"RemoveClient(): Player [{player}] not found in game [{this}]");

            if (player.BeginRemoveFromGame(this) == false)
                return Logger.WarnReturn(false, $"RemovePlayer(): BeginRemoveFromGame failed for player [{player}] when removing from game [{this}]");

            return true;
        }

        #endregion

    }
}
