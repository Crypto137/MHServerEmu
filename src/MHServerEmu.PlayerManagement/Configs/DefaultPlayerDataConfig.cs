using MHServerEmu.Core.Config;

namespace MHServerEmu.PlayerManagement.Configs
{
    public class DefaultPlayerDataConfig : ConfigContainer
    {
        public string PlayerName { get; private set; } = "Player";
        public string StartingRegion { get; private set; } = "NPEAvengersTowerHUBRegion";
        public string StartingWaypoint { get; private set; } = "BlackCat";
        public string StartingAvatar { get; private set; } = "Waypoints/HUBS/NPEAvengersTowerHub.prototype";
        public int AOIVolume { get; private set; } = 3200;
    }
}
