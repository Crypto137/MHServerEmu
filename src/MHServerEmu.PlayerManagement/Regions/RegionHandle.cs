using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
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
        IsExpired                        = 1 << 2,
    }

    public enum RegionReservationType
    {
        WorldView,
        Presence,
    }

    /// <summary>
    /// Represents a region in a game instance.
    /// </summary>
    public class RegionHandle : IComparable<RegionHandle>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<PlayerHandle> _pendingPlayers = new();
        private readonly HashSet<PlayerHandle> _players = new();

        // Reservations are incremented when a region is added to a world view or we receive a confirmation that a player is in the region.
        // This prevents regions from being unexpectedly shut down.
        private int _worldViewReservationCount = 0;
        private int _presenceReservationCount = 0;

        public GameHandle Game { get; }
        public ulong Id { get; }
        public PrototypeId RegionProtoRef { get; }
        public RegionPrototype Prototype { get; }
        public NetStructCreateRegionParams CreateParams { get; }
        public PrototypeId DifficultyTierProtoRef { get => (PrototypeId)CreateParams.DifficultyTierProtoId; }

        public TimeSpan CreationTime { get; } = Clock.UnixTime;
        public TimeSpan Uptime { get => Clock.UnixTime - CreationTime; }

        // We currently never reset towns and allow unlimited numbers of players in them to have a more social experience on smaller servers.

        public bool IsPublic { get => Prototype.IsPublic; }
        public bool IsPrivateStory { get => Prototype.Behavior == RegionBehavior.PrivateStory; }
        public bool CanExpire { get => Prototype.Behavior == RegionBehavior.PublicCombatZone || Prototype.Behavior == RegionBehavior.MatchPlay; }

        public RegionHandleState State { get; private set; } = RegionHandleState.Pending;
        public RegionFlags Flags { get; private set; }
        public RegionPlayerAccessVar PlayerAccess { get; private set; } = RegionPlayerAccessVar.eRPA_Open;

        public int PlayerCount { get => _players.Count; }
        public int PlayerLimit { get => Prototype.PlayerLimit; }
        public bool IsFull { get => Prototype.Behavior != RegionBehavior.Town && PlayerCount >= PlayerLimit; }

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
            string regionName = RegionProtoRef.GetNameFormatted();
            string difficultyName = DifficultyTierProtoRef.GetNameFormatted();
            return $"[0x{Id:X}] {regionName} ({difficultyName})";
        }

        public int CompareTo(RegionHandle other)
        {
            int gameComparison = Game.Id.CompareTo(other.Game.Id);
            if (gameComparison != 0)
                return gameComparison;

            return Id.CompareTo(other.Id);
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

            foreach (PlayerHandle player in _pendingPlayers)
                player.OnRegionReadyToTransfer();
            _pendingPlayers.Clear();

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
            foreach (PlayerHandle player in _pendingPlayers)
            {
                if (player.CurrentGame != null)
                    player.CancelRegionTransfer(player.CurrentGame.Id, RegionTransferFailure.eRTF_DestinationInaccessible);
                else
                    player.Disconnect();
            }
            _pendingPlayers.Clear();            

            DestroyAccessPortalIfNeeded();

            Game.OnRegionShutdown(this);
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

        public bool RequestTransfer(PlayerHandle player)
        {
            // If this region is already running, let the player in immediately. Otherwise do this when we receive creation confirmation.
            if (State == RegionHandleState.Running)
                player.OnRegionReadyToTransfer();
            else
                _pendingPlayers.Add(player);

            return true;
        }

        public void AddPlayer(PlayerHandle player)
        {
            Logger.Trace($"AddPlayer(): [{this}] - [{player}]");

            if (IsPublic)
                PlayerManagerService.Instance.WorldManager.UnregisterPublicRegion(this);

            _players.Add(player);

            if (IsPublic && State != RegionHandleState.Shutdown)
                PlayerManagerService.Instance.WorldManager.RegisterPublicRegion(this);
        }

        public void RemovePlayer(PlayerHandle player)
        {
            Logger.Trace($"RemovePlayer(): [{this}] - [{player}]");

            if (IsPublic)
                PlayerManagerService.Instance.WorldManager.UnregisterPublicRegion(this);

            _players.Remove(player);

            if (IsPublic && State != RegionHandleState.Shutdown)
                PlayerManagerService.Instance.WorldManager.RegisterPublicRegion(this);
        }

        public void Reserve(RegionReservationType reservationType)
        {
            switch (reservationType)
            {
                case RegionReservationType.WorldView:
                    _worldViewReservationCount++;
                    break;

                case RegionReservationType.Presence:
                    _presenceReservationCount++;
                    break;

                default:
                    Logger.Warn($"Reserve(): Unknown reservation type {reservationType}");
                    break;
            }
        }

        public void Unreserve(RegionReservationType reservationType)
        {
            switch (reservationType)
            {
                case RegionReservationType.WorldView:
                    if (_worldViewReservationCount > 0)
                        _worldViewReservationCount--;
                    else
                        Logger.Warn("Unreserve(): _worldViewReservationCount == 0");
                    break;

                case RegionReservationType.Presence:
                    if (_presenceReservationCount > 0)
                        _presenceReservationCount--;
                    else
                        Logger.Warn("Unreserve(): _presenceReservationCount == 0");
                    break;

                default:
                    Logger.Warn($"Unreserve(): Unknown reservation type {reservationType}");
                    break;
            }

            ShutdownIfVacant();
        }

        public bool CheckExpiration()
        {
            if (CanExpire == false)
                return false;

            // No need to go through this if we have already flagged this region as expired.
            if (Flags.HasFlag(RegionFlags.IsExpired))
                return false;

            TimeSpan uptime = Uptime;
            if (uptime < Prototype.Lifetime)
                return false;

            Logger.Info($"Region [{this}] expired after {uptime:dd\\:hh\\:mm\\:ss}");
            Flags |= RegionFlags.IsExpired;
            PlayerAccess = RegionPlayerAccessVar.eRPA_InviteOnly;
            return true;
        }

        public void ShutdownIfVacant()
        {
            if (State == RegionHandleState.Shutdown)
                return;

            bool hasReservations = (_worldViewReservationCount + _presenceReservationCount) > 0;

            if (Flags.HasFlag(RegionFlags.CloseWhenReservationsReachesZero) && hasReservations == false)
            {
                Logger.Trace($"Region [{this}] is shutting down because its reservations reached zero");
                Shutdown(true);
                return;
            }

            if (Flags.HasFlag(RegionFlags.IsExpired) && hasReservations == false)
            {
                Logger.Trace($"Region [{this}] is shutting down because it has expired");
                Shutdown(true);
                return;
            }

            if (Flags.HasFlag(RegionFlags.ShutdownWhenVacant) && PlayerCount == 0 && _presenceReservationCount == 0)
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
