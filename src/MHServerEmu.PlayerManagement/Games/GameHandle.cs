using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.PlayerManagement.Games
{
    public enum GameHandleState
    {
        HandleCreated,
        PendingInstanceCreation,
        Running,
        PendingShutdown,
        Shutdown,
    }

    /// <summary>
    /// Represents a game instance managed by a GameInstanceService.
    /// </summary>
    public class GameHandle
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, RegionHandle> _regions = new();
        private readonly HashSet<PlayerHandle> _players = new();

        private bool _instanceCreationCancelled = false;

        public ulong Id { get; }
        public GameHandleState State { get; private set; }

        public TimeSpan CreationTime { get; } = Clock.UnixTime;
        public TimeSpan Uptime { get => Clock.UnixTime - CreationTime; }

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

            // Handle the edge case when we shut down a game instance while it's being created. There is probably a better way of handling this.
            if (_instanceCreationCancelled)
            {
                RequestInstanceShutdown();
                return true;
            }

            // Now that we are running we can create region instances.
            foreach (RegionHandle region in _regions.Values)
                region.RequestInstanceCreation();

            return true;
        }

        /// <summary>
        /// Requests the GameInstanceService to shut down the game instance for this <see cref="GameHandle"/>.
        /// </summary>
        public bool RequestInstanceShutdown()
        {
            // Handle the edge case when we shut down a game instance while it's being created. There is probably a better way of handling this.
            if (State == GameHandleState.PendingInstanceCreation)
            {
                Logger.Warn($"RequestInstanceShutdown(): Requested to shut down game [{this}] while it is being created");
                _instanceCreationCancelled = true;
                return true;
            }

            if (State != GameHandleState.Running)
                return Logger.WarnReturn(false, $"RequestInstanceShutdown(): Invalid state {State} for game [{this}]");

            State = GameHandleState.PendingShutdown;
            Logger.Info($"Requesting instance shutdown for game [{this}]");

            ServiceMessage.GameInstanceOp gameInstanceOp = new(GameInstanceOpType.Shutdown, Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }
        
        /// <summary>
        /// Switches this <see cref="GameHandle"/> to the Shutdown state.
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

            foreach (RegionHandle region in _regions.Values)
                region.Shutdown(false);

            return true;
        }

        #endregion

        #region Region Management

        public bool CreateRegion(ulong regionId, PrototypeId regionProtoRef, NetStructCreateRegionParams createRegionParams, RegionFlags flags, out RegionHandle region)
        {
            region = null;

            if (State == GameHandleState.PendingShutdown || State == GameHandleState.Shutdown)
                return Logger.WarnReturn(false, $"CreateRegion(): Invalid state {State} for game [{this}]");

            if (createRegionParams == null)
                return Logger.WarnReturn(false, $"CreateRegion(): No params to create region 0x{regionId:X}");

            region = new(this, regionId, regionProtoRef, createRegionParams, flags);
            _regions.Add(regionId, region);

            PlayerManagerService.Instance.WorldManager.AddRegion(region);

            // If this game is already running, request region instance creation immediately.
            // If it doesn't, this will be requested as soon as we receive the confirmation that it's running.
            if (State == GameHandleState.Running)
                region.RequestInstanceCreation();

            return true;
        }

        public bool OnRegionShutdown(RegionHandle region)
        {
            PlayerManagerService.Instance.WorldManager.RemoveRegion(region);

            if (_regions.Remove(region.Id) == false)
                return Logger.WarnReturn(false, $"FinishRegionShutdown(): Region 0x{region.Id:X} not found");

            // Shut this game down if all of its regions were shut down
            if (_regions.Count == 0 && State == GameHandleState.Running)
            {
                Logger.Trace($"Game [{this}] is no longer hosting any regions, shutting down...");
                RequestInstanceShutdown();
            }

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
