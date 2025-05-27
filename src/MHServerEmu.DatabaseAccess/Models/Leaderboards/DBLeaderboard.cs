using MHServerEmu.Core.System.Time;

namespace MHServerEmu.DatabaseAccess.Models.Leaderboards
{
    public class DBLeaderboard
    {
        public long LeaderboardId { get; set; }	
        public string PrototypeName { get; set; }
        public long ActiveInstanceId { get; set; }
        public bool IsEnabled { get; set; }
        public long StartTime { get; set; }
        public int MaxResetCount { get; set; }

        public DBLeaderboard()
        {
        }

        public DBLeaderboard(DBLeaderboard other)
        {
            LeaderboardId = other.LeaderboardId;
            PrototypeName = other.PrototypeName;
            ActiveInstanceId = other.ActiveInstanceId;
            IsEnabled = other.IsEnabled;
            StartTime = other.StartTime;
            MaxResetCount = other.MaxResetCount;
        }

        public DateTime GetStartDateTime()
        {
            return Clock.TimestampToDateTime(StartTime);
        }

        public void SetStartDateTime(DateTime dateTime)
        {
            StartTime = Clock.DateTimeToTimestamp(dateTime);
        }
    }
}
