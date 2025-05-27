using Gazillion;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.DatabaseAccess.Models.Leaderboards
{
    public class DBLeaderboardInstance
    {
        public long InstanceId { get; set; }
        public long LeaderboardId { get; set; }
        public LeaderboardState State { get; set; }
        public long ActivationDate { get; set; }
        public bool Visible { get; set; }

        public DateTime GetActivationDateTime()
        {
            return Clock.TimestampToDateTime(ActivationDate);
        }

        public void SetActivationDateTime(DateTime dateTime)
        {
            ActivationDate = Clock.DateTimeToTimestamp(dateTime);
        }
    }
}
