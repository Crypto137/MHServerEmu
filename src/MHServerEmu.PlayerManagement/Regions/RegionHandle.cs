using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Regions
{
    public enum RegionHandleState
    {
        Pending,
        Running,
        Shutdown,
    }

    /// <summary>
    /// Represents a region in a game instance.
    /// </summary>
    public class RegionHandle
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<PlayerHandle> _transferringPlayers = new();

        public GameHandle Game { get; }
        public ulong Id { get; }
        public PrototypeId RegionProtoRef { get; }
        public NetStructCreateRegionParams CreateParams { get; }

        public RegionHandleState State { get; private set; } = RegionHandleState.Pending;

        public RegionHandle(GameHandle game, ulong id, PrototypeId regionProtoRef, NetStructCreateRegionParams createParams)
        {
            Game = game;
            Id = id;
            RegionProtoRef = regionProtoRef;
            CreateParams = createParams;
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
                foreach (PlayerHandle player in _transferringPlayers)
                {
                    // Try to cancel the transfer and return players to regions they were in.
                    // If this is not possible (the player is logging in and is not in a game/region yet), disconnect.
                    if (player.CurrentGame != null)
                        player.CancelRegionTransfer(player.CurrentGame.Id, RegionTransferFailure.eRTF_DestinationInaccessible);
                    else
                        player.Disconnect();
                }

                return Shutdown(false);
            }

            State = RegionHandleState.Running;
            Logger.Info($"Received instance creation confirmation for region [{this}]");

            foreach (PlayerHandle player in _transferringPlayers)
                player.OnRegionReadyToTransfer();
            _transferringPlayers.Clear();

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

        public void OnPlayerEntered(PlayerHandle player)
        {
            Logger.Debug($"OnPlayerEntered(): [{this}] - [{player}]");
        }

        public void OnPlayerLeft(PlayerHandle player)
        {
            Logger.Debug($"OnPlayerLeft(): [{this}] - [{player}]");
        }
    }
}
