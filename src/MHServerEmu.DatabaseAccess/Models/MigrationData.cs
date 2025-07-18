namespace MHServerEmu.DatabaseAccess.Models
{
    public class MigrationData
    {
        public bool SkipNextUpdate { get; set; }

        public bool IsFirstLoad { get; set; } = true;

        // TransferParams (TODO: change this to PlayerManager <-> GIS messages)
        public ulong DestTargetRegionProtoId { get; set; }
        public ulong DestTargetAreaProtoId { get; set; }
        public ulong DestTargetCellProtoId { get; set; }
        public ulong DestTargetEntityProtoId { get; set; }
        public bool HasDestTarget { get => DestTargetRegionProtoId != 0; }

        // Store everything here as ulong, PropertyCollection will sort it out game-side
        public List<KeyValuePair<ulong, ulong>> PlayerProperties { get; } = new(256);

        // TODO: Summoned inventory

        public MigrationData() { }

        public void Reset()
        {
            SkipNextUpdate = false;

            IsFirstLoad = true;

            DestTargetRegionProtoId = 0;
            DestTargetAreaProtoId = 0;
            DestTargetCellProtoId = 0;
            DestTargetEntityProtoId = 0;

            PlayerProperties.Clear();
        }
    }
}
