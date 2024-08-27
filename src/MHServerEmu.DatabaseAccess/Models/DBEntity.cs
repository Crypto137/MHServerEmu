namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBEntity
    {
        // NOTE: We store 64 bit integers as signed because the Dapper + SQLite combo throws overflow exceptions with ulong values over 2^63

        public long DbGuid { get; set; }
        public long ContainerDbGuid { get; set; }
        public long InventoryProtoGuid { get; set; }
        public uint Slot { get; set; }
        public long EntityProtoGuid { get; set; }
        public byte[] ArchiveData { get; set; }
    }
}
