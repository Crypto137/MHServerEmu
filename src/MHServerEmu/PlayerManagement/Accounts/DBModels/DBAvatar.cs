using MHServerEmu.Games.Entities.Avatars;

namespace MHServerEmu.PlayerManagement.Accounts.DBModels
{
    public class DBAvatar
    {
        // We are currently using System.Data.SQLite + Dapper for storing our persistent data.
        // Because SQLite internally stores all 64 bit integers as signed, this causes Dapper
        // overflow errors when parsing ulong values larger than 2^63. To avoid this, we create
        // "raw" long properties for each ulong property that actually get saved to and loaded
        // from the SQLite database.

        // This isn't required for AccountId, because the first byte in our id is type, and it
        // never goes over 127 to make the account id value larger than 2^63.

        public ulong AccountId { get; set; }

        public AvatarPrototype Prototype { get; set; }
        public long RawPrototype { get => (long)Prototype; private set => Prototype = (AvatarPrototype)value; }

        public ulong Costume { get; set; } = 0;
        public long RawCostume { get => (long)Costume; private set => Costume = (ulong)value; }

        public DBAvatar(ulong accountId, AvatarPrototype prototype)
        {
            AccountId = accountId;
            Prototype = prototype;
        }

        public DBAvatar() { }
    }
}
