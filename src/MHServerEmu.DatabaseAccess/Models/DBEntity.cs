namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBEntity
    {
        public ulong DbGuid { get; set; }
        public ulong ContainerDbGuid { get; set; }
        public ulong InventoryProtoGuid { get; set; }
        public uint Slot { get; set; }
        public ulong EntityProtoGuid { get; set; }
        public byte[] Blob { get; set; }
    }
}
