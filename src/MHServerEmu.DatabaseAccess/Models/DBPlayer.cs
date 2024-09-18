namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBPlayer
    {
        public long DbGuid { get; set; }
        public byte[] ArchiveData { get; set; }
        public long StartTarget { get; set; }
        public long StartTargetRegionOverride { get; set; }
        public int AOIVolume { get; set; }

        public DBPlayer() { }

        public DBPlayer(long dbGuid)
        {
            DbGuid = dbGuid;
            StartTarget = unchecked((long)11334277059865941394);    // Regions/HUBRevamp/NPEAvengersTowerHubEntry.prototype
            StartTargetRegionOverride = 0;
            AOIVolume = 3200;
        }
    }
}
