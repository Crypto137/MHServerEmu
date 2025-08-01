using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Regions
{
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
            Logger.Info($"Requesting instance creation for region [{this}]");
            ServiceMessage.GameInstanceCreateRegion message = new(Game.Id, Id, (ulong)RegionProtoRef, CreateParams);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            return true;
        }
    }
}
