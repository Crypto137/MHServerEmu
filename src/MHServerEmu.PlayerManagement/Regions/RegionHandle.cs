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
            
            ServiceMessage.GameInstanceCreateRegion message = new(Game.Id, Id, (ulong)RegionProtoRef, CreateParams);
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
                State = RegionHandleState.Shutdown;
                Game.FinishRegionShutdown(Id);
                return true;
            }

            State = RegionHandleState.Running;
            Logger.Info($"Received instance creation confirmation for region [{this}]");

            foreach (PlayerHandle player in _transferringPlayers)
                player.OnRegionReadyToTransfer();
            _transferringPlayers.Clear();

            return true;
        }

        public bool AddPlayer(PlayerHandle player)
        {
            // If this region is already running, let the player in immediately. Otherwise do this when we receive creation confirmation.
            if (State == RegionHandleState.Running)
                player.OnRegionReadyToTransfer();
            else
                _transferringPlayers.Add(player);

            return true;
        }
    }
}
