using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Configs
{
    /// <summary>
    /// Contains data for the default <see cref="DBAccount"/> used when BypassAuth is enabled.
    /// </summary>
    public class DefaultPlayerDataConfig : ConfigContainer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public string PlayerName { get; private set; } = "Player";
        public string StartingRegion { get; private set; } = "Regions/HUBRevamp/NPEAvengersTowerHUBRegion.prototype";
        public string StartingWaypoint { get; private set; } = "Waypoints/HUBS/NPEAvengersTowerHub.prototype";
        public string StartingAvatar { get; private set; } = "Entity/Characters/Avatars/Shipping/BlackCat.prototype";
        public int AOIVolume { get; private set; } = 3200;

        /// <summary>
        /// Returns a new <see cref="DBAccount"/> instance with data based on this <see cref="DefaultPlayerDataConfig"/>.
        /// </summary>
        public DBAccount InitializeDefaultAccount()
        {
            ulong regionId = HashHelper.HashPath(StartingRegion.ToCalligraphyPath());
            ulong waypointId = HashHelper.HashPath(StartingWaypoint.ToCalligraphyPath());
            ulong avatarId = HashHelper.HashPath(StartingAvatar.ToCalligraphyPath());

            return new(PlayerName, (long)regionId, (long)waypointId, (long)avatarId, AOIVolume);
        }
    }
}
