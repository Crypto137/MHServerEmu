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
            if (!Verify.IsTrue(State == GameHandleState.HandleCreated, $"Invalid state {State} for game [{this}"))
                return false;

            State = GameHandleState.PendingInstanceCreation;
            Logger.Trace($"Requesting instance creation for game [{this}]");

            ServiceMessage.GameInstanceOp gameInstanceOp = new(GameInstanceOpType.Create, Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }

        /// <summary>
        /// Switches this <see cref="GameHandle"/> to the Running state.
        /// </summary>
        public bool OnInstanceCreateResponse()
        {
            if (!Verify.IsTrue(State == GameHandleState.PendingInstanceCreation, $"Invalid state {State} for game [{this}]"))
                return false;

            State = GameHandleState.Running;
            Logger.Trace($"Received instance creation confirmation for game [{this}]");

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

            if (!Verify.IsTrue(State == GameHandleState.Running, $"Invalid state {State} for game [{this}]"))
                return false;

            State = GameHandleState.PendingShutdown;
            Logger.Trace($"Requesting instance shutdown for game [{this}]");

            ServiceMessage.GameInstanceOp gameInstanceOp = new(GameInstanceOpType.Shutdown, Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }
        
        /// <summary>
        /// Switches this <see cref="GameHandle"/> to the Shutdown state.
        /// </summary>
        public bool OnInstanceShutdownNotice()
        {
            if (!Verify.IsTrue(State == GameHandleState.Running || State == GameHandleState.PendingShutdown, $"Invalid state {State} for game [{this}]"))
                return false;

            if (State == GameHandleState.Running)
                Logger.Warn($"OnInstanceShutdownNotice(): Game [{this}] was shut down without a request");

            State = GameHandleState.Shutdown;
            Logger.Trace($"Received instance shutdown notification for game [{this}]");

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

            if (!Verify.IsTrue(State != GameHandleState.PendingShutdown && State != GameHandleState.Shutdown, $"Invalid state {State} for game [{this}]"))
                return false;

            if (!Verify.IsNotNull(createRegionParams, $"No params to create region 0x{regionId:X} ({regionProtoRef.GetName()})"))
                return false;

            region = new(this, regionId, regionProtoRef, createRegionParams, flags);
            _regions.Add(regionId, region);

            PlayerManagerService.Instance.WorldManager.AddRegion(region);

            // If this game is already running, request region instance creation immediately.
            // If it doesn't, this will be requested as soon as we receive the confirmation that it's running.
            if (State == GameHandleState.Running)
                region.RequestInstanceCreation();

            return true;
        }

        public void OnRegionShutdown(RegionHandle region)
        {
            PlayerManagerService.Instance.WorldManager.RemoveRegion(region);

            if (!Verify.IsTrue(_regions.Remove(region.Id), $"Region 0x{region.Id:X} not found"))
                return;

            // Shut this game down if all of its regions were shut down
            if (_regions.Count == 0 && State == GameHandleState.Running)
            {
                Logger.Trace($"Game [{this}] is no longer hosting any regions, shutting down...");
                RequestInstanceShutdown();
            }
        }

        #endregion

        #region Player Management

        public bool AddPlayer(PlayerHandle player)
        {
            if (!Verify.IsTrue(State == GameHandleState.Running, $"Invalid state {State} for game [{this}] when adding player [{player}]"))
                return false;

            if (!Verify.IsTrue(player.State == PlayerHandleState.Idle, $"Invalid state {player.State} for player [{player}] when adding to game [{this}]"))
                return false;

            if (!Verify.IsTrue(_players.Add(player), $"Player [{player}] is already added to game [{this}]"))
                return false;

            if (!Verify.IsTrue(player.BeginAddToGame(this), $"BeginAddToGame failed for player [{player}] when adding to game [{this}]"))
                return false;

            return true;
        }

        public bool RemovePlayer(PlayerHandle player)
        {
            // Not checking game state when removing players for now

            if (!Verify.IsTrue(player.State == PlayerHandleState.InGame, $"Invalid state {player.State} for player [{player}] when removing from game [{this}]"))
                return false;

            if (!Verify.IsTrue(_players.Remove(player), $"Player [{player}] not found in game [{this}]"))
                return false;

            if (!Verify.IsTrue(player.BeginRemoveFromGame(this), $"BeginRemoveFromGame failed for player [{player}] when removing from game [{this}]"))
                return false;

            return true;
        }

        #endregion
    }
}
