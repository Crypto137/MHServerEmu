namespace MHServerEmu.DatabaseAccess.Models
{
    public enum DBPlayerFlags : long
    {
        None        = 0,
        Imported    = 1 << 0,   // Used to clean up archive data post import when a user logs in.
    }

    public class DBPlayer
    {
        public long DbGuid { get; set; }
        public byte[] ArchiveData { get; set; }
        public long StartTarget { get; set; }
        public int AOIVolume { get; set; }
        public long GazillioniteBalance { get; set; } = -1;     // -1 indicates that Gs need to be restored to the default value for new accounts when the player logs in
        public long LastLogoutTime { get; set; }
        public long Flags { get; set; }

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
            AOIVolume = 3200;
            GazillioniteBalance = -1;
            LastLogoutTime = 0;
            Flags = 0;
        }
    }
}
