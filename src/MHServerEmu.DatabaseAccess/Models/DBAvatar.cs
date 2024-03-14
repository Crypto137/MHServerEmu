namespace MHServerEmu.DatabaseAccess.Models
{
    /// <summary>
    /// Represents an avatar entity stored in the account database.
    /// </summary>
    public class DBAvatar
    {
        // We are currently using System.Data.SQLite + Dapper for storing our persistent data.
        // Because SQLite internally stores all 64 bit integers as signed, this causes Dapper
        // overflow errors when parsing ulong values larger than 2^63. To avoid this, we create
        // "raw" long properties for each ulong property that actually get saved to and loaded
        // from the SQLite database.

        // This isn't required for AccountId, because the first 4 bits in our id are allocated
        // to its type, and for account ids it is never more or equal to 1 << 3 = 8;

        public ulong AccountId { get; set; }

        public long RawPrototype { get; set; }
        public long RawCostume { get; set; }
        public byte[] RawAbilityKeyMapping { get; set; }

        public DBAvatar() { }

        public DBAvatar(ulong accountId, long prototypeId)
        {
            AccountId = accountId;
            RawPrototype = prototypeId;
        }
    }
}
