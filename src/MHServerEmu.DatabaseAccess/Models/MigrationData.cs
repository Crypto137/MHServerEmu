using Gazillion;

namespace MHServerEmu.DatabaseAccess.Models
{
    public class MigrationData
    {
        public bool SkipNextUpdate { get; set; }

        public bool IsFirstLoad { get; set; } = true;
        public NetStructTransferParams TransferParams { get; set; }     // TODO: change this to PlayerManager <-> GIS messages?

        // Store everything here as ulong, PropertyCollection will sort it out game-side
        public List<KeyValuePair<ulong, ulong>> PlayerProperties { get; } = new(256);

        // TODO: Summoned inventory

        public MigrationData() { }

        public void Reset()
        {
            SkipNextUpdate = false;

            IsFirstLoad = true;
            TransferParams = null;
            PlayerProperties.Clear();
        }
    }
}
