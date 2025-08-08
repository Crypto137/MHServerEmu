using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.PlayerManagement.Regions
{
    public enum RegionHandleState
    {
        Pending,
        Running,
        Shutdown,
    }

    [Flags]
    public enum RegionFlags
    {
        None                             = 0,
        CloseWhenReservationsReachesZero = 1 << 0,
        ShutdownWhenVacant               = 1 << 1,
    }

    /// <summary>
    /// Represents a region in a game instance.
    /// </summary>
    public class RegionHandle
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<PlayerHandle> _transferringPlayers = new();
        private readonly HashSet<PlayerHandle> _playersInRegion = new();

        // When a region is added to a player's world view it gets "reserved", which prevents it from unexpectedly shutting down in some cases.
        private int _reservationCount = 0;

        public GameHandle Game { get; }
        public ulong Id { get; }
        public PrototypeId RegionProtoRef { get; }
        public RegionPrototype Prototype { get; }
        public NetStructCreateRegionParams CreateParams { get; }

        public bool IsPrivateStory { get => Prototype.Behavior == RegionBehavior.PrivateStory; }

        public RegionHandleState State { get; private set; } = RegionHandleState.Pending;
        public RegionFlags Flags { get; private set; }

        public int TransferCount { get => _transferringPlayers.Count; }
        public int PlayerCount { get => _playersInRegion.Count; }

        public RegionHandle(GameHandle game, ulong id, PrototypeId regionProtoRef, NetStructCreateRegionParams createParams, RegionFlags flags)
        {
            Game = game;
            Id = id;
            RegionProtoRef = regionProtoRef;
            Prototype = regionProtoRef.As<RegionPrototype>();
            CreateParams = createParams;

            Flags = flags;

            if (Prototype.CloseWhenReservationsReachesZero)
                Flags |= RegionFlags.CloseWhenReservationsReachesZero;

            if (Prototype.AlwaysShutdownWhenVacant)
                Flags |= RegionFlags.ShutdownWhenVacant;
        }

        public override string ToString()
        {
            return $"{RegionProtoRef.GetNameFormatted()} (0x{Id:X})";
        }

        public bool RequestInstanceCreation()
        {
            if (State != RegionHandleState.Pending)
                return Logger.WarnReturn(false, $"RequestInstanceCreation(): Invalid state {State} for region [{this}]");

            Logger.Info($"Requesting instance creation for region [{this}]");
            
            ServiceMessage.CreateRegion message = new(Game.Id, Id, (ulong)RegionProtoRef, CreateParams);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            
            return true;
        }

        public bool OnInstanceCreateResponse(bool result)
        {
            if (State != RegionHandleState.Pending)
                return Logger.WarnReturn(false, $"OnInstanceCreateResponse(): Invalid state {State} for region [{this}]");

            if (result == false)
            {
                Logger.Warn($"OnInstanceCreateResponse(): Region [{this}] failed to generate");
                return Shutdown(false);
            }

            State = RegionHandleState.Running;
            Logger.Info($"Received instance creation confirmation for region [{this}]");

            foreach (PlayerHandle player in _transferringPlayers)
                player.OnRegionReadyToTransfer();
            _transferringPlayers.Clear();

            return true;
        }

        public bool RequestShutdown()
        {
            Flags |= RegionFlags.CloseWhenReservationsReachesZero;
            Flags |= RegionFlags.ShutdownWhenVacant;

            ShutdownIfVacant();
            return true;
        }

        public bool Shutdown(bool sendShutdownToGis)
        {
            // Instruct the game instance service to shut down this region if needed.
            // We don't differentiate between pending shutdown and shutdown here, so we don't need a confirmation.
            if (sendShutdownToGis)
            {
                ServiceMessage.ShutdownRegion shutdownMessage = new(Game.Id, Id);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, shutdownMessage);
            }

            State = RegionHandleState.Shutdown;

            // Try to cancel the transfer and return players to regions they were in.
            // If this is not possible (the player is logging in and is not in a game/region yet), disconnect.
            foreach (PlayerHandle player in _transferringPlayers)
            {
                if (player.CurrentGame != null)
                    player.CancelRegionTransfer(player.CurrentGame.Id, RegionTransferFailure.eRTF_DestinationInaccessible);
                else
                    player.Disconnect();
            }
            _transferringPlayers.Clear();            

            DestroyAccessPortalIfNeeded();

            Game.OnRegionShutdown(Id);
            return true;
        }

        public bool MatchesCreateParams(NetStructCreateRegionParams otherParams)
        {
            if (CreateParams.DifficultyTierProtoId != otherParams.DifficultyTierProtoId)
                return false;

            // EndlessLevel > 0 indicates that this is an endless region, in which case the level needs to match.
            if (CreateParams.EndlessLevel != 0 && CreateParams.EndlessLevel != otherParams.EndlessLevel)
                return false;

            // If the other params specify an explicit seed, this needs to match too.
            if (otherParams.Seed != 0 && CreateParams.Seed != otherParams.Seed)
                return false;

            // Some regions (Cow/Doop levels, Danger Room) are created by and bound to specific transition entities.
            // Interacting with the same transition entity should transfer the player to the same region instance.
            if (CreateParams.HasAccessPortal)
            {
                if (otherParams.HasAccessPortal == false)
                    return false;

                if (CreateParams.AccessPortal.EntityDbId != otherParams.AccessPortal.EntityDbId)
                    return false;
            }

            return true;
        }

        public bool AddTransferringPlayer(PlayerHandle player)
        {
            // If this region is already running, let the player in immediately. Otherwise do this when we receive creation confirmation.
            if (State == RegionHandleState.Running)
                player.OnRegionReadyToTransfer();
            else
                _transferringPlayers.Add(player);

            return true;
        }

        public void OnAddedToWorldView(WorldView worldView)
        {
            _reservationCount++;
        }

        public void OnRemovedFromWorldView(WorldView worldView)
        {
            if (_reservationCount > 0)
                _reservationCount--;
            else
                Logger.Warn("OnRemovedFromWorldView(): _reservationCount == 0");

            ShutdownIfVacant();
        }

        public void OnPlayerEntered(PlayerHandle player)
        {
            Logger.Debug($"OnPlayerEntered(): [{this}] - [{player}]");
            _playersInRegion.Add(player);
        }

        public void OnPlayerLeft(PlayerHandle player)
        {
            Logger.Debug($"OnPlayerLeft(): [{this}] - [{player}]");
            _playersInRegion.Remove(player);
            ShutdownIfVacant();
        }

        private void ShutdownIfVacant()
        {
            if (State == RegionHandleState.Shutdown)
                return;

            if (Flags.HasFlag(RegionFlags.CloseWhenReservationsReachesZero) && _reservationCount == 0)
            {
                Logger.Trace($"Region [{this}] is shutting down because its reservations reached zero");
                Shutdown(true);
                return;
            }

            if (Flags.HasFlag(RegionFlags.ShutdownWhenVacant) && PlayerCount == 0 && TransferCount == 0)
            {
                Logger.Trace($"Region [{this}] is shutting down because it became vacant");
                Shutdown(true);
                return;
            }
        }

        private bool DestroyAccessPortalIfNeeded()
        {
            if (CreateParams.HasAccessPortal == false)
                return false;

            // Treasure rooms in some of the older regions also use access portals, don't destroy these.
            if (CreateParams.AccessPortal.BoundToOwner == false)
                return false;

            RegionHandle portalRegion = PlayerManagerService.Instance.WorldManager.GetRegion(CreateParams.AccessPortal.Location.RegionId);
            if (portalRegion == null)
                return false;

            if (portalRegion.State == RegionHandleState.Shutdown)
                return false;

            ServiceMessage.DestroyPortal message = new(portalRegion.Game.Id, CreateParams.AccessPortal);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);

            return true;
        }
    }
}
