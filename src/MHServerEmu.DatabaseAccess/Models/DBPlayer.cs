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
            Reset();
        }

        public void Reset()
        {
            ArchiveData = Array.Empty<byte>();
            StartTarget = unchecked((long)15338215617681369199);    // Regions/StoryRevamp/CH00Raft/TimesSquare/ConnectionTargets/TimesSquareTutorialStartTarget.prototype
            StartTargetRegionOverride = 0;
            AOIVolume = 3200;
        }
    }

    public class DBPlayerName
    {
        public long Id { get; set; }
        public string PlayerName { get; set; }
    }
}
