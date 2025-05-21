namespace MHServerEmu.DatabaseAccess.Models.Leaderboards
{
    public class DBMetaEntry
    {
        public long LeaderboardId { get; set; }
        public long InstanceId { get; set; }
        public long SubLeaderboardId { get; set; }
        public long SubInstanceId { get; set; }
    }
}
