using Gazillion;

namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBLeaderboard
    {
        public long LeaderboardId { get; set; }	
        public string PrototypeName { get; set; }
        public long ActiveInstanceId { get; set; }
        public bool IsActive { get; set; }
    }

    public class DBLeaderboardInstance 
    {
        public long InstanceId { get; set; }
        public long LeaderboardId { get; set; }
        public LeaderboardState State { get; set; }
        public long ActivationDate { get; set; }
        public bool Visible { get; set; }
        public DateTime ActivationDateTime => DateTimeOffset.FromUnixTimeSeconds(ActivationDate).DateTime;
    }
}
