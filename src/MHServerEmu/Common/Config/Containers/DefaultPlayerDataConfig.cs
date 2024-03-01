using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Common.Config.Containers
{
    public class DefaultPlayerDataConfig : ConfigContainer
    {
        public string PlayerName { get; private set; }
        public string StartingRegion { get; private set; }
        public string StartingWaypoint { get; private set; }
        public string StartingAvatar { get; private set; }
        public int AOIVolume { get; private set; }

        [ConfigIgnore]
        public RegionPrototypeId StartingRegionEnum
        {
            get
            {
                if (Enum.TryParse(StartingRegion, out RegionPrototypeId @enum))
                    return @enum;

                return RegionPrototypeId.NPEAvengersTowerHUBRegion;
            }
        }

        [ConfigIgnore]
        public PrototypeId StartingWaypointId
        {
            get
            {
                PrototypeId waypointId = GameDatabase.GetPrototypeRefByName(StartingWaypoint);
                if (waypointId == PrototypeId.Invalid) return (PrototypeId)10137590415717831231;  // Waypoints/HUBS/NPEAvengersTowerHub.prototype
                return waypointId;
            }
        }

        [ConfigIgnore]
        public AvatarPrototypeId StartingAvatarEnum
        {
            get
            {
                if (Enum.TryParse(StartingAvatar, out AvatarPrototypeId @enum))
                    return @enum;

                return AvatarPrototypeId.BlackCat;
            }
        }

        public DefaultPlayerDataConfig(IniFile configFile) : base(configFile) { }
    }
}
