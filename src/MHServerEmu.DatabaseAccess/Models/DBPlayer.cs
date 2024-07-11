namespace MHServerEmu.DatabaseAccess.Models
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

        // This isn't required for AccountId, because the first 4 bits in our id are allocated
        // to its type, and for account ids it is never more or equal to 1 << 3 = 8;

        public ulong AccountId { get; set; }

        public long RawRegion { get; set; }
        public long RawAvatar { get; set; }
        public long RawWaypoint { get; set; }
        public int AOIVolume { get; set; }

        // Additional data not saved to the database, but persisted between regions
        public long Credits { get; set; }

        public DBPlayer(ulong accountId)
        {
            AccountId = accountId;
            RawRegion = unchecked((long)9142075282174842340);       // Regions/HUBRevamp/NPEAvengersTowerHUBRegion.prototype
            RawAvatar = unchecked((long)10617813376954079152);      // Entity/Characters/Avatars/Shipping/CaptainAmerica.prototype
            RawWaypoint = unchecked((long)10137590415717831231);    // Waypoints/HUBS/NPEAvengersTowerHub
            AOIVolume = 3200;
        }

        public DBPlayer() { }
    }
}
