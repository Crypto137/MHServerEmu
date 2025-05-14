using MHServerEmu.Core.System.Time;

namespace MHServerEmu.DatabaseAccess.Models.Leaderboards
{
    public class DBLeaderboard
    {
        public long LeaderboardId { get; set; }	
        public string PrototypeName { get; set; }
        public long ActiveInstanceId { get; set; }
        public bool IsActive { get; set; }
        public int Frequency { get; set; }
        public int Interval { get; set; }
        public long StartEvent { get; set; }
        public long EndEvent { get; set; }

        public DateTime GetStartDateTime()
        {
            return Clock.TimestampToDateTime(StartEvent);
        }

        public void SetStartDateTime(DateTime dateTime)
        {
            StartEvent = Clock.DateTimeToTimestamp(dateTime);
        }

        public DateTime GetEndDateTime()
        {
            return Clock.TimestampToDateTime(EndEvent);
        }

        public void SetEndDateTime(DateTime dateTime)
        {
            EndEvent = Clock.DateTimeToTimestamp(dateTime);
        }
    }
}
