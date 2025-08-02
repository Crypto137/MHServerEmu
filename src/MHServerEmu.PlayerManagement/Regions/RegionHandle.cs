using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Regions
{
    public enum RegionHandleState
    {
        HandleCreated,
        PendingInstanceCreation,
        Running,
        PendingShutdown,
        Shutdown,
    }

    /// <summary>
    /// Represents a region in a game instance.
    /// </summary>
    public class RegionHandle
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public GameHandle Game { get; }
        public ulong Id { get; }
        public PrototypeId RegionProtoRef { get; }
        public NetStructCreateRegionParams CreateParams { get; }

        public RegionHandleState State { get; private set; } = RegionHandleState.HandleCreated;

        public RegionHandle(GameHandle game, ulong id, ulong regionProtoRef, NetStructCreateRegionParams createParams)
        {
            Game = game;
            Id = id;
            RegionProtoRef = (PrototypeId)regionProtoRef;
            CreateParams = createParams;
        }

        public override string ToString()
        {
            return $"{RegionProtoRef.GetNameFormatted()} (0x{Id:X})";
        }

        public bool RequestInstanceCreation()
        {
            if (State != RegionHandleState.HandleCreated)
                return Logger.WarnReturn(false, $"RequestInstanceCreation(): Invalid state {State} for region [{this}]");

            State = RegionHandleState.PendingInstanceCreation;
            Logger.Info($"Requesting instance creation for region [{this}]");
            
            ServiceMessage.GameInstanceCreateRegion message = new(Game.Id, Id, (ulong)RegionProtoRef, CreateParams);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            
            return true;
        }

        public bool OnInstanceCreateResponse(bool result)
        {
            if (State != RegionHandleState.PendingInstanceCreation)
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

            return true;
        }
    }
}
