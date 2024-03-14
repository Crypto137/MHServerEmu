using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.PlayerManagement.Accounts.DBModels
{
    /// <summary>
    /// Represents a player entity stored in the account database.
    /// </summary>
    public class DBPlayer
    {
        // We are currently using System.Data.SQLite + Dapper for storing our persistent data.
        // Because SQLite internally stores all 64 bit integers as signed, this causes Dapper
        // overflow errors when parsing ulong values larger than 2^63. To avoid this, we create
        // "raw" long properties for each ulong property that actually get saved to and loaded
        // from the SQLite database.

        // This isn't required for AccountId, because the first byte in our id is type, and it
        // never goes over 127 to make the account id value larger than 2^63.

        public ulong AccountId { get; set; }

        public PrototypeId Region { get => (PrototypeId)RawRegion; set => RawRegion = (long)value; }
        public long RawRegion { get; set; }

        public PrototypeId Avatar { get => (PrototypeId)RawAvatar; set => RawAvatar = (long)value; }
        public long RawAvatar { get; set; }
        public PrototypeId Waypoint { get => (PrototypeId)RawWaypoint; set => RawWaypoint = (long)value; }
        public long RawWaypoint { get; set; }
        
        public int AOIVolume { get; set; }

        public DBPlayer(ulong accountId)
        {
            AccountId = accountId;
            Region = (PrototypeId)RegionPrototypeId.NPEAvengersTowerHUBRegion;
            Avatar = (PrototypeId)AvatarPrototypeId.CaptainAmerica;
            Waypoint = (PrototypeId)10137590415717831231; // Waypoints/HUBS/NPEAvengersTowerHub
            AOIVolume = 3200;
        }

        public DBPlayer() { }
    }
}
