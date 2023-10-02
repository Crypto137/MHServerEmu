using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.GameServer.Frontend.Accounts.DBModels
{
    public class DBPlayer
    {
        // We are currently using System.Data.SQLite + Dapper for storing our persistent data.
        // Because SQLite internally stores all 64 bit values as signed integers, this causes
        // Dapper overflow errors when parsing ulong values larger than 2^63. To avoid this, we
        // create "raw" long properties for each ulong property that actually get saved to and
        // loaded from the SQLite database. We then cast these raw long values to ulong when we
        // need to actually access data.

        // This isn't required for AccountId, because the first byte in our id is type, and it
        // never goes over 127 to make the account id value larger than 2^63.

        public ulong AccountId { get; set; }

        public long RawRegion { get; private set; }
        public RegionPrototype Region { get => (RegionPrototype)RawRegion; set => RawRegion = (long)value; }

        public long RawAvatar { get; private set; }
        public AvatarPrototype Avatar { get => (AvatarPrototype)RawAvatar; set => RawAvatar = (long)value; }

        public DBPlayer(ulong accountId)
        {
            AccountId = accountId;
            Region = RegionPrototype.NPEAvengersTowerHUBRegion;
            Avatar = AvatarPrototype.CaptainAmerica;
        }

        public DBPlayer() { }
    }
}
